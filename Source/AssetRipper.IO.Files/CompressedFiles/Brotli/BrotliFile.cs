using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.Smart;
using System.IO.Compression;

namespace AssetRipper.IO.Files.CompressedFiles.Brotli
{
	public sealed class BrotliFile : CompressedFile
	{
		private const string BrotliSignature = "UnityWeb Compressed Content (brotli)";

		public override void Read(MemoryAreaAccessor stream)
		{
			var buffer = ReadBrotli(stream);
			UncompressedFile = new ResourceFile(buffer, FilePath, Name);
		}

		internal static bool IsBrotliFile(EndianReader reader)
		{
			long position = reader.Accessor.Position;
			string? brotliSignature = ReadBrotliMetadata(reader);
			reader.Accessor.Position = position;
			return brotliSignature == BrotliSignature;
		}

		private static string? ReadBrotliMetadata(EndianReader reader)
		{
			long remaining = reader.Accessor.Length - reader.Accessor.Position;
			if (remaining < 4)
			{
				return null;
			}

			reader.Accessor.Position += 1;
			byte bt = reader.ReadByte(); // read 3 bits
			int sizeBytes = bt & 0x3;

			if (reader.Accessor.Position + sizeBytes > reader.Accessor.Length)
			{
				return null;
			}

			int length = 0;
			for (int i = 0; i < sizeBytes; i++)
			{
				byte nbt = reader.ReadByte();  // read next 8 bits
				int bits = (bt >> 2) | ((nbt & 0x3) << 6);
				bt = nbt;
				length += bits << (8 * i);
			}

			if (length <= 0)
			{
				return null;
			}
			if (reader.Accessor.Position + length > reader.Accessor.Length)
			{
				return null;
			}

			return reader.ReadString(length);
		}

		private static MemoryAreaAccessor ReadBrotli(MemoryAreaAccessor stream)
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BrotliStream brotliStream = new BrotliStream(stream.ToStream(), CompressionMode.Decompress);
			brotliStream.CopyTo(memoryStream);
			MemoryAreaAccessor res = new MemoryAreaAccessor(memoryStream.Length);
			res.Write(memoryStream.ToArray());
			return res;
		}

		public override void Write(Stream stream)
		{
			throw new NotImplementedException();
		}
	}
}
