using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CodeGraph;
using Gee.External.Capstone.Arm;
using System.IO;

namespace Derailer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4 && args[0] != "Analyze-File")
                return;

            Console.WriteLine("Analyzing binary");

            var func = Decompiler.DisassembleArm(File.ReadAllBytes(args[1]), uint.Parse(args[3]), ArmDisassembleMode.Arm);

            File.WriteAllText(args[2], func.CachedMethod.ToString());
        }
    }
}
