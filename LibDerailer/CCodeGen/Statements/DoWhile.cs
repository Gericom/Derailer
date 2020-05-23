using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class DoWhile : Statement
    {
        public Expression Predicate { get; set; }
        public Block      Body      { get; set; }

        public DoWhile(Expression predicate, Block body = null)
        {
            Predicate = predicate;
            Body      = body ?? new Block();
        }

        public override string ToString() 
            => "do\n" +
               "{" +
               $"{AstUtil.Indent(Body.ToString())}\n" +
               "}\n" +
               $"while ({Predicate});";
    }
}