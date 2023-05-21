using System.Collections.Generic;
using System.Linq;
using RoslynTestKit.Utils;

namespace RoslynTestKit.Models
{
	public class DocumentChange
	{
		public static DocumentChange CreateUnchanged(
			string code,
			string path = null)
		{
			code = code
				.Replace("[|", "")
				.Replace("|]", "");

			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.Unchanged,
				code,
				code,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				null
			);
		}

		public static DocumentChange CreateNew(
			string code,
			string path = null)
		{
			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.New,
				code,
				code,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				null
			);
		}

		public static DocumentChange CreateTargetUnchanged(
			string markup,
			string path = null)
		{
			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();

			var locator = MarkupHelper.GetLocator(markup);
			var code = markup.Replace("[|", "").Replace("|]", "");

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.Unchanged,
				code,
				code,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				locator
			);
		}

		public static DocumentChange CreateChanged(
			string markup,
			string finalCode,
			string path = null)
		{
			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();

			var code = markup.Replace("[|", "").Replace("|]", "");

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.Changed,
				code,
				finalCode,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				null
			);
		}

		public static DocumentChange CreateTargetDeleted(
			string markup,
			string path = null)
		{
			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();


			var locator = MarkupHelper.GetLocator(markup);
			var code = markup.Replace("[|", "").Replace("|]", "");

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.Deleted,
				code,
				null,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				locator
			);
		}

		public static DocumentChange CreateTargetChange(
			string markup,
			string finalCode,
			string path = null)
		{
			path ??= @"TestProject\TestDocument.cs";

			var pathParts = path.Split('\\').ToList();

			var locator = MarkupHelper.GetLocator(markup);
			var code = markup.Replace("[|", "").Replace("|]", "");

			return new DocumentChange(
				pathParts[0],
				pathParts.Last(),
				DocumentState.Changed,
				code,
				finalCode,
				path,
				pathParts
					.Skip(1)
					.Take(pathParts.Count - 2)
					.ToList(),
				locator
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
			InitialCode = initialCode ?? string.Empty;
			FinalCode = finalCode ?? string.Empty;
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