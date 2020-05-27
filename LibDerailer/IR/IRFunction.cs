using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.IR.Instructions;

namespace LibDerailer.IR
{
    public class IRFunction
    {
        public List<IRBasicBlock> BasicBlocks { get; } = new List<IRBasicBlock>();
        public IRBasicBlock       Epilogue    { get; set; }
    }
}