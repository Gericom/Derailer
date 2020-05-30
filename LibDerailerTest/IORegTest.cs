using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class IORegTest
    {
        [TestMethod]
        public void CSVTest()
        {
            var map = new LibDerailer.Machine.MemoryMap(
                $"{Environment.GetEnvironmentVariable("NITROSDK3_ROOT")}\\build\\buildsetup\\ioreg\\io_register_list.csv");

            map.ToString();
        }
    }
}
