using System;

namespace LibDerailer.IO
{
    public class IOUtil
    {
        public static short ReadS16Le(byte[] data, int offset)
        {
            return (short) (data[offset] | (data[offset + 1] << 8));
        }

        public static short[] ReadS16SLe(byte[] data, int offset, int count)
        {
            var res = new short[count];
            for (int i = 0; i < count; i++)
                res[i] = ReadS16Le(data, offset + i * 2);
            return res;
        }

        public static short ReadS16Be(byte[] data, int offset)
        {
            return (short) ((data[offset] << 8) | data[offset + 1]);
        }

        public static void WriteS16Le(byte[] data, int offset, short value)
        {
            data[offset]     = (byte) (value & 0xFF);
            data[offset + 1] = (byte) ((value >> 8) & 0xFF);
        }

        public static void WriteS16SLe(byte[] data, int offset, short[] values)
        {
            for (int i = 0; i < values.Length; i++)
                WriteS16Le(data, offset + i * 2, values[i]);
        }

        public static ushort ReadU16Le(byte[] data, int offset)
        {
            return (ushort) (data[offset] | (data[offset + 1] << 8));
        }

        public static ushort[] ReadU16SLe(byte[] data, int offset, int count)
        {
            var res = new ushort[count];
            for (int i = 0; i < count; i++)
                res[i] = ReadU16Le(data, offset + i * 2);
            return res;
        }

        public static ushort ReadU16Be(byte[] data, int offset)
        {
            return (ushort) ((data[offset] << 8) | data[offset + 1]);
        }

        public static void WriteU16Le(byte[] data, int offset, ushort value)
        {
            data[offset]     = (byte) (value & 0xFF);
            data[offset + 1] = (byte) ((value >> 8) & 0xFF);
        }

        public static uint ReadU24Le(byte[] data, int offset)
        {
            return (uint) (data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16));
        }

        public static uint ReadU32Le(byte[] data, int offset)
        {
            return (uint) (data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) |
                           (data[offset + 3] << 24));
        }

        public static uint ReadU32Be(byte[] data, int offset)
        {
            return (uint) ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) |
                           data[offset + 3]);
        }

        public static void WriteU32Le(byte[] data, int offset, uint value)
        {
            data[offset]     = (byte) (value & 0xFF);
            data[offset + 1] = (byte) ((value >> 8) & 0xFF);
            data[offset + 2] = (byte) ((value >> 16) & 0xFF);
            data[offset + 3] = (byte) ((value >> 24) & 0xFF);
        }

        public static ulong ReadU64Le(byte[] data, int offset)
        {
            return (ulong) data[offset] | ((ulong) data[offset + 1] << 8) | ((ulong) data[offset + 2] << 16) |
                   ((ulong) data[offset + 3] << 24) | ((ulong) data[offset + 4] << 32) |
                   ((ulong) data[offset + 5] << 40) | ((ulong) data[offset + 6] << 48) |
                   ((ulong) data[offset + 7] << 56);
        }

        public static ulong ReadU64Be(byte[] data, int offset)
        {
            return ((ulong) data[offset] << 56) | ((ulong) data[offset + 1] << 48) | ((ulong) data[offset + 2] << 40) |
                   ((ulong) data[offset + 3] << 32) | ((ulong) data[offset + 4] << 24) |
                   ((ulong) data[offset + 5] << 16) | ((ulong) data[offset + 6] << 8) | ((ulong) data[offset + 7] << 0);
        }

        public static void WriteU64Le(byte[] data, int offset, ulong value)
        {
            data[offset]     = (byte) (value & 0xFF);
            data[offset + 1] = (byte) ((value >> 8) & 0xFF);
            data[offset + 2] = (byte) ((value >> 16) & 0xFF);
            data[offset + 3] = (byte) ((value >> 24) & 0xFF);
            data[offset + 4] = (byte) ((value >> 32) & 0xFF);
            data[offset + 5] = (byte) ((value >> 40) & 0xFF);
            data[offset + 6] = (byte) ((value >> 48) & 0xFF);
            data[offset + 7] = (byte) ((value >> 56) & 0xFF);
        }

        public static void WriteSingleLe(byte[] data, int offset, float value)
        {
            var a = BitConverter.GetBytes(value);
            data[0 + offset] = a[0];
            data[1 + offset] = a[1];
            data[2 + offset] = a[2];
            data[3 + offset] = a[3];
        }
    }
}