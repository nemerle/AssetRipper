using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.Smart;
using System.IO.Compression;

namespace AssetRipper.IO.Files.CompressedFiles.GZip
{
	public sealed class GZipFile : CompressedFile
	{
		private const ushort GZipMagic = 0x1F8B;

		public override void Read(MemoryAreaAccessor stream)
		{
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream gzipStream = new GZipStream(stream.ToStream(), CompressionMode.Decompress, true))
			{
				gzipStream.CopyTo(memoryStream);
			}
			memoryStream.Position = 0;
			var maa = new MemoryAreaAccessor(memoryStream.Length);
			
			memoryStream.CopyTo(maa.ToStream());
			UncompressedFile = new ResourceFile(maa, FilePath, Name);
		}

		public override void Write(Stream stream)
		{
			using MemoryStream memoryStream = new();
			UncompressedFile?.Write(memoryStream);
			memoryStream.Position = 0;
			using GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, true);
			memoryStream.CopyTo(gzipStream);
		}

		internal static bool IsGZipFile(EndianReader reader)
		{
			long position = reader.Accessor.Position;
			ushort gzipMagic = ReadGZipMagic(reader);
			reader.Accessor.Position = position;
			return gzipMagic == GZipMagic;
		}

		private static ushort ReadGZipMagic(EndianReader reader)
		{
			long remaining = reader.Accessor.Length - reader.Accessor.Position;
			if (remaining >= sizeof(ushort))
			{
				return reader.ReadUInt16();
			}
			return 0;
		}
	}
}
