using AssetRipper.IO.Files;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetRipper.IO
{
    public unsafe class MemoryAreaAccessor  : IDisposable, IEquatable<MemoryAreaAccessor>
    {
        private byte* Memory;
        private byte* _owned_Memory;
        private long _size;
        private MemoryMappedFileWrapper? _parentFile;
        public long Position { get; set; }
        public static MemoryAreaAccessor Empty = new MemoryAreaAccessor();
        

        public MemoryAreaAccessor()
        {
            Memory = null;
            Position = 0;
            _size = 0;
        }
        public byte * GetBaseMemory()
        {
            return Memory;
        }
        public MemoryAreaAccessor(long size)
        {
            // self-owned memory
            _owned_Memory = (byte*)Marshal.AllocHGlobal((IntPtr)size);
            Memory = _owned_Memory;
            Position = 0;
            _size = size;
        }
        public static MemoryAreaAccessor FromSpan(ReadOnlySpan<byte> span)
        {
            MemoryAreaAccessor result = new MemoryAreaAccessor(span.Length);
            span.CopyTo(result.WriteableSpan());
            return result;
        }

        public MemoryAreaAccessor(MemoryMappedFileWrapper file, long offset = 0, long size = -1)
        {
            if (size == -1)
            {
                size = file.Length - offset;
            }
            if(offset+size>file.Length)
            {
                throw new Exception("MemoryAreaAccessor: offset+size>file.Length");
            }
            Memory = file.Memory + offset;
            Position = 0;
            _size = size;
            _parentFile = file;
        }
        public MemoryAreaAccessor(byte * memory, long offset = 0, long size = -1)
        {
            Memory = memory + offset;
            Position = 0;
            _size = size;
        }
        public MemoryAreaAccessor CopyDeep()
        {
            MemoryAreaAccessor tgt = new MemoryAreaAccessor(_size);
            Buffer.MemoryCopy(Memory, tgt.Memory, _size, _size);
            return tgt;
        } 
        public void CopyDataFrom(MemoryAreaAccessor src)
        {
            if (src._size != _size)
            {
                throw new Exception("MemoryAreaAccessor: src._size!=_size");
            }
            Buffer.MemoryCopy(src.Memory,Memory,_size, src._size);
        } 
        public void CopyDataFrom<T>(T[] src) where T : unmanaged
        {
            fixed (T* srcptr = &src[0])
            {
                if (src.Length*sizeof(T) != _size)
                {
                    throw new Exception("MemoryAreaAccessor: src._size!=_size");
                }
                Buffer.MemoryCopy(srcptr,Memory,_size, src.Length*sizeof(T));
            }
        } 
        public ReadOnlySpan<byte> getSpan(long size=-1)
        {
            if (size == -1)
            {
                size = _size-Position;
            }
            return new ReadOnlySpan<byte>(Memory+Position, (int)size);
        }
        public ReadOnlySpan<T> getSpanTyped<T>(long size = -1) where T : unmanaged
        {
            if (size == -1)
            {
                size = _size - Position;
            }
            return new ReadOnlySpan<T>(Memory + Position, (int)size / sizeof(T));
        }
        public ReadOnlySpan<byte> ReadBytes(int bytes)
        {
            if(Position+bytes>_size)
            {
                throw new Exception("MemoryAreaAccessor: Position+bytes>_size");
            }
            ReadOnlySpan<byte> result = new(Memory+Position, bytes);
            Position += bytes;
            return result;
        }
        public MemoryAreaAccessor CreateSubAccessor(long offset = 0, long size = -1)
        {
            if (size == -1)
            {
                size = _size-(Position+offset);
            }   
            if(offset+Position+size>_size)
            {
                throw new Exception("MemoryAreaAccessor: offset+size>file.Length");
            }
            return new MemoryAreaAccessor(Memory+offset+Position, 0, size);
        }
        public MemoryAreaAccessor CloneClean()
        {
            if (Position == 0)
            {
                return this;
            }

            return new MemoryAreaAccessor(Memory, 0, _size);
        }
        public void CopyTo(Stream stream)
        {
            stream.Write(getSpan());
        }
        public void CopyTo(Span<byte> targetSpan)
        {
            getSpan().CopyTo(targetSpan);
        }
        public ValueTask CopyToAsync(Stream stream)
        {
            return stream.WriteAsync(getSpan().ToArray());
        }
        public long Length => _size;

        public byte ReadByte()
        {
            if(Position>=_size)
            {
                throw new Exception("MemoryAreaAccessor: Position>=_size");
            }
            return Memory[Position++];  
        }
        public T Read<T>() where T : unmanaged
        {
            if (Position + sizeof(T) > _size)
            {
                throw new Exception("MemoryAreaAccessor: Position+sizeof(T)>_size");
            }
            T result = *(T*)(Memory + Position);
            Position += sizeof(T);
            return result;
        }

        public void Align(int alignment=4)
        {
            long pos = Position;
            long mod = pos % alignment;
            if (mod != 0)
            {
                Position += alignment - mod;
            }
        }
        public static bool operator==(MemoryAreaAccessor a, MemoryAreaAccessor b)
        {
            if ((object)a == null)
                return ((object)b == null);
            if ((object)b == null)
                return false;
            
            if (a.Memory == b.Memory)
            {
                return a.Position == b.Position && a._size == b._size;
            }
            var span_a = a.CloneClean().getSpan();
            var span_b = b.CloneClean().getSpan();
            return span_a.SequenceEqual(span_b);
        }

        public static bool operator !=(MemoryAreaAccessor a, MemoryAreaAccessor b)
        {
            return !(a == b);
        }
        public bool Equals(MemoryAreaAccessor? other)
        {
            if(other==null)
            {
                return false;
            }
            return this == other;
        }
        public override bool Equals(object obj) => Equals(obj as MemoryAreaAccessor);
        public override int GetHashCode() => ((IntPtr)Memory, Position,Length).GetHashCode();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (_owned_Memory != null)
            {
                Marshal.FreeHGlobal((IntPtr)_owned_Memory);
                _owned_Memory = null;
            }
        }

        public int Write(byte[] buffer, int offset=0, int size=-1)
        {
            if(size==-1)
            {
                size = buffer.Length - offset;
            }
            if (Position + size > _size)
            {
                throw new Exception("MemoryAreaAccessor: Position+size>_size");
            }
            Marshal.Copy(buffer, offset, (IntPtr)(Memory + Position), size);
            Position += size;
            return size;
        }
        public void Write(MemoryAreaAccessor buffer, long size=-1)
        {
            if(size==-1)
            {
                size = (int)buffer.Length - buffer.Position;
            }
            if(buffer.Position+size>buffer.Length)
            {
                throw new Exception("MemoryAreaAccessor: buffer.Position+size>buffer.Length");
            }
            if (Position + size > _size)
            {
                throw new Exception("MemoryAreaAccessor: Position+size>_size");
            }
            Buffer.MemoryCopy(buffer.Memory+buffer.Position, Memory + Position, size, size);
            
            buffer.Position += size;
            Position += size;
        }

        public Span<byte> WriteableSpan()
        {
            return new Span<byte>(Memory+Position, (int)(_size - Position));
        }
        public MemoryAreaAccessorStream ToStream()
        {
            return new MemoryAreaAccessorStream(this);
        }

        public bool IsNullOrEmpty()
        {
            return Memory == null || _size == 0;
        }
        public string ToFormattedHex()
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (byte b in CloneClean().getSpan())
            {
                sb.Append(b.ToString("X2"));
                count++;
                if (count >= 16)
                {
                    sb.AppendLine();
                    count = 0;
                }
                else if (count % 4 == 0)
                {
                    sb.Append('\t');
                }
                else
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }
    }
    public class MemoryAreaAccessorStream : Stream
    {
        private MemoryAreaAccessor _accessor;
        public MemoryAreaAccessorStream(MemoryAreaAccessor accessor)
        {
            _accessor = accessor;
        }
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _accessor.Length;

        public override long Position { get => _accessor.Position; set => _accessor.Position = value; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var spl = _accessor.ReadBytes(count);
             _accessor.ReadBytes(count).ToArray().CopyTo(buffer, offset);
             return spl.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _accessor.Position = offset;
                    break;
                case SeekOrigin.Current:
                    _accessor.Position += offset;
                    break;
                case SeekOrigin.End:
                    _accessor.Position = _accessor.Length - offset;
                    break;
            }
            return _accessor.Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _accessor.Write(buffer, offset, count);
        }

    }


}
