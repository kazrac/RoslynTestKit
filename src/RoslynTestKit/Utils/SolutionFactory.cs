using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using RoslynTestKit.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace RoslynTestKit.Utils
{
	public class SolutionFactory
	{
		public static Solution Create(
			ICollection<DocumentChange> documentChanges,
			ICollection<ProjectSetup> projectSetups,
			string languageName,
			IReadOnlyCollection<MetadataReference>? references)
		{
			var workspace = new AdhocWorkspace();

			var options = workspace
				.Options
				.WithChangedOption(
					FormattingOptions.UseTabs,
					LanguageNames.CSharp,
					true
				);

			options = options
				.WithChangedOption(
					FormattingOptions.TabSize,
					LanguageNames.CSharp, 
					4
				);

			workspace
				.TryApplyChanges(
					workspace
						.CurrentSolution
						.WithOptions(options)
				);

			var solution = workspace
				.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), new VersionStamp()));

			foreach (var projectSetup in projectSetups)
			{
				solution = solution
					.AddProject(projectSetup.Name, projectSetup.Name, languageName)
					.WithCompilationOptions(
						new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
							.WithNullableContextOptions(
								projectSetup.IsNullableEnabled 
								? NullableContextOptions.Enable
								: NullableContextOptions.Disable
							)
					)
					.WithDefaultNamespace(projectSetup.Name)
					.AddMetadataReferences(CreateMetadataReferences(references))
					.Solution;
			}

			foreach (var documentChange in documentChanges)
			{
				var project = solution
					.Projects
					.First(x => x.Name == documentChange.ProjectName);

				if (documentChange.State != DocumentState.New)
				{
					solution = project
					.AddDocument(
						documentChange.DocumentName,
						documentChange.InitialCode,
						documentChange.Folders,
						documentChange.Path
					)
					.Project
					.Solution;
				}
			}

			return FillProjectReferences(
				solution,
				projectSetups
			);
		}

		private static Solution FillProjectReferences(
			Solution solution,
			ICollection<ProjectSetup> projectSetups)
		{
			var backupSolution = solution;

			foreach (var projectSetup in projectSetups)
			{
				var project = solution
					.Projects
					.First(p => p.Name == projectSetup.Name);

				solution = project
					.AddProjectReferences(
						projectSetup
							.ReferenceProjectNames
							.Select(
								x => new ProjectReference(
									backupSolution.Projects.First(p => p.Name == x).Id
								)
							)
					)
					.Solution;
			}

			return solution;
		}

		private static ImmutableArray<MetadataReference> CreateMetadataReferences(IReadOnlyCollection<MetadataReference>? references)
		{
			var immutableReferencesBuilder = ImmutableArray.CreateBuilder<MetadataReference>();
			if (references != null)
			{
				immutableReferencesBuilder.AddRange(references);
			}

			immutableReferencesBuilder.Add(ReferenceSource.Core);
			immutableReferencesBuilder.Add(ReferenceSource.Linq);
			immutableReferencesBuilder.Add(ReferenceSource.LinqExpression);

			if (ReferenceSource.Core.Display?.EndsWith("mscorlib.dll") == false)
			{
				foreach (var netStandardCoreLib in ReferenceSource.NetStandardBasicLibs.Value)
				{
					immutableReferencesBuilder.Add(netStandardCoreLib);
				}
			}

			return immutableReferencesBuilder.ToImmutable();
		}
	}
}