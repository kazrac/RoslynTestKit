using System.Collections.Generic;
using System.Linq;

namespace RoslynTestKit.Models
{
    public class ProjectSetup
    {
        public ProjectSetup(
            string name)
            :this(name, false, new string[] {})
        {
        }

        public ProjectSetup(
            string name, 
            params string[] referenceProjectNames)
			: this(name, false, referenceProjectNames)
        {
		}

		public ProjectSetup(
			string name,
			bool isNullableEnabled)
			: this(
				name, 
				isNullableEnabled, 
				new string[] { }
			)
		{
		}

		public ProjectSetup(
			string name,
			bool isNullableEnabled,
			params string[] referenceProjectNames)
		{
			Name = name;
			IsNullableEnabled = isNullableEnabled;
			ReferenceProjectNames = referenceProjectNames.ToList();
		}

		public string Name { get; }
		public bool IsNullableEnabled { get; }
		public IReadOnlyList<string> ReferenceProjectNames { get; }

    }
}