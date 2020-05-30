using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.Machine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class IORegTest
    {
        [TestMethod]
        public void CSVTest()
        {
            var map = new NitroMemoryMap(
                $"{Environment.GetEnvironmentVariable("NITROSDK3_ROOT")}\\build\\buildsetup\\ioreg\\io_register_list.csv");

            var dispcnt = map.GetRegister(0x04000000, 4);
            Assert.IsNotNull(dispcnt);
            Assert.AreEqual("DISPCNT", dispcnt.Name);
            Assert.AreEqual("REG_DISPCNT_OFFSET", map.GetRegisterOffsetDefineName(dispcnt));
            Assert.AreEqual("REG_DISPCNT_ADDR", map.GetRegisterAddressDefineName(dispcnt));
            Assert.AreEqual("reg_GX_DISPCNT", map.GetRegisterDefineName(dispcnt));

            var oField = dispcnt.GetField(31, 1);
            Assert.IsNotNull(oField);
            Assert.AreEqual("O", oField.Name);
            Assert.AreEqual("REG_GX_DISPCNT_O_SHIFT", map.GetRegisterFieldShiftDefineName(dispcnt, oField));
            Assert.AreEqual("REG_GX_DISPCNT_O_SIZE", map.GetRegisterFieldSizeDefineName(dispcnt, oField));
            Assert.AreEqual("REG_GX_DISPCNT_O_MASK", map.GetRegisterFieldMaskDefineName(dispcnt, oField));
        }
    }
}