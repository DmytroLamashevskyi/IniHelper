using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IniAttribute : Attribute
    {
        public string Name { get; }

        // Constructor to accept attribute parameters
        public IniAttribute(string name)
        {
            Name = name;
        }
    }
}