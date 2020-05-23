using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CFor : CStatement
    {
        public CStatement  Initial   { get; set; }
        public CStatement  Update    { get; set; }
        public CExpression Predicate { get; set; }
        public CBlock      Body      { get; set; } = new CBlock();

        public CFor()
        {
        }

        public CFor(CStatement initial, CStatement update, CExpression predicate, CBlock body)
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