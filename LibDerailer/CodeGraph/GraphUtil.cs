using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public static class GraphUtil
    {
        public static Dictionary<T, int> GetReversePostOrder<T>(T[] nodes, T root) where T : IGraphNode<T>
        {
            var nodeId     = new Dictionary<T, int>();
            int curId      = nodes.Length - 1;
            var dfsStack   = new Stack<(T node, bool setId)>();
            var dfsVisited = new HashSet<T>();
            dfsStack.Push((root, false));
            while (dfsStack.Count > 0)
            {
                var (node, setId) = dfsStack.Pop();
                if (setId)
                {
                    nodeId[node] = curId--;
                    continue;
                }

                if (dfsVisited.Contains(node))
                    continue;
                dfsVisited.Add(node);
                dfsStack.Push((node, true));
                foreach (var successor in node.Successors)
                    dfsStack.Push((successor, false));
            }

            return nodeId;
        }
    }
}