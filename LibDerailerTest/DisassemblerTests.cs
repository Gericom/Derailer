﻿using System;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph;
using LibDerailer.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class DisassemblerTests
    {
        [TestMethod]
        public void DisassembleArmTest()
        {
            var code = new byte[]
            {
                0x24, 0x10, 0x9F, 0xE5, 0x00, 0x30, 0xA0, 0xE3, 0x00, 0x20, 0x91, 0xE5,
                0xA8, 0x10, 0x92, 0xE5, 0x01, 0x00, 0x50, 0xE1, 0x02, 0x00, 0x00, 0x3A,
                0xAC, 0x10, 0x92, 0xE5, 0x01, 0x00, 0x50, 0xE1, 0x01, 0x30, 0xA0, 0x93,
                0x03, 0x00, 0xA0, 0xE1, 0x1E, 0xFF, 0x2F, 0xE1, 0x10, 0xAA, 0x17, 0x02
            };
            var func = Decompiler.DisassembleArm(code, 0x02059C04, ArmDisassembleMode.Arm);
            Assert.AreEqual(4, func.BasicBlocks.Count);
        }

        [TestMethod]
        public void DisassembleArmStackTest()
        {
            var code = new byte[]
            {
                0x70, 0x40, 0x2D, 0xE9, 0x10, 0xD0, 0x4D, 0xE2, 0x00, 0x60, 0xA0, 0xE1,
                0x20, 0x00, 0x96, 0xE5, 0x01, 0x50, 0xA0, 0xE1, 0x00, 0x00, 0x90, 0xE5,
                0x02, 0x40, 0xA0, 0xE1, 0x00, 0x00, 0x90, 0xE5, 0x00, 0x0E, 0xA0, 0xE1,
                0x20, 0x0E, 0xA0, 0xE1, 0x06, 0x00, 0x40, 0xE2, 0x01, 0x00, 0x50, 0xE3,
                0x10, 0xD0, 0x8D, 0x82, 0x70, 0x40, 0xBD, 0x88, 0x1E, 0xFF, 0x2F, 0x81,
                0x00, 0x20, 0x94, 0xE5, 0x00, 0x10, 0x95, 0xE5, 0x05, 0x00, 0xA0, 0xE1,
                0x01, 0x10, 0x82, 0xE0, 0xA1, 0x1F, 0x81, 0xE0, 0xC1, 0x10, 0xA0, 0xE1,
                0x28, 0x10, 0x86, 0xE5, 0x04, 0x30, 0x94, 0xE5, 0x04, 0x20, 0x95, 0xE5,
                0x04, 0x10, 0xA0, 0xE1, 0x02, 0x20, 0x83, 0xE0, 0xA2, 0x2F, 0x82, 0xE0,
                0xC2, 0x20, 0xA0, 0xE1, 0x2C, 0x20, 0x86, 0xE5, 0x08, 0x30, 0x94, 0xE5,
                0x08, 0x20, 0x95, 0xE5, 0x02, 0x20, 0x83, 0xE0, 0xA2, 0x2F, 0x82, 0xE0,
                0xC2, 0x20, 0xA0, 0xE1, 0x30, 0x20, 0x86, 0xE5, 0xCA, 0xBB, 0x04, 0xEB,
                0xA0, 0x0F, 0x80, 0xE0, 0xC0, 0x00, 0xA0, 0xE1, 0x60, 0x00, 0x86, 0xE5,
                0x00, 0x20, 0x94, 0xE5, 0x00, 0x10, 0x95, 0xE5, 0x00, 0x00, 0x8D, 0xE2,
                0x01, 0x10, 0x42, 0xE0, 0x00, 0x10, 0x8D, 0xE5, 0x04, 0x30, 0x94, 0xE5,
                0x04, 0x20, 0x95, 0xE5, 0x00, 0x10, 0xA0, 0xE1, 0x02, 0x20, 0x43, 0xE0,
                0x04, 0x20, 0x8D, 0xE5, 0x08, 0x30, 0x94, 0xE5, 0x08, 0x20, 0x95, 0xE5,
                0x02, 0x20, 0x43, 0xE0, 0x08, 0x20, 0x8D, 0xE5, 0x47, 0xBC, 0x04, 0xEB,
                0x00, 0x00, 0x9D, 0xE5, 0xB0, 0x05, 0xC6, 0xE1, 0x04, 0x00, 0x9D, 0xE5,
                0xB2, 0x05, 0xC6, 0xE1, 0x08, 0x00, 0x9D, 0xE5, 0xB4, 0x05, 0xC6, 0xE1,
                0x10, 0xD0, 0x8D, 0xE2, 0x70, 0x40, 0xBD, 0xE8, 0x1E, 0xFF, 0x2F, 0xE1
            };

            var func = Decompiler.DisassembleArm(code, 0x02018F94, ArmDisassembleMode.Arm);
            //Assert.AreEqual(3, func.BasicBlocks.Count);
        }

        [TestMethod]
        public void DeschedulingTest()
        {
            var code = new byte[]
            {
                0x20, 0x20, 0x9F, 0xE5, 0x00, 0x10, 0x92, 0xE5, 0x01, 0x19, 0x11, 0xE2,
                0x00, 0x10, 0x92, 0x05, 0x00, 0x00, 0xE0, 0x13, 0x02, 0x1A, 0x01, 0x02,
                0xA1, 0x16, 0xA0, 0x01, 0x00, 0x10, 0x80, 0x05, 0x00, 0x00, 0xA0, 0x03,
                0x1E, 0xFF, 0x2F, 0xE1, 0x00, 0x06, 0x00, 0x04
            };
            var func = Decompiler.DisassembleArm(code, 0x0214A5F0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void DeschedulingTest2()
        {
            var code = new uint[]
            {
                0xE92D4FF0, 0xE24DD004, 0xE5913008, 0xE5911000, 0xE1D340B0, 0xE1D172B2, 0xE1D310B8, 0xE1A0A2C4,
                0xE1A09544, 0xE1A01E81, 0xE1B01FA1, 0xE5D31004, 0xE1A062C7, 0xE1A08547, 0xE1A05000, 0xE207701F,
                0xE206601F, 0xE5D33005, 0xE208001F, 0x01871286, 0x01810500, 0xE204401F, 0xE20AA01F, 0xE209901F,
                0x028DD004, 0x01C503B6, 0x08BD4FF0, 0x012FFF1E, 0xE0428001, 0xE0400009, 0xE0000098, 0xE043B001,
                0xE1A0100B, 0xEB000000, 0xE0471004, 0xE1A07000, 0xE0000198, 0xE1A0100B, 0xEB000000, 0xE046100A,
                0xE1A06000, 0xE0000198, 0xE1A0100B, 0xEB000000, 0xE0841006, 0xE08A0000, 0xE0892007, 0xE1810280,
                0xE1800502, 0xE1C503B6, 0xE28DD004, 0xE8BD4FF0, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SwitchTest()
        {
            var code = new[]
            {
                0xE240100A, 0xE3510005, 0x908FF101, 0xE12FFF1E, 0xEA000004, 0xEA000005, 0xEA000006, 0xEA000007,
                0xEA000008, 0xEA000009, 0xE3A00000, 0xE12FFF1E, 0xE3A00001, 0xE12FFF1E, 0xE3A00004, 0xE12FFF1E,
                0xE3A00009, 0xE12FFF1E, 0xE3E00000, 0xE12FFF1E, 0xE3A0005A, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void FxMulTest()
        {
            var code = new uint[]
            {
                0xE0C12190, 0xE3A00B02, 0xE0920000, 0xE2A11000, 0xE1A00620, 0xE1800A01, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void DoubleFxMulTest()
        {
            var code = new[]
            {
                0xE0C13190, 0xE3A00B02, 0xE0933000, 0xE2A11000, 0xE1A03623, 0xE1833A01, 0xE0C12293, 0xE0920000,
                0xE2A11000, 0xE1A00620, 0xE1800A01, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void ConstDivTest()
        {
            var code = new[]
            {
                0xE59F3028, 0xE1A01FA0, 0xE0CC2093, 0xE080C00C, 0xE1A0C2CC, 0xE081C00C, 0xE59F3014, 0xE1A01FAC,
                0xE0C02C93, 0xE1A000C0, 0xE0810000, 0xE12FFF1E, 0xEA0EA0EB, 0x66666667u
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SDiv2Test()
        {
            var code = new uint[]
            {
                0xE0800FA0, 0xE1A000C0, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SDiv3Test()
        {
            var code = new uint[]
            {
                0xE59F200C, 0xE0C31092, 0xE0833FA0, 0xE1A00003, 0xE12FFF1E, 0x55555556
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SDiv5Test()
        {
            var code = new uint[]
            {
                0xE59F3010, 0xE1A01FA0, 0xE0C02093, 0xE1A000C0, 0xE0810000, 0xE12FFF1E, 0x66666667
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SDiv35Test()
        {
            var code = new uint[]
            {
                0xE59F3018, 0xE1A01FA0, 0xE0CC2093, 0xE080C00C, 0xE1A0C2CC, 0xE081C00C, 0xE1A0000C, 0xE12FFF1E,
                0xEA0EA0EB
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void FxMulTest2()
        {
            var code = new byte[]
            {
                0x74, 0x30, 0x93, 0xE5, 0x00, 0x20, 0x90, 0xE5, 0x02, 0x01, 0x53, 0xE3,
                0x03, 0x20, 0xA0, 0x11, 0xB6, 0x30, 0xD0, 0xE1, 0x03, 0x3F, 0xA0, 0xE1,
                0x23, 0x3F, 0xB0, 0xE1, 0x02, 0x00, 0x00, 0x0A, 0x01, 0x00, 0x53, 0xE3,
                0x15, 0x00, 0x00, 0x0A, 0x1E, 0xFF, 0x2F, 0xE1, 0x3C, 0x30, 0x91, 0xE5,
                0x02, 0x00, 0x53, 0xE1, 0x07, 0x00, 0x00, 0xAA, 0x0C, 0x00, 0x91, 0xE5,
                0x00, 0x00, 0x83, 0xE0, 0x02, 0x00, 0x50, 0xE1, 0x03, 0x00, 0x42, 0xC0,
                0x0C, 0x00, 0x81, 0xC5, 0xB4, 0x02, 0xD1, 0xC1, 0xB6, 0x02, 0xC1, 0xC1,
                0x1E, 0xFF, 0x2F, 0xC1, 0x02, 0x00, 0x53, 0xE1, 0x1E, 0xFF, 0x2F, 0xB1,
                0x0C, 0x00, 0x91, 0xE5, 0x00, 0x00, 0x83, 0xE0, 0x02, 0x00, 0x50, 0xE1,
                0x03, 0x00, 0x42, 0xB0, 0x0C, 0x00, 0x81, 0xB5, 0xB4, 0x02, 0xD1, 0xB1,
                0xB6, 0x02, 0xC1, 0xB1, 0x1E, 0xFF, 0x2F, 0xE1, 0x3C, 0xC0, 0x91, 0xE5,
                0x02, 0x00, 0x5C, 0xE1, 0x10, 0x00, 0x00, 0xAA, 0x0C, 0x30, 0x91, 0xE5,
                0x03, 0x30, 0x8C, 0xE0, 0x02, 0x00, 0x53, 0xE1, 0x0C, 0x00, 0x00, 0xDA,
                0x0C, 0x20, 0x42, 0xE0, 0x0C, 0x20, 0x81, 0xE5, 0xF4, 0x20, 0xD0, 0xE1,
                0x18, 0x30, 0x91, 0xE5, 0x02, 0x0B, 0xA0, 0xE3, 0x93, 0xC2, 0xC2, 0xE0,
                0x00, 0x30, 0x9C, 0xE0, 0x00, 0x00, 0xA2, 0xE2, 0x23, 0x26, 0xA0, 0xE1,
                0x00, 0x2A, 0x82, 0xE1, 0x00, 0x00, 0x62, 0xE2, 0x18, 0x00, 0x81, 0xE5,
                0x1E, 0xFF, 0x2F, 0xE1, 0x02, 0x00, 0x5C, 0xE1, 0x1E, 0xFF, 0x2F, 0xB1,
                0x0C, 0x30, 0x91, 0xE5, 0x03, 0x30, 0x8C, 0xE0, 0x02, 0x00, 0x53, 0xE1,
                0x1E, 0xFF, 0x2F, 0xA1, 0x0C, 0x20, 0x42, 0xE0, 0x0C, 0x20, 0x81, 0xE5,
                0xF4, 0x20, 0xD0, 0xE1, 0x18, 0x30, 0x91, 0xE5, 0x02, 0x0B, 0xA0, 0xE3,
                0x93, 0xC2, 0xC2, 0xE0, 0x00, 0x30, 0x9C, 0xE0, 0x00, 0x00, 0xA2, 0xE2,
                0x23, 0x26, 0xA0, 0xE1, 0x00, 0x2A, 0x82, 0xE1, 0x00, 0x00, 0x62, 0xE2,
                0x18, 0x00, 0x81, 0xE5, 0x1E, 0xFF, 0x2F, 0xE1
            };
            var func = Decompiler.DisassembleArm(code, 0x0201E054, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void ForLoopTest()
        {
            var code = new[]
            {
                0xE3A02000, 0xE1A01002, 0xE3500000, 0x9A000005, 0xE3520005, 0x02822001u, 0xE2811001, 0xE1510000,
                0xE2822001, 0x3AFFFFF9u, 0xE1A00002, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void StackArgFuncTest()
        {
            var code = new[]
            {
                0xE59D1000, 0xE59D0004, 0xE0810000, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void CallStackArgFuncTest()
        {
            var code = new[]
            {
                0xE92D4000, 0xE24DD00C, 0xE58D0000, 0xE3A00000, 0xE58D1004, 0xE1A01000, 0xE1A02000, 0xE1A03000,
                0xEB000000, 0xE28DD00C, 0xE8BD4000, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void LoopIfTest()
        {
            var code = new byte[]
            {
                0xF0, 0x47, 0x2D, 0xE9, 0x00, 0x80, 0xA0, 0xE1, 0x04, 0x70, 0x98, 0xE5,
                0x00, 0x00, 0x57, 0xE3, 0x3F, 0x00, 0x00, 0x0A, 0x04, 0xA0, 0x88, 0xE2,
                0x10, 0x90, 0x88, 0xE2, 0x00, 0x40, 0xA0, 0xE3, 0x24, 0x20, 0x97, 0xE5,
                0x20, 0x10, 0x97, 0xE5, 0x82, 0x0D, 0xA0, 0xE1, 0xA0, 0x0F, 0xB0, 0xE1,
                0x00, 0x50, 0x91, 0xE5, 0x00, 0x60, 0x97, 0xE5, 0x05, 0x00, 0x00, 0x1A,
                0xBC, 0x14, 0xD7, 0xE1, 0xB2, 0x03, 0xD5, 0xE1, 0x00, 0x00, 0x51, 0xE1,
                0x10, 0x00, 0x82, 0x23, 0x24, 0x00, 0x87, 0x25, 0xBC, 0x44, 0xC7, 0x21,
                0x24, 0x00, 0x97, 0xE5, 0x80, 0x0E, 0xA0, 0xE1, 0xA0, 0x0F, 0xB0, 0xE1,
                0x0A, 0x00, 0x00, 0x1A, 0x80, 0x00, 0x97, 0xE5, 0x80, 0x06, 0xA0, 0xE1,
                0xA0, 0x0E, 0xB0, 0xE1, 0x03, 0x00, 0x00, 0x0A, 0xB8, 0x14, 0xD8, 0xE1,
                0x01, 0x00, 0x40, 0xE2, 0x00, 0x00, 0x51, 0xE1, 0x02, 0x00, 0x00, 0x1A,
                0x08, 0x00, 0xA0, 0xE1, 0x07, 0x10, 0xA0, 0xE1, 0xC4, 0x02, 0x00, 0xEB,
                0x00, 0x00, 0x95, 0xE5, 0x80, 0x08, 0xA0, 0xE1, 0xA0, 0x0F, 0xB0, 0xE1,
                0x09, 0x00, 0x00, 0x0A, 0xBC, 0x13, 0xD5, 0xE1, 0x00, 0x00, 0x51, 0xE3,
                0x06, 0x00, 0x00, 0x0A, 0x24, 0x00, 0x97, 0xE5, 0x80, 0x0D, 0xA0, 0xE1,
                0xA0, 0x0F, 0xB0, 0xE1, 0x02, 0x00, 0x00, 0x0A, 0xBC, 0x04, 0xD7, 0xE1,
                0x01, 0x00, 0x50, 0xE1, 0x03, 0x00, 0x00, 0x8A, 0x24, 0x00, 0x97, 0xE5,
                0x80, 0x0F, 0xA0, 0xE1, 0xA0, 0x0F, 0xB0, 0xE1, 0x0B, 0x00, 0x00, 0x0A,
                0x0C, 0x00, 0x97, 0xE5, 0x00, 0x00, 0x50, 0xE3, 0x08, 0x00, 0x00, 0x1A,
                0x18, 0x00, 0x97, 0xE5, 0x00, 0x00, 0x50, 0xE3, 0x05, 0x00, 0x00, 0x1A,
                0x0A, 0x00, 0xA0, 0xE1, 0x07, 0x10, 0xA0, 0xE1, 0xE3, 0x16, 0x00, 0xEB,
                0x00, 0x10, 0xA0, 0xE1, 0x09, 0x00, 0xA0, 0xE1, 0x13, 0x17, 0x00, 0xEB,
                0x06, 0x70, 0xA0, 0xE1, 0x00, 0x00, 0x56, 0xE3, 0xC2, 0xFF, 0xFF, 0x1A,
                0xB8, 0x04, 0xD8, 0xE1, 0x01, 0x00, 0x80, 0xE2, 0xB8, 0x04, 0xC8, 0xE1,
                0xB8, 0x04, 0xD8, 0xE1, 0x01, 0x00, 0x50, 0xE3, 0x00, 0x00, 0xA0, 0x83,
                0xB8, 0x04, 0xC8, 0x81, 0xF0, 0x47, 0xBD, 0xE8, 0x1E, 0xFF, 0x2F, 0xE1
            };
            var func = Decompiler.DisassembleArm(code, 0x0201873C, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void IfOrTest()
        {
            var code = new uint[]
            {
                0xE3500000, 0x1A000005, 0xE3510000, 0x1A000003, 0xE3520000, 0x1A000001, 0xE3530000, 0x0A000001,
                0xE3A00005, 0xEA000000, 0xE3A00001, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void IfOrTestTrinaryOp()
        {
            var code = new uint[]
            {
                0xE3500000, 0x1A000005, 0xE3510003, 0x11A02003, 0xE3520000, 0x1A000001, 0xE3530000, 0x0A000001,
                0xE3A00005, 0xEA000000, 0xE3A00001, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void IfAndTest()
        {
            var code = new uint[]
            {
                0xE3500000, 0x0A000006, 0xE3510000, 0x0A000004, 0xE3520000, 0x0A000002, 0xE3530000, 0x13A00005,
                0x1A000000, 0xE3A00001, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void MiniSwitchTest()
        {
            var code = new uint[]
            {
                0xE3500000, 0x0A000004, 0xE3500001, 0x0A000004, 0xE3500002, 0x0A000004, 0xEA000005, 0xE3A00005,
                0xEA000004, 0xE3A00009, 0xEA000002, 0xE3A00001, 0xEA000000, 0xE3A00004, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void IfOrAndOrTest()
        {
            var code = new uint[]
            {
                0xE3500000, 0x1A000001, 0xE3510000, 0x0A000001, 0xE3520000, 0x1A000001, 0xE3530000, 0x0A000001,
                0xE3A00005, 0xEA000000, 0xE3A00001, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void IfAndOrAndTest()
        {
            var code = new uint[]
            {
                0xE3500000, 0x0A000001, 0xE3510000, 0x1A000001, 0xE3520000, 0x0A000002, 0xE3530000, 0x13A00005,
                0x1A000000, 0xE3A00001, 0xE2800001, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void LoopTest2()
        {
            var code = new byte[]
            {
                0x30, 0x40, 0x2D, 0xE9, 0x04, 0xD0, 0x4D, 0xE2, 0x10, 0x50, 0x91, 0xE5,
                0x00, 0x40, 0xA0, 0xE3, 0x08, 0x30, 0xD5, 0xE5, 0x00, 0x00, 0x53, 0xE3,
                0x04, 0xD0, 0x8D, 0xD2, 0x30, 0x40, 0xBD, 0xD8, 0x1E, 0xFF, 0x2F, 0xD1,
                0x09, 0xC0, 0xD5, 0xE5, 0x04, 0xE0, 0xA0, 0xE1, 0x0C, 0x10, 0x8E, 0xE0,
                0x01, 0x00, 0x52, 0xE1, 0x04, 0x10, 0xD5, 0xB7, 0x04, 0xD0, 0x8D, 0xB2,
                0x2C, 0x10, 0xC0, 0xB5, 0x30, 0x40, 0xBD, 0xB8, 0x1E, 0xFF, 0x2F, 0xB1,
                0x01, 0x40, 0x84, 0xE2, 0x03, 0x00, 0x54, 0xE1, 0x0C, 0xE0, 0x8E, 0xE0,
                0xF4, 0xFF, 0xFF, 0xBA, 0x04, 0xD0, 0x8D, 0xE2, 0x30, 0x40, 0xBD, 0xE8,
                0x1E, 0xFF, 0x2F, 0xE1
            };
            var func = Decompiler.DisassembleArm(code, 0x0201DC24, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void DoubleLoopTest()
        {
            var code = new uint[]
            {
                0xE92D4000, 0xE24DD004, 0xE3A0E000, 0xE1A0C00E, 0xE3500000, 0xDA00000B, 0xE1A0200E, 0xE1A03002,
                0xE3510000, 0xDA000003, 0xE2833001, 0xE1530001, 0xE08EE000, 0xBAFFFFFB, 0xE28CC001, 0xE15C0000,
                0xE08EE001, 0xBAFFFFF4, 0xE28E0004, 0xE28DD004, 0xE8BD4000, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void BreakContinueTest()
        {
            var code = new uint[]
            {
                0xE3A03000, 0xE1A02003, 0xE3500000, 0xDA000008, 0xE3530005, 0x02833001, 0xE1530001, 0x0A000004,
                0xE2822001, 0xE3530009, 0x12833001, 0xE1520000, 0xBAFFFFF6, 0xE1A00003, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void ContinueTest()
        {
            var code = new uint[]
            {
                0xE3A02000, 0xE1A01002, 0xE3500000, 0xDA000008, 0xE3520005, 0x02822001, 0xE3520009, 0x0A000001,
                0xE3520013, 0x12822001, 0xE2811001, 0xE1510000, 0xBAFFFFF6, 0xE1A00002, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void DehoistTest()
        {
            var code = new uint[]
            {
                0xE3A03000, 0xE1A02003, 0xE3500000, 0xDA000005, 0xE1A01003, 0xE2822001, 0xE0833001, 0xE1520000,
                0xE2811006, 0xBAFFFFFA, 0xE1A00003, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void NoInitialTestLoopTest()
        {
            var code = new byte[]
            {
                0xF0, 0x43, 0x2D, 0xE9, 0x04, 0xD0, 0x4D, 0xE2, 0x9C, 0x20, 0x9F, 0xE5,
                0xB4, 0x37, 0xD0, 0xE1, 0x00, 0x20, 0x92, 0xE5, 0x58, 0x00, 0xA0, 0xE3,
                0x93, 0x20, 0x27, 0xE0, 0x01, 0x90, 0xA0, 0xE1, 0x00, 0x80, 0xA0, 0xE3,
                0x08, 0x50, 0xA0, 0xE1, 0x01, 0x60, 0xA0, 0xE3, 0x00, 0x40, 0xE0, 0xE3,
                0x88, 0x11, 0x87, 0xE0, 0x10, 0x00, 0x91, 0xE5, 0x00, 0x00, 0x50, 0xE3,
                0x02, 0x00, 0x00, 0x1A, 0x14, 0x10, 0x91, 0xE5, 0x00, 0x00, 0x51, 0xE3,
                0x0F, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x59, 0xE3, 0x99, 0x60, 0xC0, 0x05,
                0x88, 0x01, 0x87, 0x00, 0x14, 0x00, 0x90, 0x05, 0x99, 0x60, 0xC0, 0x05,
                0x03, 0x00, 0x00, 0x0A, 0x5F, 0x1F, 0x00, 0xEB, 0x88, 0x01, 0x87, 0xE0,
                0x14, 0x00, 0x90, 0xE5, 0x5C, 0x1F, 0x00, 0xEB, 0x88, 0x21, 0x87, 0xE0,
                0x14, 0x50, 0x82, 0xE5, 0x14, 0x10, 0x92, 0xE5, 0x08, 0x01, 0x87, 0xE0,
                0x10, 0x10, 0x82, 0xE5, 0x04, 0x40, 0x80, 0xE5, 0x01, 0x00, 0x88, 0xE2,
                0x00, 0x08, 0xA0, 0xE1, 0x20, 0x88, 0xA0, 0xE1, 0x02, 0x00, 0x58, 0xE3,
                0xE3, 0xFF, 0xFF, 0x3A, 0x04, 0xD0, 0x8D, 0xE2, 0xF0, 0x43, 0xBD, 0xE8,
                0x1E, 0xFF, 0x2F, 0xE1, 0x84, 0xAE, 0x17, 0x02
            };
            var func = Decompiler.DisassembleArm(code, 0x02083928, ArmDisassembleMode.Arm);
        }

        public void MiniSwitchTest2()
        {
            var code = new uint[]
            {
                0xE3500000, // cmp r0,#0
                0x0A000004, // beq *+24 ; 0x0000001c
                0xE3500001, // cmp r0,#1
                0x0A000004, // beq *+24 ; 0x00000024
                0xE3500002, // cmp r0,#2
                0x0A000004, // beq *+24 ; 0x0000002c
                0xEA000005, // b *+28 ; 0x00000034
                0xE3A00005, // mov r0,#5
                0xEA000004, // b *+24 ; 0x00000038
                0xE3A00009, // mov r0,#9
                0xEA000002, // b *+16 ; 0x00000038
                0xE3A00001, // mov r0,#1
                0xEA000000, // b *+8
                0xE3A00004, // mov r0,#4
                0xE2800001, // add r0,r0,#1
                0xE12FFF1E  // bx lr
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void MiniSwitchTest3()
        {
            /*
            int foo(int rv0) {
                int rv9 = 2;
                switch (rv0)
                {
                    case 0:  rv9 = 0x5; break;
                    case 1:  rv9 = 0x9;
                    case 2:  rv9 = rv9 + 3; break;
                    default: rv9 = 0x4; break;
                }
                return rv9 + 0x1;
            }
            */
            var code = new uint[]
            {
                0xE3500000, // cmp r0,#0
                0xE3A01002, // mov r1,#2
                0x0A000004, // beq *+24 ; 0x00000020
                0xE3500001, // cmp r0,#1
                0x0A000004, // beq *+24 ; 0x00000028
                0xE3500002, // cmp r0,#2
                0x0A000003, // beq *+20 ; 0x0000002c
                0xEA000004, // b *+24 ; 0x00000034
                0xE3A01005, // mov r1,#5
                0xEA000003, // b *+20 ; 0x00000038
                0xE3A01009, // mov r1,#9
                0xE2811003, // add r1,r1,#3
                0xEA000000, // b *+8
                0xE3A01004, // mov r1,#4
                0xE2810001, // add r0,r1,#1
                0xE12FFF1E  // bx lr
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void UnstructuredLoopSwitchTest()
        {
            /*
             int bar(int x, int* y, int *z) {
                int i;
                for (i = 0; i < 10; i++) {
                switch (i) {
                    case 1: continue;
                    case 2: *y = i; break;
                    case 3: *y = i + 1;
                }

                *z++;
                }
            }
             */
            var code = new uint[]
            {
                0xE3A0C000, // mov r12,#0
                0xE35C0001, // cmp r12,#1
                0x0A000007, // beq *+36 ; 0x0000002c
                0xE35C0002, // cmp r12,#2
                0x0A000003, // beq *+20 ; 0x00000024
                0xE35C0003, // cmp r12,#3
                0x028C3001, // addeq r3,r12,#1
                0x05813000, // streq r3,[r1]
                0xEA000000, // b *+8
                0xE581C000, // str r12,[r1]
                0xE2822004, // add r2,r2,#4
                0xE28CC001, // add r12,r12,#1
                0xE35C000A, // cmp r12,#10
                0xBAFFFFF2, // blt *-48 ; 0x00000004
                0xE12FFF1E  // bx lr
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void SwitchBinarySearchTest()
        {
            /* int binarySwitch(int x) {
                switch(x) {
                    case 3: return 4;
                    case 6: return 9;
                    case 1: return 3;
                    case 200: return 10;
                    case 201: return 6;
                }
            }*/
            var code = new uint[]
            {
                0xE35000C8, // cmp r0,#200
                0xCA00000C, // bgt * +56; 0x0000003c
                0xE35000C8, // cmp r0,#200
                0xAA000011, // bge * +76; 0x00000058
                0xE3500006, // cmp r0,#6
                0xC12FFF1E, // bxgt lr
                0xE3500001, // cmp r0,#1
                0xB12FFF1E, // bxlt lr
                0xE3500001, // cmp r0,#1
                0x0A000009, // beq * +44; 0x00000050
                0xE3500003, // cmp r0,#3
                0x0A000005, // beq * +28; 0x00000048
                0xE3500006, // cmp r0,#6
                0x03A00009, // moveq r0,#9
                0xE12FFF1E, // bx lr
                0xE35000C9, // cmp r0,#201
                0x03A00006, // moveq r0,#6
                0xE12FFF1E, // bx lr
                0xE3A00004, // mov r0,#4
                0xE12FFF1E, // bx lr
                0xE3A00003, // mov r0,#3
                0xE12FFF1E, // bx lr
                0xE3A0000A, // mov r0,#10
                0xE12FFF1E  // bx lr
            };

            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void Add6464Test()
        {
            var code = new uint[]
            {
                0xE0900002, 0xE0A11003, 0xE12FFF1E
            };
            var func = Decompiler.DisassembleArm(InstructionWordsToBytes(code), 0, ArmDisassembleMode.Arm);
        }

        [TestMethod]
        public void ThumbBXR3Test()
        {
            var code = new byte[]
            {
                0xF0, 0xB5, 0x83, 0xB0, 0x21, 0x49, 0x09, 0x68, 0xCB, 0x6C, 0x0C, 0x22,
                0x50, 0x43, 0x18, 0x18, 0x01, 0x90, 0x00, 0x25, 0x00, 0x95, 0x2E, 0x00,
                0x2B, 0x00, 0x40, 0x88, 0x00, 0x28, 0x0F, 0xD9, 0x01, 0x9A, 0x12, 0x88,
                0x49, 0x6C, 0x24, 0x27, 0xD4, 0x18, 0x24, 0x04, 0x24, 0x0C, 0x7C, 0x43,
                0x0C, 0x19, 0xA4, 0x69, 0x36, 0x19, 0x5B, 0x1C, 0x1B, 0x04, 0x1B, 0x0C,
                0x83, 0x42, 0xF3, 0xD3, 0x00, 0x24, 0x27, 0x1C, 0x01, 0x98, 0x01, 0x19,
                0x04, 0x20, 0x09, 0x56, 0xF8, 0x43, 0x81, 0x42, 0x0B, 0xD0, 0x00, 0x29,
                0x09, 0xD0, 0x08, 0x04, 0x00, 0x0C, 0xFF, 0xF7, 0xD1, 0xFF, 0x00, 0x99,
                0x08, 0x18, 0x00, 0x90, 0x68, 0x1C, 0x00, 0x04, 0x05, 0x0C, 0x60, 0x1C,
                0x00, 0x04, 0x04, 0x0C, 0x03, 0x2C, 0xE7, 0xD3, 0x00, 0x2D, 0x04, 0xD0,
                0x00, 0x98, 0x29, 0x1C, 0xEF, 0xF0, 0xB8, 0xEF, 0x36, 0x18, 0x30, 0x1C,
                0x03, 0xB0, 0xF0, 0xBC, 0x08, 0xBC, 0x18, 0x47, 0x20, 0x56, 0x17, 0x02
            };
            var func = Decompiler.DisassembleArm(code, 0x02043078, ArmDisassembleMode.Thumb);
        }

        private static byte[] InstructionWordsToBytes(uint[] instructions)
        {
            var code = new byte[instructions.Length * 4];
            for (int i = 0; i < instructions.Length; i++)
                IOUtil.WriteU32Le(code, i * 4, instructions[i]);
            return code;
        }
    }
}