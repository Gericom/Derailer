using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.Analysis
{
    public class ForLoopsPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            for (int i = context.Function.BasicBlocks.Count - 1; i >= 0; i--)
            {
                var block = context.Function.BasicBlocks[i];
                if (block.LoopHead != block || block.LoopType != LoopType.DoWhile)
                    continue;

                //find all loop invariant things
                var defs = new HashSet<IRVariable>();
                var uses = new HashSet<IRVariable>();
                for (int j = block.ReversePostOrderIndex; j <= block.LatchNode.ReversePostOrderIndex; j++)
                {
                    var loopBlock = context.Function.BasicBlocks[j];
                    if (loopBlock.LoopHead != block)
                        continue;
                    foreach (var instruction in loopBlock.Instructions)
                    {
                        defs.UnionWith(instruction.Defs);
                        uses.UnionWith(instruction.Uses);
                    }
                }

                var candidates = new HashSet<IRVariable>(defs);
                candidates.IntersectWith(uses);

                var condVars = new HashSet<IRVariable>(candidates);
                condVars.IntersectWith(block.LatchNode.BlockJump.Uses);

                if (condVars.Count != 1)
                    continue;

                var condVar = condVars.First();

                var condVarDefs = block.FindDefs(block.Instructions.First(), condVar);
                int inLoopCount = condVarDefs.Count(v => v.ParentBlock.LoopHead == block);
                int outLoopCount = condVarDefs.Length - inLoopCount;
                if (outLoopCount != 1 || inLoopCount != 1)
                    continue;
                var initialDef = condVarDefs.First(v => v.ParentBlock.LoopHead != block);
                if (!(initialDef is IRAssignment initialAssgn))
                    continue;
                var ifExpr = block.LatchNode.BlockJump.Condition.CloneComplete();
                ifExpr.Substitute(condVar, initialAssgn.Source);
                ifExpr = ifExpr.ReverseConditionSides();

                if (!(ifExpr is IRComparisonExpression cmp && cmp.OperandA is IRConstant && cmp.OperandB is IRConstant))
                {
                    var invIfExpr = ifExpr.CloneComplete().InverseCondition();
                    IRBasicBlock ifBlock = null;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var b = context.Function.BasicBlocks[j];
                        if (b.BlockJump != null &&
                            ((b.BlockJump.Destination != block.LoopFollow && b.BlockJump.Condition.Equals(ifExpr)) ||
                             (b.BlockJump.Destination == block.LoopFollow && b.BlockJump.Condition.Equals(invIfExpr))))
                        {
                            ifBlock = b;
                            break;
                        }
                    }

                    if (ifBlock == null)
                        continue;

                    block.ForLoopInitialIf = ifBlock;
                    ifBlock.ForLoopHead = block;

                    //if we found the if block, we need to check for hoisted variables which are declared just before the loop
                    var hoistBlock = ifBlock.Successors.First(s => s != block.LoopFollow);
                    if (hoistBlock != block)
                    {
                        if (hoistBlock.Successors.Count != 1 || hoistBlock.Successors[0] != block)
                            throw new Exception();
                        var hoistVars = new HashSet<IRVariable>();
                        var hoistInsts = new Dictionary<IRVariable, IRAssignment>();
                        foreach (var instruction in hoistBlock.Instructions)
                        {
                            if (!(instruction is IRAssignment assg) || !(assg.Destination is IRVariable hoistVar))
                                continue;
                            hoistInsts.Add(hoistVar, assg);
                            hoistVars.Add(hoistVar);
                        }

                        var invariantVars = new HashSet<IRVariable>(hoistVars);
                        invariantVars.ExceptWith(defs);
                        //invariantVars should be the vars that were invariant-hoisted out of the loop
                        //we can now simply substitute them back
                        foreach (var invariantVar in invariantVars)
                        {
                            var hoistInst = hoistInsts[invariantVar];
                            var hoistUses = hoistBlock.FindUses(hoistInst, invariantVar);
                            foreach (var use in hoistUses)
                                use.SubstituteUse(invariantVar, hoistInst.Source);
                            hoistBlock.Instructions.Remove(hoistInst);
                        }

                        hoistVars.ExceptWith(invariantVars);

                        var strengthVars = new HashSet<IRVariable>(hoistVars);
                        strengthVars.IntersectWith(candidates);
                        foreach (var strengthVar in strengthVars)
                        {
                            var hoistInst = hoistInsts[strengthVar];
                            var hoistDefs = block.FindDefs(0, strengthVar);
                            if (hoistDefs.Length != 2)
                                continue;
                            if (!(hoistDefs.FirstOrDefault(d => d != hoistInst) is IRAssignment assg))
                                continue;
                            if (!(assg.Source is IRBinaryExpression bin) || bin.Operator != IRBinaryOperator.Add)
                                continue;

                            var mulFactor = bin.OperandA.Equals(strengthVar) ? bin.OperandB : bin.OperandA;
                            var mulExpr = condVar * mulFactor;

                            var hoistUses = hoistBlock.FindUses(hoistInst, strengthVar);
                            foreach (var use in hoistUses)
                                use.SubstituteUse(strengthVar, mulExpr);
                            hoistBlock.Instructions.Remove(hoistInst);
                            assg.ParentBlock.Instructions.Remove(assg);
                        }

                        if (hoistBlock.Instructions.Count != 0)
                            continue;
                        ifBlock.Successors[ifBlock.Successors.IndexOf(hoistBlock)] = block;
                        block.Predecessors[block.Predecessors.IndexOf(hoistBlock)] = ifBlock;
                        hoistBlock.Predecessors.Clear();
                        hoistBlock.Successors.Clear();
                    }
                }

                block.LoopType = LoopType.For;

                //move update of loop var to end of latch block
                if(condVarDefs.Length != 2)
                    throw new Exception();
                var loopVarDef = condVarDefs.First(v => v is IRAssignment && v.ParentBlock.LoopHead == block);//v.ParentBlock == block.LatchNode);
                loopVarDef.ParentBlock.Instructions.Remove(loopVarDef);
                block.LatchNode.Instructions.Insert(block.LatchNode.Instructions.Count - 1, loopVarDef);
            }
        }
    }
}
