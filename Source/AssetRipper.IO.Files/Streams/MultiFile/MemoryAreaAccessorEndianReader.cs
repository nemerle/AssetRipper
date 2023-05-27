using AssetRipper.IO.Endian;
using AssetRipper.IO.Files;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssetRipper.IO.Endian
{
    public interface IReadableMemoryArea<TSelf> where TSelf : IReadableMemoryArea<TSelf>
    {
        static abstract TSelf Read(MemoryAreaReader reader);
    }

    public class MemoryAreaReader
    {
        private MemoryAreaAccessor memoryAccessor;
        private bool isBigEndian = false;

        public EndianType EndianType
        {
            get => isBigEndian ? EndianType.BigEndian : EndianType.LittleEndian;
            set => isBigEndian = value == EndianType.BigEndian;
        }

        public bool IsAlignArray { get; }

        protected const int BufferSize = 4096;

        public MemoryAreaReader(MemoryAreaAccessor memoryAccessor, EndianType endianess, bool alignArray = false)
        {
            EndianType = endianess;
            this.memoryAccessor = memoryAccessor;
            IsAlignArray = alignArray;
        }

        public MemoryAreaAccessor Accessor => memoryAccessor;

        public string ReadStringZeroTerm()
        {
            if (ReadStringZeroTerm(BufferSize, out string? result))
            {
                return result;
            }

            throw new Exception("Can't find end of string");
        }

        public MemoryAreaAccessor takeView<T>(int count) where T : unmanaged
        {
            unsafe
            {
                if (Position + count * sizeof(T) > memoryAccessor.Length)
                {
                    throw new Exception("Can't read data");
                }

                var sub = memoryAccessor.CreateSubAccessor(0, count * sizeof(T));
                memoryAccessor.Position += count * sizeof(T);
                return sub;
            }
        }

        /// <summary>
        /// Read C like UTF8 format zero terminated string
        /// </summary>
        /// <param name="maxLength">Max allowed character count to read</param>
        /// <param name="result">Read string if found</param>
        /// <returns>Whether zero term has been found</returns>
        public bool ReadStringZeroTerm(int maxLength, [NotNullWhen(true)] out string? result)
        {
            // maxLength = Math.Min(maxLength, m_buffer.Length);
            Span<byte> buffer = (stackalloc byte[maxLength]);
            for (int i = 0; i < maxLength; i++)
            {
                byte bt = memoryAccessor.ReadByte();
                if (bt == 0)
                {
                    result = Encoding.UTF8.GetString(buffer[..i]);
                    return true;
                }

                buffer[i] = bt;
            }

            result = null;
            return false;
        }

        public T ReadEndian<T>() where T : IReadableMemoryArea<T>
        {
            return T.Read(this);
        }

        public T[] ReadEndianArray<T>() where T : IReadableMemoryArea<T>
        {
            int count = ReadInt32();
            ThrowIfNotEnoughSpaceForArray(count, sizeof(byte));
            T[] array = count == 0 ? Array.Empty<T>() : new T[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = T.Read(this);
            }

            if (IsAlignArray)
            {
                AlignStream();
            }

            return array;
        }

        public void AlignStream()
        {
            memoryAccessor.Position = (memoryAccessor.Position + 3) & ~3;
        }

        public void Align() => AlignStream();

        protected void ThrowIfNotEnoughSpaceForArray(int elementNumberToRead, int elementSize)
        {
            if (RemainingStreamBytes < elementNumberToRead * elementSize)
            {
                throw new Exception($"Stream only has {RemainingStreamBytes} bytes in the stream, so {elementNumberToRead} elements of size {elementSize} cannot be read.");
            }
        }

        protected long RemainingStreamBytes => memoryAccessor.Length - memoryAccessor.Position;
        public long Position => memoryAccessor.Position;
        public long Length => memoryAccessor.Length;

        public long ReadInt64()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadInt64BigEndian(memoryAccessor.ReadBytes(8));
            }

            return memoryAccessor.Read<long>();
        }

        public byte ReadByte() => memoryAccessor.ReadByte();
        public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

        public char ReadChar()
        {
            return (char)ReadUInt16();
        }

        public short ReadInt16()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadInt16BigEndian(memoryAccessor.ReadBytes(2));
            }
            else
            {
                return memoryAccessor.Read<short>();
            }
        }

        public ushort ReadUInt16()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(memoryAccessor.ReadBytes(2));
            }
            else
            {
                return memoryAccessor.Read<ushort>();
            }
        }

        public int ReadInt32()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadInt32BigEndian(memoryAccessor.ReadBytes(4));
            }

            return memoryAccessor.Read<int>();
        }

        public uint ReadUInt32()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(memoryAccessor.ReadBytes(4));
            }
            else
            {
                return memoryAccessor.Read<uint>();
            }
        }

        public ulong ReadUInt64()
        {
            if (isBigEndian)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(memoryAccessor.ReadBytes(8));
            }

            return memoryAccessor.Read<ulong>();
        }

        public Half ReadHalf()
        {
            ReadOnlySpan<byte> src = memoryAccessor.ReadBytes(2);
            Half result = isBigEndian ? BinaryPrimitives.ReadHalfBigEndian(src) : BinaryPrimitives.ReadHalfLittleEndian(src);
            return result;
        }

        public float ReadSingle()
        {
            ReadOnlySpan<byte> src = memoryAccessor.ReadBytes(4);
            float result = isBigEndian ? BinaryPrimitives.ReadSingleBigEndian(src) : BinaryPrimitives.ReadSingleLittleEndian(src);
            return result;
        }

        public double ReadDouble()
        {
            ReadOnlySpan<byte> src = memoryAccessor.ReadBytes(8);
            double result = isBigEndian ? BinaryPrimitives.ReadDoubleBigEndian(src) : BinaryPrimitives.ReadDoubleLittleEndian(src);
            return result;
        }

        public virtual string ReadString()
        {
            int length = ReadInt32();
            return ReadString(length);
        }

        public string ReadString(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot be negative.");
            }
            else if (length == 0)
            {
                return "";
            }
            else if (length > RemainingStreamBytes)
            {
                throw new EndOfStreamException($"Can't read {length}-byte string because there are only {RemainingStreamBytes} bytes left in the stream");
            }

            var buffer = Accessor.ReadBytes(length);
            if (buffer.Length != length)
            {
                throw new EndOfStreamException($"End of stream. Expected to read {length} bytes, but only read {buffer.Length} bytes.");
            }

            return Encoding.UTF8.GetString(buffer);
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public int[] ReadInt32Array(bool allowAlignment = true)
        {
            int count = ReadInt32();
            int index = 0;
            ThrowIfNotEnoughSpaceForArray(count, sizeof(int));
            int[] array = count == 0 ? Array.Empty<int>() : new int[count];
            while (index < count)
            {
                try
                {
                    array[index] = ReadInt32();
                }
                catch (Exception ex)
                {
                    throw new Exception($"End of stream. Read {index}, expected {count} elements", ex);
                }

                index++;
            }

            if (allowAlignment && IsAlignArray)
            {
                AlignStream();
            }

            return array;
        }

        public string[] ReadStringArray(bool allowAlignment = true)
        {
            int count = ReadInt32();
            ThrowIfNotEnoughSpaceForArray(count, sizeof(byte));
            string[] array = count == 0 ? Array.Empty<string>() : new string[count];
            for (int i = 0; i < count; i++)
            {
                string value = ReadString();
                array[i] = value;
            }

            if (allowAlignment && IsAlignArray)
            {
                AlignStream();
            }

            return array;
        }

        public byte[] ReadByteArray(bool allowAlignment = true)
        {
            int count = ReadInt32();
            ThrowIfNotEnoughSpaceForArray(count, sizeof(byte));
            var blob = Accessor.ReadBytes(count);
            if (allowAlignment && IsAlignArray)
            {
                AlignStream();
            }

            return blob.ToArray();
        }

        public Utf8String ReadUtf8String()
        {
            int length = ReadInt32();
            return new Utf8String(memoryAccessor.ReadBytes(length));
        }

        public Utf8String ReadUtf8StringAligned()
        {
            Utf8String result = ReadUtf8String();
            AlignStream(); //Alignment after strings has happened since 2.1.0
            return result;
        }

        public string[] ReadStringArray(UnityVersion version)
        {
            int count = ReadInt32();
            int index = 0;
            ThrowIfNotEnoughSpaceForArray(this, count, sizeof(int));
            string[] array = count == 0 ? Array.Empty<string>() : new string[count];
            while (index < count)
            {
                try
                {
                    array[index] = ReadUtf8StringAligned();
                }
                catch (Exception ex)
                {
                    throw new EndOfStreamException($"End of stream. Read {index}, expected {count} elements", ex);
                }

                index++;
            }

            if (IsAlignArrays(version))
            {
                AlignStream();
            }

            return array;
        }

        public T ReadPrimitive<T>() where T : unmanaged
        {
            if (typeof(T) == typeof(short))
            {
                short value = ReadInt16();
                return Unsafe.As<short, T>(ref value);
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort value = ReadUInt16();
                return Unsafe.As<ushort, T>(ref value);
            }
            else if (typeof(T) == typeof(int))
            {
                int value = ReadInt32();
                return Unsafe.As<int, T>(ref value);
            }
            else if (typeof(T) == typeof(uint))
            {
                uint value = ReadUInt32();
                return Unsafe.As<uint, T>(ref value);
            }
            else if (typeof(T) == typeof(long))
            {
                long value = ReadInt64();
                return Unsafe.As<long, T>(ref value);
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong value = ReadUInt64();
                return Unsafe.As<ulong, T>(ref value);
            }
            else if (typeof(T) == typeof(Half))
            {
                Half value = ReadHalf();
                return Unsafe.As<Half, T>(ref value);
            }
            else if (typeof(T) == typeof(float))
            {
                float value = ReadSingle();
                return Unsafe.As<float, T>(ref value);
            }
            else if (typeof(T) == typeof(double))
            {
                double value = ReadDouble();
                return Unsafe.As<double, T>(ref value);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool value = ReadBoolean();
                return Unsafe.As<bool, T>(ref value);
            }
            else if (typeof(T) == typeof(byte))
            {
                byte value = ReadByte();
                return Unsafe.As<byte, T>(ref value);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte value = ReadSByte();
                return Unsafe.As<sbyte, T>(ref value);
            }
            else if (typeof(T) == typeof(char))
            {
                char value = ReadChar();
                return Unsafe.As<char, T>(ref value);
            }

            return default; //Throwing an exception prevents method inlining.
        }

        public T[] ReadPrimitiveArray<T>(UnityVersion version) where T : unmanaged
        {
            int count = ReadInt32();
            int index = 0;
            ThrowIfNotEnoughSpaceForArray(this, count, Unsafe.SizeOf<T>());
            T[] array = count == 0 ? Array.Empty<T>() : new T[count];
            while (index < count)
            {
                try
                {
                    array[index] = ReadPrimitive<T>();
                }
                catch (Exception ex)
                {
                    throw new EndOfStreamException($"End of stream. Read {index}, expected {count} elements", ex);
                }

                index++;
            }

            if (IsAlignArrays(version))
            {
                AlignStream();
            }

            return array;
        }

        private static bool IsAlignArrays(UnityVersion version) => version.IsGreaterEqual(2017);

        private static void ThrowIfNotEnoughSpaceForArray(MemoryAreaReader reader, int elementNumberToRead, int elementSize)
        {
            long remainingBytes = reader.memoryAccessor.Length - reader.memoryAccessor.Position;
            if (remainingBytes < elementNumberToRead * elementSize)
            {
                throw new EndOfStreamException($"Stream only has {remainingBytes} bytes in the stream, so {elementNumberToRead} elements of size {elementSize} cannot be read.");
            }
        }
    }

    public interface IMemoryAreaReadable
    {
        void ReadEditor(ref MemoryAreaReader reader);
        void ReadRelease(ref MemoryAreaReader reader);
    }
}
