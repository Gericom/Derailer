using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using LibDerailer.CodeGraph;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.IR
{
    public class IRBasicBlock
    {
        public int                OrderIndex   { get; set; }
        public List<IRBasicBlock> Predecessors { get; } = new List<IRBasicBlock>();
        public List<IRBasicBlock> Successors   { get; } = new List<IRBasicBlock>();

        public List<IRInstruction> Instructions { get; } = new List<IRInstruction>();

        public bool IsLatchNode { get; set; }

        public IRBasicBlock LatchNode { get; set; }

        public LoopType LoopType { get; set; }

        public IRBasicBlock LoopHead         { get; set; }
        public IRBasicBlock LoopFollow       { get; set; }
        public IRBasicBlock ForLoopInitialIf { get; set; }
        public IRBasicBlock ForLoopHead      { get; set; }

        public IRBasicBlock IfFollow { get; set; }

        public IRJump BlockJump { get; set; }

        public int          PreOrderIndex         { get; set; }
        public int          ReversePostOrderIndex { get; set; }
        public IRBasicBlock ImmediateDominator    { get; set; }

        public int BackEdgeCount { get; set; }

        public HashSet<IRVariable> Uses     { get; } = new HashSet<IRVariable>();
        public HashSet<IRVariable> Defs     { get; } = new HashSet<IRVariable>();
        public HashSet<IRVariable> LiveIns  { get; } = new HashSet<IRVariable>();
        public HashSet<IRVariable> LiveOuts { get; } = new HashSet<IRVariable>();

        public IRVariable                       SwitchVariable { get; set; }
        public List<(IRConstant, IRBasicBlock)> SwitchCases    { get; } = new List<(IRConstant, IRBasicBlock)>();
        public IRBasicBlock                     SwitchFollow   { get; set; }

        public IRInstruction[] FindDefs(IRInstruction instruction, IRVariable v)
            => FindDefs(Instructions.IndexOf(instruction), v);

        public IRInstruction[] FindDefs(int instIdx, IRVariable v)
        {
            var  defLocs = new List<IRInstruction>();
            bool found   = false;
            for (int i = instIdx - 1; i >= 0; i--)
            {
                var instruction = Instructions[i];
                if (instruction.Defs.Contains(v))
                {
                    defLocs.Add(instruction);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var visited = new HashSet<IRBasicBlock>();
                var queue   = new Queue<IRBasicBlock>();
                foreach (var predecessor in Predecessors)
                    queue.Enqueue(predecessor);
                while (queue.Count > 0)
                {
                    var bb = queue.Dequeue();
                    if (visited.Contains(bb))
                        continue;
                    visited.Add(bb);
                    bool stop = false;
                    for (int i = bb.Instructions.Count - 1; i >= 0; i--)
                    {
                        var instruction = bb.Instructions[i];
                        if (instruction.Defs.Contains(v))
                        {
                            defLocs.Add(instruction);
                            stop = true;
                            break;
                        }

                        if (instruction == Instructions[instIdx])
                        {
                            stop = true;
                            break;
                        }
                    }

                    if (!stop)
                    {
                        foreach (var predecessor in bb.Predecessors)
                            queue.Enqueue(predecessor);
                    }
                }
            }

            return defLocs.ToArray();
        }

        public IRInstruction[] FindUses(IRInstruction instruction, IRVariable v)
            => FindUses(Instructions.IndexOf(instruction), v);

        public IRInstruction[] FindUses(int instIdx, IRVariable v)
        {
            var  useLocs = new List<IRInstruction>();
            bool found   = false;
            for (int i = instIdx + 1; i < Instructions.Count; i++)
            {
                var instruction = Instructions[i];
                if (instruction.Uses.Contains(v))
                    useLocs.Add(instruction);

                if (instruction.Defs.Contains(v))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var visited = new HashSet<IRBasicBlock>();
                var queue   = new Queue<IRBasicBlock>();
                foreach (var successor in Successors)
                    queue.Enqueue(successor);
                while (queue.Count > 0)
                {
                    var bb = queue.Dequeue();
                    if (visited.Contains(bb))
                        continue;
                    visited.Add(bb);
                    bool gotDef = false;
                    for (int i = 0; i < bb.Instructions.Count; i++)
                    {
                        var instruction = bb.Instructions[i];
                        if (instruction.Uses.Contains(v))
                            useLocs.Add(instruction);

                        if (instruction.Defs.Contains(v) || instruction == Instructions[instIdx])
                        {
                            gotDef = true;
                            break;
                        }
                    }

                    if (!gotDef)
                    {
                        foreach (var successor in bb.Successors)
                            queue.Enqueue(successor);
                    }
                }
            }

            return useLocs.ToArray();
        }

        public static IntervalNode[][] GetIntervalSequence(IEnumerable<IRBasicBlock> blocks, IRBasicBlock root)
        {
            //wrap original graph in IntervalNodes
            var g1 = blocks.Select(b => new IntervalNode(b)).ToArray();
            foreach (var node in g1)
            {
                node.Predecessors.AddRange(node.Block.Predecessors.Select(s => g1.First(g => g.Block == s)));
                node.Successors.AddRange(node.Block.Successors.Select(s => g1.First(g => g.Block == s)));
            }

            return IntervalNode.GetIntervalSequence(g1, g1.First(b => b.Block == root));
        }
    }
}