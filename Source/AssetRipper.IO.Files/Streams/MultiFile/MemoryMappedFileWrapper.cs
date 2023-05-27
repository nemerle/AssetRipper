using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;

namespace AssetRipper.IO.Files
{
    // This class wraps MemoryMappedFileWrapper, and records current position
    public unsafe class MemoryMappedFileWrapper : IDisposable
    {
        public byte* Memory;
        private long _size;
        string filepath;
        MemoryMappedFile? file;
        private bool _isDisposed = false;
        public MemoryMappedViewAccessor Accessor { get; }
        public SafeMemoryMappedViewHandle Handle { get; }
        public MemoryMappedFileWrapper(string filepath)
        {
            this.filepath = filepath;
            if (!File.Exists(filepath))
            {
                throw new ArgumentException($"File {filepath} is missing");
            }
            file = MemoryMappedFile.CreateFromFile(filepath, FileMode.Open);
            _size = new FileInfo(filepath).Length;
            Accessor = file.CreateViewAccessor(0, _size, MemoryMappedFileAccess.Read);
            Handle = Accessor.SafeMemoryMappedViewHandle;
            Handle.AcquirePointer(ref Memory);
        }
        public MemoryMappedFileWrapper(long size)
        {
            file = MemoryMappedFile.CreateNew(null, size);
            _size = size;
            Accessor = file.CreateViewAccessor(0, _size, MemoryMappedFileAccess.ReadWrite);
            Handle = Accessor.SafeMemoryMappedViewHandle;
            Handle.AcquirePointer(ref Memory);
        }
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Accessor.Dispose();
                Handle.ReleasePointer(); // is this even needed?
                Handle.Dispose();
                file?.Dispose();
                _isDisposed = true;
                Memory = null;
            }
        }
        public ReadOnlySpan<byte> getSpan(long offset=0, long size=-1)
        {
            if (size == -1)
            {
                size = _size-offset;
            }
            return new ReadOnlySpan<byte>(Memory+offset, (int)size);
        }
        public MemoryAreaAccessor CreateAccessor(long offset = 0, long size = -1)
        {
            if (size == -1)
            {
                size = _size-(offset);
            }
            return new MemoryAreaAccessor(this, offset, size);
        }
        public long Length => _size;
        public bool CanRead => file!=null && _size>0;
        public bool CanWrite => false;
    }
}
