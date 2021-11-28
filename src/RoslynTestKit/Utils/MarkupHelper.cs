﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using RoslynTestKit.Models;

namespace RoslynTestKit.Utils
{
    internal static class MarkupHelper
    {
        public static Document GetTargetDocumentInSolution(
             ICollection<DocumentChange> documentChanges,
             ICollection<ProjectSetup> projectSetups,
             string languageName,
             IReadOnlyCollection<MetadataReference> references)
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), new VersionStamp()));

            foreach (var projectName in projectSetups.Select(x => x.Name))
            {
                solution = solution
                    .AddProject(projectName, projectName, languageName)
                    .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddMetadataReferences(CreateMetadataReferences(references))
                    .Solution;
            }

            foreach (var documentChange in documentChanges)
            {
                var project = solution
                    .Projects
                    .First(x => x.Name == documentChange.ProjectName);

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

            var projectMetaReferences = new Dictionary<string, MetadataReference>();

            solution = FillMetaReferences(
                solution,
                projectSetups,
                projectMetaReferences
            );

            var targetDocument = documentChanges
                .First(x => x.IsTargetDocument);

            return solution
                .Projects
                .First(x => x.Name == targetDocument.ProjectName)
                .Documents
                .First(x => x.FilePath == targetDocument.Path);
        }

        private static Solution FillMetaReferences(
            Solution solution,
            ICollection<ProjectSetup> projectSetups,
            Dictionary<string, MetadataReference> projectMetaReferences)
        {
            foreach (var projectSetup in projectSetups)
            {
                if (projectMetaReferences.ContainsKey(projectSetup.Name))
                {
                    continue;
                }

                solution = AddMetaReference(
                    solution,
                    projectSetup,
                    projectSetups,
                    projectMetaReferences
                );
            }

            return solution;
        }

        private static Solution AddMetaReference(
            Solution solution,
            ProjectSetup projectSetup,
            ICollection<ProjectSetup> projectSetups,
            Dictionary<string, MetadataReference> projectMetaReferences)
        {
            if (projectMetaReferences.ContainsKey(projectSetup.Name))
            {
                return solution;
            }

            foreach (var projectName in projectSetup.ReferenceProjectNames)
            {
                if (!projectMetaReferences.ContainsKey(projectName))
                {
                    var dependentProjectSetup = projectSetups.First(x => x.Name == projectName);

                    solution = AddMetaReference(
                        solution,
                        dependentProjectSetup,
                        projectSetups,
                        projectMetaReferences
                    );
                }

                solution = solution
                    .Projects
                    .First(x => x.Name == projectSetup.Name)
                    .AddMetadataReference(
                        projectMetaReferences[projectName]
                    )
                    .Solution;
            }

            var metaReference = solution
                ?.Projects
                .First(x => x.Name == projectSetup.Name)
                .GetCompilationAsync()
                .Result
                ?.ToMetadataReference();

            projectMetaReferences
                .Add(projectSetup.Name, metaReference);

            return solution;
        }

        public static Document GetDocumentFromMarkup(string markup, string languageName, IReadOnlyCollection<MetadataReference> references, string projectName = null, string documentName = null)
        {
            var code = markup.Replace("[|", "").Replace("|]", "");
            return GetDocumentFromCode(code, languageName, references, projectName, documentName);
        }

        public static Document GetDocumentFromCode(string code, string languageName, IReadOnlyCollection<MetadataReference> references, string projectName= null, string documentName= null)
        {
            var metadataReferences = CreateMetadataReferences(references);

            var compilationOptions = GetCompilationOptions(languageName);

            return new AdhocWorkspace()
                .AddProject(projectName ?? "TestProject", languageName)
                .WithCompilationOptions(compilationOptions)
                .AddMetadataReferences(metadataReferences)
                .AddDocument(documentName ?? "TestDocument", code);
        }

        private static CompilationOptions GetCompilationOptions(string languageName) =>
            languageName switch
            {
                LanguageNames.CSharp => (CompilationOptions) new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                LanguageNames.VisualBasic => (CompilationOptions) new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                _ => throw new NotSupportedException($"Language {languageName} is not supported")
            };

        private static ImmutableArray<MetadataReference> CreateMetadataReferences(IReadOnlyCollection<MetadataReference> references)
        {
            var immutableReferencesBuilder = ImmutableArray.CreateBuilder<MetadataReference>();
            if (references != null)
            {
                immutableReferencesBuilder.AddRange(references);
            }

            immutableReferencesBuilder.Add(ReferenceSource.Core);
            immutableReferencesBuilder.Add(ReferenceSource.Linq);
            immutableReferencesBuilder.Add(ReferenceSource.LinqExpression);

            if (ReferenceSource.Core.Display.EndsWith("mscorlib.dll") == false)
            {
                foreach (var netStandardCoreLib in ReferenceSource.NetStandardBasicLibs.Value)
                {
                    immutableReferencesBuilder.Add(netStandardCoreLib);
                }
            }

            return immutableReferencesBuilder.ToImmutable();
        }

        public static IDiagnosticLocator GetLocator(string markupCode)
        {
            if (TryFindMarkedTimeSpan(markupCode, out var textSpan))
            {
                return new TextSpanLocator(textSpan);
            }

            throw new Exception("Cannot find any position marked with [||]");
        }

        private static bool TryFindMarkedTimeSpan(string markupCode, out TextSpan textSpan)
        {
            textSpan = default;
            var start = markupCode.IndexOf("[|", StringComparison.InvariantCulture);
            if (start < 0)
            {
                return false;
            }

            var end = markupCode.IndexOf("|]", StringComparison.InvariantCulture);
            if (end < 0)
            {
                return false;
            }

            textSpan = TextSpan.FromBounds(start, end - 2);
            return true;
        }
    }
}