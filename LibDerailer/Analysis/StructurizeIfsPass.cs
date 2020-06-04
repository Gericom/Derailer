using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;

namespace LibDerailer.Analysis
{
    //Based on https://github.com/nemerle/dcc/blob/qt5/src/control.cpp
    public class StructurizeIfsPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var unresolved = new List<IRBasicBlock>();
            foreach (var block in context.Function.BasicBlocks.AsEnumerable().Reverse())
            {
                if (block.SwitchFollow != null)
                {
                    foreach (var uBlock in unresolved)
                        if (block.SwitchFollow.ReversePostOrderIndex > uBlock.ReversePostOrderIndex)
                            uBlock.IfFollow = block.SwitchFollow;
                    unresolved.RemoveAll(a => a.IfFollow != null);
                    continue;
                }

                //and not some loop thingie
                if (block.Successors.Count != 2 || block.BlockJump.IsLoopJump || block.SwitchFollow != null)
                    continue;
                IRBasicBlock follow        = null;
                int          followInEdges = 0;
                for (int i = block.ReversePostOrderIndex + 1; i < context.Function.BasicBlocks.Count; i++)
                {
                    var dominatedBlock = context.Function.BasicBlocks[i];
                    if (dominatedBlock.ImmediateDominator != block)
                        continue;
                    int followInEdgesNew = dominatedBlock.Predecessors.Count - dominatedBlock.BackEdgeCount;
                    //if (followInEdgesNew >= followInEdges)
                    {
                        follow        = dominatedBlock;
                        followInEdges = followInEdgesNew;
                    }
                }

                if (follow != null && followInEdges > 1)
                {
                    block.IfFollow = follow;
                    foreach (var uBlock in unresolved)
                        if (follow.ReversePostOrderIndex > uBlock.ReversePostOrderIndex)
                            uBlock.IfFollow = follow;
                    unresolved.RemoveAll(a => a.IfFollow != null);
                }
                else
                    unresolved.Add(block);
            }
        }
    }
}