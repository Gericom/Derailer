using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf;

namespace LibDerailer.Compiler
{
    public class Compiler
    {
        public string ExePath { get; }

        public Compiler(string exePath)
        {
            ExePath = exePath;
        }

        public Elf Compile(string source, params string[] flags)
        {
            string tmpFile  = Path.GetTempFileName();
            string tmpFile2 = Path.GetTempFileName();
            File.WriteAllText(tmpFile, source, Encoding.ASCII);

            var p = new Process();
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ExePath);
            p.StartInfo.FileName         = ExePath;
            p.StartInfo.UseShellExecute  = false;
            p.StartInfo.Arguments        = $"{string.Join(" ", flags)} -o \"{tmpFile2}\" \"{tmpFile}\"";
            p.StartInfo.RedirectStandardOutput = false;//true;
            p.Start();
            p.WaitForExit();
            File.Delete(tmpFile);
            if (p.ExitCode != 0)
                return null;
            var elf = new Elf(File.ReadAllBytes(tmpFile2));
            File.Delete(tmpFile2);
            return elf;
        }
    }
}