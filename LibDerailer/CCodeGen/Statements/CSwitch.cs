using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CSwitch : CStatement
    {
        public CExpression Expression { get; set; }

        public List<(CLiteral caseVal, CBlock caseBlock)> Cases { get; } =
            new List<(CLiteral caseVal, CBlock caseBlock)>();

        public CSwitch(CExpression expression, params (CLiteral caseVal, CBlock caseBlock)[] cases)
        {
            Expression = expression;
            Cases.AddRange(cases);
        }

        public override string ToString()
        {
            string result = $"switch({Expression})\n{{\n";
            foreach (var (val, block) in Cases)
            {
                if(val is null)
                    result += $"    default:\n    {{{AstUtil.Indent(AstUtil.Indent(block.ToString()))}\n        break;\n    }}\n";
                else
                    result += $"    case {val}:\n    {{{AstUtil.Indent(AstUtil.Indent(block.ToString()))}\n        break;\n    }}\n";
            }

            return result + "}";
        }
    }
}