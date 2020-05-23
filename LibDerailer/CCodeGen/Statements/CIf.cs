using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CIf : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      IfBody    { get; set; }
        public CBlock      ElseBody  { get; set; }

        public CIf(CExpression predicate, CBlock ifBody = null, CBlock elseBody = null)
        {
            Predicate = predicate;
            IfBody    = ifBody ?? new CBlock();
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