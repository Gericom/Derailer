using System.Collections.Generic;
using System.Linq;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen
{
    public class CBlock
    {
        public List<CStatement> Statements { get; } = new List<CStatement>();

        public CBlock(params CStatement[] statements)
        {
            Statements.AddRange(statements);
        }

        public override string ToString()
        {
            return "\n" + string.Join("\n",
                from st in Statements select (st is CExpression ? st.ToString() + ";" : st.ToString()));
        }
    }
}