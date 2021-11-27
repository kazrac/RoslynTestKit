using System.Collections.Generic;

namespace RoslynTestKit.Models
{
    public class ProjectSetup
    {
        public ProjectSetup(
            string name)
            :this(name, new List<string>())
        {
        }

        public ProjectSetup(
            string name, 
            IReadOnlyList<string> referenceProjectNames)
        {
            Name = name;
            ReferenceProjectNames = referenceProjectNames;
        }

        public string Name { get; }
        public IReadOnlyList<string> ReferenceProjectNames { get; }

    }
}