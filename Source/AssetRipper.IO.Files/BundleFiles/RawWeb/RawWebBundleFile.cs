using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.BundleFiles.RawWeb.Raw;
using AssetRipper.IO.Files.BundleFiles.RawWeb.Web;
using AssetRipper.IO.Files.Extensions;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.BundleFiles.RawWeb
{
	public abstract class RawWebBundleFile<THeader> : FileContainer where THeader : RawWebBundleHeader, new()
	{
		public THeader Header { get; } = new();
		public DirectoryInfo<RawWebNode>? DirectoryInfo { get; set; } = new();

		public override void Read(MemoryAreaAccessor stream)
		{
			EndianReader reader = new(stream, EndianType.BigEndian);
			long basePosition = stream.Position;
			Header.Read(reader);
			long headerSize = stream.Position - basePosition;
			if (headerSize != Header.HeaderSize)
			{
				throw new Exception($"Read {headerSize} but expected {Header.HeaderSize} bytes while reading the raw/web bundle header.");
			}
			ReadRawWebMetadata(stream, out MemoryAreaAccessor dataStream, out long metadataOffset);//ReadBlocksAndDirectory
			ReadRawWebData(dataStream, metadataOffset);//also ReadBlocksAndDirectory
		}

		public override void Write(Stream stream)
		{
			EndianWriter writer = new EndianWriter(stream, EndianType.BigEndian);
			Header.Write(writer);
			throw new NotImplementedException();
		}

		private void ReadRawWebMetadata(MemoryAreaAccessor stream, out MemoryAreaAccessor dataStream, out long metadataOffset)
		{
			int metadataSize = RawWebBundleHeader.HasUncompressedBlocksInfoSize(Header.Version) ? Header.UncompressedBlocksInfoSize : 0;

			//These branches are collapsed by JIT
			if (typeof(THeader) == typeof(RawBundleHeader))
			{
				dataStream = stream;
				metadataOffset = stream.Position;
				ReadMetadata(dataStream, metadataSize);
			}
			else if (typeof(THeader) == typeof(WebBundleHeader))
			{
				// read only last chunk
				BundleScene chunkInfo = Header.Scenes[^1];
				MemoryMappedFileWrapper file = new(chunkInfo.DecompressedSize);
				dataStream = file.CreateAccessor();
				LzmaCompression.DecompressLzmaSizeStream(stream, chunkInfo.CompressedSize, dataStream);
				metadataOffset = 0;

				dataStream.Position = 0;
				ReadMetadata(dataStream, metadataSize);
			}
			else
			{
				throw new Exception($"Unsupported bundle type '{typeof(THeader)}'");
			}
		}

		private void ReadMetadata(MemoryAreaAccessor stream, int metadataSize)
		{
			long metadataPosition = stream.Position;
			EndianReader reader = new(stream, EndianType.BigEndian);
			DirectoryInfo = DirectoryInfo<RawWebNode>.Read(reader);
			reader.AlignStream();
			if (metadataSize > 0)
			{
				if (stream.Position - metadataPosition != metadataSize)
				{
					throw new Exception($"Read {stream.Position - metadataPosition} but expected {metadataSize} while reading bundle metadata");
				}
			}
		}

		private void ReadRawWebData(MemoryAreaAccessor memaccess, long metadataOffset)
		{
			if (DirectoryInfo == null)
			{
				return;
			}
			MemoryAreaAccessor baseArea = memaccess.CloneClean();
			baseArea.Position = metadataOffset;
			foreach (RawWebNode entry in DirectoryInfo.Nodes)
			{
				MemoryAreaAccessor subAccessor = baseArea.CreateSubAccessor(entry.Offset, entry.Size);
				ResourceFile file = new(subAccessor, FilePath, entry.Path);
				AddResourceFile(file);
			}
		}
	}
}
