using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CDoWhile : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      Body      { get; set; }

        public CDoWhile(CExpression predicate, CBlock body = null)
        {
            Predicate = predicate;
            Body      = body ?? new CBlock();
        }

        public override string ToString() 
            => "do\n" +
               "{" +
               $"{AstUtil.Indent(Body.ToString())}\n" +
               "}\n" +
               $"while ({Predicate});";
    }
}