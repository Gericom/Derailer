using System.Collections.Generic;
using System.Linq;

namespace LibDerailer.CodeGraph
{
    public class IntervalNode : IGraphNode<IntervalNode>
    {
        public bool InLoop { get; set; }

        public BasicBlock Block    { get; }
        public Interval   Interval { get; }

        public List<IntervalNode> Predecessors { get; } = new List<IntervalNode>();
        public List<IntervalNode> Successors   { get; } = new List<IntervalNode>();

        public IntervalNode(Interval interval)
        {
            Interval = interval;
        }

        public IntervalNode(BasicBlock block)
        {
            Block = block;
        }

        public IntervalNode[] GetNodes()
        {
            if (Block != null)
                return new IntervalNode[0];

            return Interval.Blocks;
        }

        public BasicBlock GetHeadBasicBlock()
        {
            var inode = this;
            while (inode.Block == null)
                inode = inode.Interval.Header;
            return inode.Block;
        }

        public IEnumerable<BasicBlock> GetAllBasicBlocks()
        {
            if (Block != null)
                return new[] {Block};
            return Interval.Blocks.SelectMany(b => b.GetAllBasicBlocks());
        }

        public static IEnumerable<Interval> FindIntervals(IntervalNode[] blocks, IntervalNode header)
        {
            var heads           = new HashSet<IntervalNode>();
            var intervalVisited = new HashSet<IntervalNode>();
            heads.Add(header);
            while (true)
            {
                var n = heads.Except(intervalVisited).FirstOrDefault();
                if (n == null)
                    break;
                intervalVisited.Add(n);
                var iofn = new HashSet<IntervalNode>();
                iofn.Add(n);
                while (true)
                {
                    var candidate = iofn.SelectMany(i => i.Successors.Intersect(blocks)).Distinct()
                        .Except(iofn).FirstOrDefault(p => iofn.IsSupersetOf(p.Predecessors.Intersect(blocks)));
                    if (candidate == null)
                        break;
                    iofn.Add(candidate);
                }

                heads.UnionWith(blocks.Where(m =>
                    !heads.Contains(m) && !iofn.Contains(m) &&
                    m.Predecessors.Intersect(blocks).Any(p => iofn.Contains(p))));
                yield return new Interval(n, iofn.ToArray());
            }
        }

        public static IntervalNode[][] GetIntervalSequence(IntervalNode[] blocks, IntervalNode root)
        {
            var i1 = FindIntervals(blocks, root).ToArray();

            var intervalNodes = i1.Select(interval => new IntervalNode(interval)).ToArray();
            foreach (var intervalNode in intervalNodes)
            {
                intervalNode.Predecessors.AddRange(intervalNode.Interval.Blocks
                    .SelectMany(b => b.Predecessors)
                    .Except(intervalNode.Interval.Blocks)
                    .Select(b => intervalNodes.First(i => i.Interval.Blocks.Contains(b)))
                    .Distinct());
                intervalNode.Successors.AddRange(intervalNode.Interval.Blocks
                    .SelectMany(b => b.Successors)
                    .Except(intervalNode.Interval.Blocks)
                    .Select(b => intervalNodes.First(i => i.Interval.Blocks.Contains(b)))
                    .Distinct());
            }

            if (intervalNodes.Length != 1)
                return GetIntervalSequence(intervalNodes, intervalNodes[0]).Prepend(intervalNodes).ToArray();
            return new[] {intervalNodes};
        }
    }
}