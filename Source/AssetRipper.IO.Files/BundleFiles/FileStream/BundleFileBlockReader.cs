using AssetRipper.IO.Files.Exceptions;
using AssetRipper.IO.Files.Extensions;
using AssetRipper.IO.Files.Streams.Smart;
using K4os.Compression.LZ4;

namespace AssetRipper.IO.Files.BundleFiles.FileStream
{
	internal sealed class BundleFileBlockReader : IDisposable
	{
		public BundleFileBlockReader(MemoryAreaAccessor stream, BlocksInfo blocksInfo)
		{
			m_stream = stream;
			m_blocksInfo = blocksInfo;
			m_dataOffset = stream.Position;
		}

		~BundleFileBlockReader()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public MemoryMappedFileWrapper ReadEntry(FileStreamNode entry)
		{
			if (m_isDisposed)
			{
				throw new ObjectDisposedException(nameof(BundleFileBlockReader));
			}

			// find block offsets
			int blockIndex;
			long blockCompressedOffset = 0;
			long blockDecompressedOffset = 0;
			for (blockIndex = 0; blockDecompressedOffset + m_blocksInfo.StorageBlocks[blockIndex].UncompressedSize <= entry.Offset; blockIndex++)
			{
				blockCompressedOffset += m_blocksInfo.StorageBlocks[blockIndex].CompressedSize;
				blockDecompressedOffset += m_blocksInfo.StorageBlocks[blockIndex].UncompressedSize;
			}
			long entryOffsetInsideBlock = entry.Offset - blockDecompressedOffset;

			MemoryMappedFileWrapper entryRes = CreateStream(entry.Size);
			var entryStream= entryRes.CreateAccessor();
			long left = entry.Size;
			m_stream.Position = m_dataOffset + blockCompressedOffset;

			// copy data of all blocks used by current entry to new stream
			while (left > 0)
			{
				long blockStreamOffset;
				MemoryAreaAccessor blockStream;
				StorageBlock block = m_blocksInfo.StorageBlocks[blockIndex];
				if (m_cachedBlockIndex == blockIndex)
				{
					// data of the previous entry is in the same block as this one
					// so we don't need to unpack it once again. Instead we can use cached stream
					blockStreamOffset = 0;
					blockStream = m_cachedBlockStreamAccessor;
					m_stream.Position += block.CompressedSize;
				}
				else
				{
					CompressionType compressType = block.CompressionType;
					if (compressType is CompressionType.None)
					{
						blockStreamOffset = m_dataOffset + blockCompressedOffset;
						blockStream = m_stream;
					}
					else
					{
						blockStreamOffset = 0;
						m_cachedBlockIndex = blockIndex;
						m_cachedBlockStream = CreateStream(block.UncompressedSize);
						m_cachedBlockStreamAccessor = m_cachedBlockStream.CreateAccessor();
						switch (compressType)
						{
							case CompressionType.Lzma:
								LzmaCompression.DecompressLzmaStream(m_stream, (long)block.CompressedSize, m_cachedBlockStreamAccessor, (long)block.UncompressedSize);
								break;

							case CompressionType.Lz4:
							case CompressionType.Lz4HC:
								uint uncompressedSize = block.UncompressedSize;
								byte[] uncompressedBytes = new byte[uncompressedSize];
								var compressedBytes = m_stream.ReadBytes((int)block.CompressedSize);
								int bytesWritten = LZ4Codec.Decode(compressedBytes, uncompressedBytes);
								if (bytesWritten < 0)
								{
									EncryptedFileException.Throw(entry.PathFixed);
								}
								else if (bytesWritten != uncompressedSize)
								{
									DecompressionFailedException.ThrowIncorrectNumberBytesWritten(uncompressedSize, bytesWritten);
								}
								m_cachedBlockStreamAccessor.Write(uncompressedBytes);
								break;

							default:
								throw new NotSupportedException($"Bundle compression '{compressType}' isn't supported");
						}
						blockStream = m_cachedBlockStreamAccessor;
					}
				}

				// consider next offsets:
				// 1) block - if it is new stream then offset is 0, otherwise offset of this block in the bundle file
				// 2) entry - if this is first block for current entry then it is offset of this entry related to this block
				//			  otherwise 0
				long blockSize = block.UncompressedSize - entryOffsetInsideBlock;
				blockStream.Position = blockStreamOffset + entryOffsetInsideBlock;
				entryOffsetInsideBlock = 0;

				long size = Math.Min(blockSize, left);
				entryStream.Write(blockStream, size);
				blockIndex++;

				blockCompressedOffset += block.CompressedSize;
				left -= size;
			}
			if (left < 0)
			{
				DecompressionFailedException.ThrowReadMoreThanExpected(entry.Size, entry.Size - left);
			}
			entryStream.Position = 0;
			return entryRes;
		}

		private void Dispose(bool disposing)
		{
			m_isDisposed = true;
			if (disposing)
			{
				m_cachedBlockStream?.Dispose();
			}
		}

		private static MemoryMappedFileWrapper CreateStream(long decompressedSize)
		{
			return new MemoryMappedFileWrapper(decompressedSize);
		}

		/// <summary>
		/// The arbitrary maximum size of a decompressed stream to be stored in RAM. 1 MB
		/// </summary>
		/// <remarks>
		/// This number can be set to any integer value, including <see cref="int.MaxValue"/>.
		/// </remarks>
		private const int MaxMemoryStreamLength = 1024 * 1024;
		private readonly MemoryAreaAccessor m_stream;
		private readonly BlocksInfo m_blocksInfo = new();
		private readonly long m_dataOffset;

		private MemoryMappedFileWrapper? m_cachedBlockStream = null;
		private MemoryAreaAccessor? m_cachedBlockStreamAccessor = null;
		private int m_cachedBlockIndex = -1;

		private bool m_isDisposed = false;
	}
}
