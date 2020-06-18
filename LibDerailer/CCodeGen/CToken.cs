using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen
{
    public struct CToken
    {
        public CTokenType  Type;
        public string      Data;

        public CToken(CTokenType type, string data)
        {
            Type = type;
            Data = data;
        }
    }
}
