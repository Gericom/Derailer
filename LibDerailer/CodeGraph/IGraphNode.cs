using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public interface IGraphNode<T>
    {
        List<T> Predecessors { get; }
        List<T> Successors   { get; }
    }
}
