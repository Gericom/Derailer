using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class If : Statement
    {
        public Expression Predicate { get; set; }
        public Block      IfBody    { get; set; }
        public Block      ElseBody  { get; set; }

        public If(Expression predicate, Block ifBody = null, Block elseBody = null)
        {
            Predicate = predicate;
            IfBody    = ifBody ?? new Block();
            ElseBody  = elseBody;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"if ({Predicate})\n{{");
            builder.Append(AstUtil.Indent(IfBody.ToString()));

            if (ElseBody != null)
            {
                builder.Append("\n}\nelse\n{\n");
                builder.Append(AstUtil.Indent(ElseBody.ToString()));
            }

            builder.Append("\n}");

            return builder.ToString();
        }
    }
}