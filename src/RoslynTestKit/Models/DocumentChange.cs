using System.Collections.Generic;
using System.Linq;
using RoslynTestKit.Utils;

namespace RoslynTestKit.Models
{
    public class DocumentChange
    {
        public static DocumentChange CreateUnchanged(
            string code,
            string path = null,
            string documentName = null,
            string projectName = null)
        {
            return new DocumentChange(
                projectName ?? "TestProject",
                documentName ?? "TestDocument",
                DocumentState.Unchanged,
                code,
                code,
                path,
                path?.Split('\\').ToList(),
                null
            );
        }

        public DocumentChange(
            string projectName, 
            string documentName, 
            DocumentState state,
            string initialCode,
            string finalCode,
            string path,
            IReadOnlyList<string> folders,
            IDiagnosticLocator locator)
        {
            ProjectName = projectName;
            DocumentName = documentName;
            State = state;
            InitialCode = initialCode;
            FinalCode = finalCode;
            Path = path;
            Folders = folders;
            Locator = locator;
        }

        public string ProjectName { get; }
        public string DocumentName { get; }
        public DocumentState State { get; }
        public string InitialCode { get; }
        public string FinalCode { get; }
        public string Path { get; }
        public IReadOnlyList<string> Folders { get; }
        public IDiagnosticLocator Locator { get; }
        public bool IsTargetDocument => Locator != null;

    }
}