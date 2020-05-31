using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;

namespace LibDerailer.Analysis
{
    //Based on https://github.com/nemerle/dcc/blob/qt5/src/control.cpp
    public class StructurizeLoopsPass : AnalysisPass
    {
        private void FindLoopNodes(IRFunction func, IRBasicBlock headNode, IRBasicBlock latchNode,
            IRBasicBlock[] intervalNodes)
        {
            headNode.LoopHead = headNode;
            var loopNodes = new List<IRBasicBlock>();
            loopNodes.Add(headNode);
            for (int i = headNode.ReversePostOrderIndex + 1; i < latchNode.ReversePostOrderIndex; i++)
            {
                var block = func.BasicBlocks[i];
                if (loopNodes.Contains(block.ImmediateDominator) && intervalNodes.Contains(block))
                {
                    loopNodes.Add(block);
                    if (block.LoopHead == null)
                        block.LoopHead = headNode;
                }
            }

            latchNode.LoopHead = headNode;
            if (latchNode != headNode)
                loopNodes.Add(latchNode);

            if (latchNode.Successors.Count == 2)
            {
                var latchElseBlock = latchNode.BlockJump.Destination;
                var latchIfBlock   = latchNode.Successors.First(s => s != latchElseBlock);
                // if (headNode.Successors.Count == 2 || latchNode == headNode)
                // {
                //     var headElseBlock = headNode.BlockBranch.Destination;
                //     var headIfBlock   = headNode.Successors.First(s => s != headElseBlock);
                //     // if (latchNode == headNode || (loopNodes.Contains(headElseBlock) && loopNodes.Contains(headIfBlock)))
                //     // {
                //         headNode.LoopType = LoopType.Repeat;
                //         if (latchIfBlock == headIfBlock)
                //             headNode.LoopFollow = latchElseBlock;
                //         else
                //             headNode.LoopFollow = latchIfBlock;
                //     // }
                //     // else
                //     // {
                //     //     headNode.LoopType = LoopType.While;
                //     //     if (loopNodes.Contains(headIfBlock))
                //     //         headNode.LoopFollow = headElseBlock;
                //     //     else
                //     //         headNode.LoopFollow = headIfBlock;
                //     // }
                // }
                // else
                // {
                headNode.LoopType = LoopType.DoWhile;
                if (latchIfBlock == headNode)
                    headNode.LoopFollow = latchElseBlock;
                else
                    headNode.LoopFollow = latchIfBlock;
                latchNode.BlockJump.IsLoopJump = true;
                // }
            }
            else
            {
                //todo
            }
        }

        private bool IsBackEdge(IRBasicBlock p, IRBasicBlock s)
        {
            if (p.PreOrderIndex < s.PreOrderIndex)
                return false;

            s.BackEdgeCount++;
            return true;
        }

        public override void Run(IRContext context)
        {
            var intervals =
                IRBasicBlock.GetIntervalSequence(context.Function.BasicBlocks, context.Function.BasicBlocks[0]);

            foreach (var gi in intervals)
            {
                var ni = gi.SelectMany(g => g.GetNodes()).ToArray();
                foreach (var intervalNode in gi)
                {
                    var headBBlock = intervalNode.GetHeadBasicBlock();
                    var nodes      = intervalNode.GetAllBasicBlocks().ToArray();

                    IRBasicBlock latchNode = null;
                    foreach (var predecessor in headBBlock.Predecessors)
                    {
                        if (!nodes.Contains(predecessor) || !IsBackEdge(predecessor, headBBlock))
                            continue;
                        if (latchNode == null || predecessor.ReversePostOrderIndex > latchNode.ReversePostOrderIndex)
                            latchNode = predecessor;
                    }

                    if (latchNode != null)
                    {
                        //todo: case level check
                        headBBlock.LatchNode = latchNode;
                        FindLoopNodes(context.Function, headBBlock, latchNode, nodes);
                        latchNode.IsLatchNode = true;
                    }
                }
            }
        }
    }
}