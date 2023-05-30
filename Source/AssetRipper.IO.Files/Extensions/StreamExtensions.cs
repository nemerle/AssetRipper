namespace AssetRipper.IO.Files.Extensions
{
	public static class StreamExtensions
	{
		public static void Align(this Stream _this) => _this.Align(4);
		public static void Align(this Stream _this, int alignment)
		{
			long pos = _this.Position;
			long mod = pos % alignment;
			if (mod != 0)
			{
				_this.Position += alignment - mod;
			}
		}

		public static void ReadBuffer(this Stream _this, byte[] buffer, int offset, int count)
		{
			_this.ReadExactly(buffer, offset, count);
		}

		public static void CopyStream(this Stream _this, Stream dstStream, long size)
		{
			byte[] buffer = new byte[BufferSize];
			for (long left = size; left > 0; left -= BufferSize)
			{
				int toRead = BufferSize < left ? BufferSize : (int)left;
				int offset = 0;
				int count = toRead;
				while (count > 0)
				{
					int read = _this.Read(buffer, offset, count);
					if (read == 0)
					{
						throw new Exception($"No data left");
					}
					offset += read;
					count -= read;
				}
				dstStream.Write(buffer, 0, toRead);
			}
		}

		private const int BufferSize = 81920;
	}
}
