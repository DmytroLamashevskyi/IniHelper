using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class IniClassAttribute : Attribute
    {
        public IniClassAttribute(string section)
        {
            Section = section;
        }

        public string Section { get; }

    }
}
