﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using RoslynTestKit.CodeActionLocators;
using RoslynTestKit.Models;
using RoslynTestKit.Utils;

namespace RoslynTestKit
{
    public abstract class SolutionWideCodeRefactoringTestFixture : BaseTestFixture
    {
        protected abstract CodeRefactoringProvider CreateProvider();

        protected void TestCodeRefactoring(
            ICollection<DocumentChange> documentChanges,
            ICollection<ProjectSetup> projectSetups = null,
            int refactoringIndex = 0)
        {
            if (projectSetups == null)
            {
                projectSetups = new[] { new ProjectSetup("TestProject") };
            }

            var document = MarkupHelper
                .GetTargetDocumentInSolution(
                    documentChanges,
                    projectSetups,
                    LanguageName,
                    References
                );

            var locator = documentChanges
                .First(x => x.IsTargetDocument)
                .Locator;

            var codeActionSelector = new ByIndexCodeActionSelector(refactoringIndex);

            if (FailWhenInputContainsErrors)
            {
                var errors = document.GetErrors();
                if (errors.Count > 0)
                {
                    throw RoslynTestKitException.UnexpectedErrorDiagnostic(errors);
                }
            }

            var codeRefactorings = GetCodeRefactorings(document, locator);
            var selectedRefactoring = codeActionSelector.Find(codeRefactorings);

            if (selectedRefactoring is null)
            {
                throw RoslynTestKitException.CodeRefactoringNotFound(codeActionSelector, codeRefactorings, locator);
            }

            Verify
                .CodeAction(
                    selectedRefactoring,
                    document,
                    documentChanges
                );
        }

 

        protected void TestCodeRefactoring(string markupCode, string expected, int refactoringIndex = 0)
        {
            var document = MarkupHelper.GetDocumentFromMarkup(markupCode, LanguageName, References);
            var locator = MarkupHelper.GetLocator(markupCode);
            TestCodeRefactoring(document, expected, locator, new ByIndexCodeActionSelector(refactoringIndex));
        }

        protected virtual bool FailWhenInputContainsErrors => true;

        protected void TestCodeRefactoring(string sourceMarkupCode, string expected, string title)
        {
            var document = MarkupHelper.GetDocumentFromMarkup(sourceMarkupCode, LanguageName, References);
            var locator = MarkupHelper.GetLocator(sourceMarkupCode);
            TestCodeRefactoring(document, expected, locator, new ByTitleCodeActionSelector(title));
        }

        protected void TestCodeRefactoringAtLine(string code, string expected, int line, int refactoringIndex = 0)
        {
            var document = MarkupHelper.GetDocumentFromCode(code, LanguageName, References);
            var locator = LineLocator.FromCode(code, line);
            TestCodeRefactoring(document, expected, locator, new ByIndexCodeActionSelector(refactoringIndex));
        }
        protected void TestCodeRefactoringAtLine(Document document, string expected, int line, int refactoringIndex = 0)
        {
            var locator = LineLocator.FromDocument(document, line);
            TestCodeRefactoring(document, expected, locator, new ByIndexCodeActionSelector(refactoringIndex));
        }

        protected void TestCodeRefactoring(Document document, string expected, TextSpan span, int refactoringIndex = 0)
        {
            var locator = new TextSpanLocator(span);
            TestCodeRefactoring(document, expected, locator, new ByIndexCodeActionSelector(refactoringIndex));
        }

        private void TestCodeRefactoring(Document document, string expected, IDiagnosticLocator locator, ICodeActionSelector codeActionSelector)
        {
            if (FailWhenInputContainsErrors)
            {
                var errors = document.GetErrors();
                if (errors.Count > 0)
                {
                    throw RoslynTestKitException.UnexpectedErrorDiagnostic(errors);
                }
            }

            var codeRefactorings = GetCodeRefactorings(document, locator);
            var selectedRefactoring = codeActionSelector.Find(codeRefactorings);
            
            if (selectedRefactoring is null)
            {
                throw RoslynTestKitException.CodeRefactoringNotFound(codeActionSelector, codeRefactorings, locator);
            }

            Verify.CodeAction(selectedRefactoring, document, expected);
        }

        protected void TestNoCodeRefactoring(string markupCode)
        {
            var document = MarkupHelper.GetDocumentFromMarkup(markupCode, LanguageName, References);
            var locator = MarkupHelper.GetLocator(markupCode);
            TestNoCodeRefactoring(document, locator);
        }

        protected void TestNoCodeRefactoring(Document document, TextSpan span)
        {
            var locator = new TextSpanLocator(span);
            TestNoCodeRefactoring(document, locator);
        }

        private void TestNoCodeRefactoring(Document document, IDiagnosticLocator locator)
        {
            var codeRefactorings = GetCodeRefactorings(document, locator).ToImmutableArray();
            if (codeRefactorings.Length != 0)
            {
                throw RoslynTestKitException.UnexpectedCodeRefactorings(codeRefactorings);
            }
        }

        private ImmutableArray<CodeAction> GetCodeRefactorings(Document document, IDiagnosticLocator locator)
        {
            var builder = ImmutableArray.CreateBuilder<CodeAction>();
            var context = new CodeRefactoringContext(document, locator.GetSpan(), a => builder.Add(a), CancellationToken.None);
            var provider = CreateProvider();
            provider.ComputeRefactoringsAsync(context).Wait();
            return builder.ToImmutable();
        }
    }
}