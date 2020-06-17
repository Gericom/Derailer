using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CodeGraph;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR
{
    public class IRContext
    {
        public ProgramContext ProgramContext { get; set; }

        public Dictionary<Variable, IRVariable> VariableMapping { get; } = new Dictionary<Variable, IRVariable>();

        public Dictionary<BasicBlock, IRBasicBlock> BasicBlockMapping { get; } =
            new Dictionary<BasicBlock, IRBasicBlock>();

        public IRFunction Function { get; set; }
    }
}