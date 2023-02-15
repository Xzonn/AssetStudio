using System.IO;
using System.Text;

namespace AssetStudio
{
    unsafe public class EndianBinaryWriter : BinaryWriter
    {
        public EndianType Endian;
        public EndianBinaryWriter(Stream stream, EndianType endian = EndianType.BigEndian) : base(stream)
        {
            Endian = endian;
        }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public void WriteBE(short value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(short value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(ushort value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(ushort value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(int value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(int value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(uint value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(uint value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(long value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(long value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(ulong value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(ulong value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(float value)
        {
            uint num = *(uint*)&value;
            var _buffer = new byte[] {
                (byte)(num >> 24),
                (byte)(num >> 16),
                (byte)(num >> 8),
                (byte)num
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(float value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteBE(double value)
        {
            ulong num = *(ulong*)&value;
            var _buffer = new byte[] {
                (byte)(num >> 56),
                (byte)(num >> 48),
                (byte)(num >> 40),
                (byte)(num >> 32),
                (byte)(num >> 24),
                (byte)(num >> 16),
                (byte)(num >> 8),
                (byte)num
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(double value)
        {
            if (Endian == EndianType.BigEndian)
            {
                WriteBE(value);
            }
            else
            {
                base.Write(value);
            }
        }

        public void WriteAlignedString(string value)
        {
            Write(value.Length);
            var bytes = Encoding.UTF8.GetBytes(value);
            OutStream.Write(bytes, 0, bytes.Length);
            this.AlignStream(4);
        }

        public void WriteStringToNull(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            OutStream.Write(bytes, 0, bytes.Length);
            OutStream.WriteByte(0);
        }

        public void WriteArray(int[] value)
        {
            Write(value.Length);
            foreach (var _ in value)
            {
                Write(_);
            }
        }
    }
}