using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;

namespace LibDerailer.Analysis
{
    public abstract class AnalysisPass
    {
        public abstract void Run(IRContext context);
    }
}
