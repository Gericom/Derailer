using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class For : Statement
    {
        public Statement  Initial   { get; set; }
        public Statement  Update    { get; set; }
        public Expression Predicate { get; set; }
        public Block      Body      { get; set; } = new Block();

        public For()
        {
        }

        public For(Statement initial, Statement update, Expression predicate, Block body)
        {
            Initial   = initial;
            Update    = update;
            Predicate = predicate;
            Body      = body;
        }

        public override string ToString()
            => $"for ({Initial}; {Predicate}; {Update})\n" +
               "{" +
               $"{AstUtil.Indent(Body.ToString())}\n" +
               "}";
    }
}