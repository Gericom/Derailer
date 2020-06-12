using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf;
using LibDerailer.IO.Elf.Dwarf2;
using LibDerailer.IR.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class TypeInfoTests
    {
        [TestMethod]
        public void Dwarf2Test()
        {
            var elf = new Elf(File.ReadAllBytes(@"TestData\Sphere.o"));
            var dwarf = new Dwarf2(elf);
            Assert.AreEqual(1, dwarf.RootDies.Length);
            Assert.IsInstanceOfType(dwarf.RootDies[0], typeof(Dwarf2CompileUnit));
            var vecFx32Die = ((Dwarf2CompileUnit) dwarf.RootDies[0]).Children.Find(child =>
                child is Dwarf2ClassType c && c.Name == "VecFx32");
            Assert.IsNotNull(vecFx32Die);
            Assert.AreEqual(3, vecFx32Die.Children.Count);
        }

        [TestMethod]
        public void Dwarf2ToIRTest()
        {
            var elf   = new Elf(File.ReadAllBytes(@"TestData\Sphere.o"));
            var dwarf = new Dwarf2(elf);
            var types = IRType.FromDwarf((Dwarf2CompileUnit) dwarf.RootDies[0]);
        }
    }
}
