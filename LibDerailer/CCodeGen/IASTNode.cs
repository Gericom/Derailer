using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen
{
    public abstract class IASTNode
    {
        public abstract IEnumerable<CToken> ToTokens();
    }
}
