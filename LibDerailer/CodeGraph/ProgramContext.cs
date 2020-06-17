using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class ProgramContext
    {
        public Dictionary<uint, string> SymbolMap { get; } = new Dictionary<uint, string>();

        public event Func<uint, string> ResolveSymbol;

        public string TryGetSymbol(uint address)
        {
            if (SymbolMap.ContainsKey(address))
                return SymbolMap[address];

            string symbol = ResolveSymbol?.Invoke(address);
            if (symbol != null)
                SymbolMap[address] = symbol;

            return symbol;
        }
    }
}