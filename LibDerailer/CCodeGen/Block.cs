using System.Collections.Generic;
using System.Linq;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen
{
    public class Block
    {
        public List<Statement> Statements { get; } = new List<Statement>();

        public Block(params Statement[] statements)
        {
            Statements.AddRange(statements);
        }

        public override string ToString()
        {
            return "\n" + string.Join("\n",
                from st in Statements select (st is Expression ? st.ToString() + ";" : st.ToString()));
        }
    }
}