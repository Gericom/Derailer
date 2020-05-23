using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen.Statements
{
    public class CDeclaration : CStatement
    {
        public CType Type { get; set; }
        public string   Name { get; set; }

        public CDeclaration(CType type, string name)
        {
            Type = type;
            Name = name;
        }

        public override string ToString() => $"{Type} {Name};";
    }
}