using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using RoslynTestKit.Models;
using RoslynTestKit.Utils;

namespace RoslynTestKit
{
    public static class Verify
    {
        public static void CodeAction(CodeAction codeAction, Document document, string expectedCode)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult().ToList();
            if (operations.Count == 0)
            {
                throw RoslynTestKitException.NoOperationForCodeAction(codeAction);
            }

            var workspace = document.Project.Solution.Workspace;

            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }
            var newDocument = workspace.CurrentSolution.GetDocument(document.Id);

            if (newDocument == null)
            {
                throw new InvalidOperationException("Resulting solution does not have the original document");
            }

            var sourceText = newDocument.GetTextAsync(CancellationToken.None).GetAwaiter().GetResult();
            var mergedDocumentBuilder = new StringBuilder();
            var text = ConvertToLineEndingsAwareString(sourceText);

            mergedDocumentBuilder.Append(text);


            foreach (var doc in newDocument.Project.Documents.OrderByDescending(x => x.Name))
            {
                if (doc.Id != document.Id)
                {
                    mergedDocumentBuilder.AppendLine($"{Environment.NewLine}{BaseTestFixture.FileSeparator}");

                    var docText = ConvertToLineEndingsAwareString(sourceText);

                    mergedDocumentBuilder.Append(docText);
                }
            }
            var actualCode = mergedDocumentBuilder.ToString();

            if (actualCode != expectedCode)
            {
                DiffHelper.TryToReportDiffWithExternalTool(expectedCode, actualCode);
                var diff = DiffHelper.GenerateInlineDiff(expectedCode, actualCode);
                throw new TransformedCodeDifferentThanExpectedException(actualCode, expectedCode, diff);
            }
        }

        public static void CodeAction(
            CodeAction codeAction, 
            Document document,
            ICollection<DocumentChange> documentChanges)
        {
            var workspace = document.Project.Solution.Workspace;

            ApplyCodeAction(codeAction, workspace);

            foreach (var documentChange in documentChanges)
            {
                var correspondingDocument = workspace
                    .CurrentSolution
                    .Projects
                    .FirstOrDefault(x => x.Name == documentChange.ProjectName)
                    ?.Documents
                    .FirstOrDefault(
                        x =>
                            x.Name == documentChange.DocumentName
                            && x.Folders.SequenceEqual(documentChange.Folders)
                    );

                var actualCode = correspondingDocument
                    ?.GetTextAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult()
                    .ToString() ?? "";

                if (documentChange.State == DocumentState.Deleted)
                {
                    CheckDifference(actualCode, string.Empty);
                }
                else
                {
                    CheckDifference(
                        actualCode, 
                        documentChange.FinalCode
                    );
                }
            }
        }

        public static void ApplyCodeAction(
            CodeAction codeAction, 
            Workspace workspace)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult().ToList();
            if (operations.Count == 0)
            {
                throw RoslynTestKitException.NoOperationForCodeAction(codeAction);
            }

            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }
        }

        private static void CheckDifference(string actualCode, string expectedCode)
        {
            if (actualCode != expectedCode)
            {
                DiffHelper.TryToReportDiffWithExternalTool(expectedCode, actualCode);
                var diff = DiffHelper.GenerateInlineDiff(expectedCode, actualCode);
                throw new TransformedCodeDifferentThanExpectedException(actualCode, expectedCode, diff);
            }
        }

        private static string ConvertToLineEndingsAwareString(SourceText sourceText)
        {
            string text = sourceText.ToString();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                text = text.Replace("\r\n", "\n");
            }

            return text;
        }
    }
}
