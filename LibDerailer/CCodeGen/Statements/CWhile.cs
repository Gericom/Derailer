using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CWhile : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      Body      { get; set; }

        public CWhile(CExpression predicate, CBlock body = null)
        {
            Predicate = predicate;
            Body      = body ?? new CBlock();
        }

        public override string ToString()
        {
            if (Body == null || Body.Statements.Count == 0)
                return $"while ({Predicate});";

            return $"while ({Predicate})\n" +
                   "{" +
                   $"{AstUtil.Indent(Body.ToString())}\n" +
                   "}";
        }
    }
}