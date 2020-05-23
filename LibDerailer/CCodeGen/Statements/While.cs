using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class While : Statement
    {
        public Expression Predicate { get; set; }
        public Block      Body      { get; set; }

        public While(Expression predicate, Block body = null)
        {
            Predicate = predicate;
            Body      = body ?? new Block();
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