using System.Collections.Generic;
using System.Linq;

namespace LibDerailer.CodeGraph
{
    public class Interval
    {
        public IntervalNode   Header { get; }
        public IntervalNode[] Blocks { get; }

        public Interval(IntervalNode header, IntervalNode[] blocks)
        {
            Header = header;
            Blocks = blocks;
        }
    }
}