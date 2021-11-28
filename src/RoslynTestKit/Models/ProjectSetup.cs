using System.Collections.Generic;
using System.Linq;

namespace RoslynTestKit.Models
{
    public class ProjectSetup
    {
        public ProjectSetup(
            string name)
            :this(name, new string[] {})
        {
        }

        public ProjectSetup(
            string name, 
            params string[] referenceProjectNames)
        {
            Name = name;
            ReferenceProjectNames = referenceProjectNames.ToList();
        }

        public string Name { get; }
        public IReadOnlyList<string> ReferenceProjectNames { get; }

    }
}