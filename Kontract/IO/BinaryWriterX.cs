using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Kontract.IO
{
    public class BinaryWriterX : BinaryWriter
    {
        private int _nibble = -1;

        public ByteOrder ByteOrder { get; set; }

        public BinaryWriterX(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode)
        {
            ByteOrder = byteOrder;
        }

        // Parameters out of order with a default encoding of Unicode
        public BinaryWriterX(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(input, Encoding.Unicode, leaveOpen)
        {
            ByteOrder = byteOrder;
        }

        public void WriteStruct<T>(T item) => Write(item.StructToBytes(ByteOrder));
        
        public override void Write(short value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(int value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(long value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ushort value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(uint value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public override void Write(ulong value)
        {
            if (ByteOrder == ByteOrder.LittleEndian)
                base.Write(value);
            else
                base.Write(BitConverter.GetBytes(value).Reverse().ToArray());
        }

        public void WriteNibble(int val)
        {
            val &= 15;
            if (_nibble == -1)
                _nibble = val;
            else
            {
                Write((byte)(_nibble + 16 * val));
                _nibble = -1;
            }
        }
    }
}