using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;

namespace LibDerailer.Analysis
{
    public class LifetimeAnalyser : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            //Pass 1
            foreach (var block in context.Function.BasicBlocks)
            {
                block.Uses.Clear();
                block.Defs.Clear();
                block.LiveIns.Clear();
                block.LiveOuts.Clear();
                for (int i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var instr = block.Instructions[i];
                    instr.LiveOuts.Clear();
                    instr.LiveOuts.UnionWith(instr.Uses);
                    instr.Dead.Clear();
                    instr.Dead.UnionWith(instr.Defs);
                    instr.Dead.ExceptWith(instr.LiveOuts);

                    block.Uses.ExceptWith(instr.Dead);
                    block.Uses.UnionWith(instr.LiveOuts);

                    block.Defs.ExceptWith(instr.LiveOuts);
                    block.Defs.UnionWith(instr.Dead);
                }
            }

            //Pass 2
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var block in context.Function.BasicBlocks)
                {
                    var outs = new HashSet<IRVariable>();
                    foreach (var successor in block.Successors)
                        outs.UnionWith(successor.LiveIns);

                    var ins = new HashSet<IRVariable>();
                    ins.UnionWith(outs);
                    ins.ExceptWith(block.Defs);
                    ins.UnionWith(block.Uses);
                    if(!ins.SetEquals(block.LiveIns) || !outs.SetEquals(block.LiveOuts))
                    {
                        changed = true;
                        block.LiveIns.Clear();
                        block.LiveIns.UnionWith(ins);
                        block.LiveOuts.Clear();
                        block.LiveOuts.UnionWith(outs);
                    }
                }
            }

            //Pass 3
            foreach (var block in context.Function.BasicBlocks)
            {
                var live = new HashSet<IRVariable>();
                foreach (var successor in block.Successors)
                    live.UnionWith(successor.LiveIns);

                for (int i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var instr = block.Instructions[i];
                    var newLive = new HashSet<IRVariable>(live);
                    newLive.ExceptWith(instr.Dead);
                    newLive.UnionWith(instr.LiveOuts);
                    instr.LiveOuts.Clear();
                    instr.LiveOuts.UnionWith(live);
                    live = newLive;
                }
            }
        }
    }
}