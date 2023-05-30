using AssetRipper.IO.Files.Exceptions;
using AssetRipper.IO.Files.Extensions;
using SharpCompress.Compressors.LZMA;
using System.Buffers;

namespace AssetRipper.IO.Files.BundleFiles
{
	public static class LzmaCompression
	{
		/// <summary>
		/// Read LZMA properties and decompress LZMA data
		/// </summary>
		/// <param name="compressedStream">LZMA compressed stream</param>
		/// <param name="compressedSize">Compressed data length</param>
		/// <param name="decompressedStream">Stream for decompressed output</param>
		/// <param name="decompressedSize">Decompressed data length</param>
		public static void DecompressLzmaStream(Stream compressedStream, long compressedSize, Stream decompressedStream, long decompressedSize)
		{
			byte[] properties = new byte[PropertiesSize];
			long basePosition = compressedStream.Position;

			compressedStream.ReadBuffer(properties, 0, PropertiesSize);

			long headSize = compressedStream.Position - basePosition;
			long headlessSize = compressedSize - headSize;

			DecompressLzmaStream(properties, compressedStream, headlessSize, decompressedStream, decompressedSize);

			if (compressedStream.Position > basePosition + compressedSize)
			{
				DecompressionFailedException.ThrowReadMoreThanExpected(compressedSize, compressedStream.Position - basePosition);
			}
			compressedStream.Position = basePosition + compressedSize;
		}
		public static void DecompressLzmaStream(MemoryAreaAccessor compressedStream, long compressedSize, Stream decompressedStream, long decompressedSize)
		{
			long basePosition = compressedStream.Position;
			var properties = compressedStream.ReadBytes(PropertiesSize);

			long headSize = compressedStream.Position - basePosition;
			long headlessSize = compressedSize - headSize;

			DecompressLzmaStream(properties, compressedStream, headlessSize, decompressedStream, decompressedSize);

			if (compressedStream.Position != basePosition + compressedSize)
			{
				throw new Exception($"Read {compressedStream.Position - basePosition} more than expected {compressedSize}");
			}
			
			//compressedStream.Position = basePosition + compressedSize;
		}

		public static void DecompressLzmaStream(MemoryAreaAccessor compressedStream, long compressedSize, MemoryAreaAccessor decompressedStream, long decompressedSize)
		{
			long basePosition = compressedStream.Position;
			var properties = compressedStream.ReadBytes(PropertiesSize);

			long headSize = compressedStream.Position - basePosition;
			long headlessSize = compressedSize - headSize;

			DecompressLzmaStream(properties, compressedStream, headlessSize, decompressedStream, decompressedSize);

			if (compressedStream.Position != basePosition + compressedSize)
			{
				throw new Exception($"Read {compressedStream.Position - basePosition} more than expected {compressedSize}");
			}
			
			//compressedStream.Position = basePosition + compressedSize;
		}
		/// <summary>
		/// Read LZMA properties and decompressed size and decompress LZMA data
		/// </summary>
		/// <param name="compressedStream">LZMA compressed stream</param>
		/// <param name="compressedSize">Compressed data length</param>
		/// <param name="decompressedStream">Stream for decompressed output</param>
		public static void DecompressLzmaSizeStream(MemoryAreaAccessor compressedStream, long compressedSize, MemoryAreaAccessor decompressedStream)
		{
			long basePosition = compressedStream.Position;

			var properties = compressedStream.ReadBytes(PropertiesSize);
			var sizeBytes = compressedStream.ReadBytes(UncompressedSize);
			long decompressedSize = BitConverter.ToInt64(sizeBytes);

			long headSize = compressedStream.Position - basePosition;
			long headlessSize = compressedSize - headSize;

			DecompressLzmaStream(properties, compressedStream, headlessSize, decompressedStream, decompressedSize);

			if (compressedStream.Position > basePosition + compressedSize)
			{
				DecompressionFailedException.ThrowReadMoreThanExpected(compressedSize, compressedStream.Position - basePosition);
			}
			compressedStream.Position = basePosition + compressedSize;
		}

		private static void DecompressLzmaStream(ReadOnlySpan<byte> properties, Stream compressedStream, long headlessSize, Stream decompressedStream, long decompressedSize)
		{
			LzmaStream lzmaStream = new LzmaStream(properties.ToArray(), compressedStream, headlessSize, -1, null, false);

			byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
			long totalRead = 0;
			while (totalRead < decompressedSize)
			{
				int toRead = (int)Math.Min(buffer.Length, decompressedSize - totalRead);
				int read = lzmaStream.Read(buffer, 0, toRead);
				if (read > 0)
				{
					decompressedStream.Write(buffer, 0, read);
					totalRead += read;
				}
				else
				{
					break;
				}
			}
			ArrayPool<byte>.Shared.Return(buffer);
		}
		private static void DecompressLzmaStream(ReadOnlySpan<byte> properties, MemoryAreaAccessor access, long headlessSize, Stream decompressedStream, long decompressedSize)
		{
			var substr=access.ToStream();
			LzmaStream lzmaStream = new LzmaStream(properties.ToArray(), substr, headlessSize, -1, null, false);

			byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
			long totalRead = 0;
			while (totalRead < decompressedSize)
			{
				int toRead = (int)Math.Min(buffer.Length, decompressedSize - totalRead);
				int read = lzmaStream.Read(buffer, 0, toRead);
				if (read > 0)
				{
					decompressedStream.Write(buffer, 0, read);
					totalRead += read;
				}
				else
				{
					break;
				}
			}
			access.Position += substr.Position; // this many consumed
			ArrayPool<byte>.Shared.Return(buffer);
		}
		private static void DecompressLzmaStream(ReadOnlySpan<byte> properties, MemoryAreaAccessor access, long headlessSize, MemoryAreaAccessor decompressedStream, long decompressedSize)
		{
			var substr=access.ToStream();
			LzmaStream lzmaStream = new LzmaStream(properties.ToArray(), substr, headlessSize, -1, null, false);

			byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
			long totalRead = 0;
			while (totalRead < decompressedSize)
			{
				int toRead = (int)Math.Min(buffer.Length, decompressedSize - totalRead);
				int read = lzmaStream.Read(buffer, 0, toRead);
				if (read > 0)
				{
					decompressedStream.Write(buffer, 0, read);
					totalRead += read;
				}
				else
				{
					break;
				}
			}
			access.Position += substr.Position; // this many consumed
			ArrayPool<byte>.Shared.Return(buffer);
		}
		/// <summary>
		/// Compress some data with LZMA.
		/// </summary>
		/// <param name="uncompressedStream">The source stream with uncompressed data.</param>
		/// <param name="uncompressedSize">The number of bytes to read from <paramref name="uncompressedSize"/>.</param>
		/// <param name="compressedStream">The stream in which to write the compressed data.</param>
		/// <returns>The number of compressed bytes written to <paramref name="compressedStream"/> including the 5 property bytes.</returns>
		public static long CompressLzmaStream(Stream uncompressedStream, long uncompressedSize, Stream compressedStream)
		{
			long basePosition = compressedStream.Position;
			LzmaStream lzmaStream = new LzmaStream(new(), false, compressedStream);
			compressedStream.Write(lzmaStream.Properties);
			CopyToLzma(uncompressedStream, lzmaStream, uncompressedSize);
			lzmaStream.Close();
			return compressedStream.Position - basePosition;
		}

		/// <summary>
		/// Compress some data with LZMA.
		/// </summary>
		/// <param name="uncompressedStream">The source stream with uncompressed data.</param>
		/// <param name="uncompressedSize">The number of bytes to read from <paramref name="uncompressedSize"/>.</param>
		/// <param name="compressedStream">The stream in which to write the compressed data.</param>
		/// <returns>
		/// The number of compressed bytes written to <paramref name="compressedStream"/> including the 5 property bytes
		/// and <see langword="long"/> uncompressed size value.
		/// </returns>
		public static long CompressLzmaSizeStream(Stream uncompressedStream, long uncompressedSize, Stream compressedStream)
		{
			long basePosition = compressedStream.Position;
			LzmaStream lzmaStream = new LzmaStream(new(), false, compressedStream);
			compressedStream.Write(lzmaStream.Properties);
			new BinaryWriter(compressedStream).Write(uncompressedSize);
			CopyToLzma(uncompressedStream, lzmaStream, uncompressedSize);
			lzmaStream.Close();
			return compressedStream.Position - basePosition;
		}

		private static void CopyToLzma(Stream inputStream, LzmaStream lzmaStream, long uncompressedSize)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
			long totalCopied = 0;
			while (totalCopied < uncompressedSize)
			{
				int read = inputStream.Read(buffer, 0, (int)Math.Min(buffer.Length, uncompressedSize - totalCopied));
				if (read == 0)
				{
					throw new EndOfStreamException();
				}
				lzmaStream.Write(buffer, 0, read);
				totalCopied += read;
			}
		}

		private const int PropertiesSize = 5;
		private const int UncompressedSize = sizeof(long);
	}
}
