using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using RoslynTestKit.CodeActionLocators;
using RoslynTestKit.Models;
using RoslynTestKit.Utils;

namespace RoslynTestKit
{
    public abstract class SolutionWideCodeRefactoringTestFixture : BaseTestFixture
    {
        protected abstract CodeRefactoringProvider CreateProvider();

        protected virtual bool FailWhenInputContainsErrors => true;

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

		protected void TestExpectedCodeActions(
			ICollection<DocumentChange> documentChanges,
			ICollection<ProjectSetup> projectSetups = null,
			params string[] expectedCodeActionTitles)
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
            
			if (FailWhenInputContainsErrors)
			{
				var errors = document.GetErrors();
				if (errors.Count > 0)
				{
					throw RoslynTestKitException.UnexpectedErrorDiagnostic(errors);
				}
			}

			var codeRefactorings = GetCodeRefactorings(
				document, 
				locator
			);

			var titlesOfUnexpectedCodeRefactorings = codeRefactorings
				.Select(x => x.Title)
				.Except(expectedCodeActionTitles)
				.ToList();

			var titlesOfMissingCodeRefactorings = expectedCodeActionTitles
				.Except(
					codeRefactorings
						.Select(
							x => x.Title
						)
				)
				.ToList();

            if(titlesOfUnexpectedCodeRefactorings.Any())
			{
				throw RoslynTestKitException
					.UnexpectedCodeRefactorings(
						codeRefactorings
							.Where(x => titlesOfUnexpectedCodeRefactorings.Contains(x.Title))
							.ToImmutableArray()
					);
			}

			if (titlesOfMissingCodeRefactorings.Any())
			{
				throw RoslynTestKitException
					.CodeRefactoringNotFound(
						codeRefactorings
							.Where(x => titlesOfMissingCodeRefactorings.Contains(x.Title))
							.ToImmutableArray()
					);
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
