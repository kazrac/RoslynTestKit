﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RoslynTestKit
{
    public static class ReferenceSource
    {
        internal static readonly MetadataReference Core = FromType<int>();
        internal static readonly MetadataReference Linq = FromType(typeof(Enumerable));
        internal static readonly MetadataReference LinqExpression = FromType(typeof(System.Linq.Expressions.Expression));
        private static readonly string[]? _netCoreAssemblies;
        public static readonly MetadataReference? NetStandardCore = null;

        static ReferenceSource()
        {
			if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
            {
                _netCoreAssemblies = trustedPlatformAssemblies.Split(Path.PathSeparator);

				var mscorlibDllPath = _netCoreAssemblies?.FirstOrDefault(x => x.EndsWith("mscorlib.dll"));

				if (mscorlibDllPath != null)
				{
					NetStandardCore = MetadataReference.CreateFromFile(mscorlibDllPath);
				}
            }
        }

        internal static readonly Lazy<IReadOnlyList<MetadataReference>> NetStandardBasicLibs = new Lazy<IReadOnlyList<MetadataReference>>(()=> GetNetStandardCoreLibs().ToList());
        internal static IEnumerable<MetadataReference> GetNetStandardCoreLibs()
        {
            if (NetStandardCore != null)
            {
                yield return NetStandardCore;

                var mscorlibFile = _netCoreAssemblies?.FirstOrDefault(x => x.EndsWith("mscorlib.dll"));
                if (string.IsNullOrWhiteSpace(mscorlibFile) == false)
                {
                    var referencedAssemblies = Assembly.LoadFile(mscorlibFile).GetReferencedAssemblies();
                    foreach (var referencedAssembly in referencedAssemblies)
                    {
                        
                        var assemblyFile = _netCoreAssemblies?.FirstOrDefault(x => x.EndsWith($"{referencedAssembly.Name}.dll"));
                        if (string.IsNullOrWhiteSpace(assemblyFile) == false)
                        {
                            yield return MetadataReference.CreateFromFile(assemblyFile);
                        }
                    }
                }
            }
        }

        public static MetadataReference FromType<T>() => FromType(typeof(T));

        public static MetadataReference FromAssembly(Assembly assembly) => FromAssembly(assembly.Location);

        public static MetadataReference FromAssembly(string assemblyLocation) => MetadataReference.CreateFromFile(assemblyLocation);

        public static MetadataReference FromType(Type type) => MetadataReference.CreateFromFile(type.Assembly.Location);

        public static readonly MetadataReference NetStandard2_0 = MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location);
    }
}