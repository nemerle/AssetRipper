using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.Extensions;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.MultiFile;
using AssetRipper.IO.Files.Streams.Smart;
using K4os.Compression.LZ4;

namespace AssetRipper.IO.Files.BundleFiles.FileStream
{
	public sealed class FileStreamBundleFile : FileContainer
	{
		public FileStreamBundleHeader Header { get; } = new();
		public BlocksInfo BlocksInfo { get; private set; } = new();
		public DirectoryInfo<FileStreamNode> DirectoryInfo { get; set; } = new();

		public FileStreamBundleFile()
		{
		}

		public FileStreamBundleFile(string filePath)
		{
			var ms = MultiFileStream.OpenReadSingle(filePath);
			Read(ms.CreateAccessor());
		}

		public override void Read(MemoryAreaAccessor memarea)
		{
			var subarea = memarea.CreateSubAccessor();
			var reader = new EndianReader(subarea, EndianType.BigEndian);
			Header.Read(reader);
			long headerSize = subarea.Position;
			ReadFileStreamMetadata(subarea, 0);//ReadBlocksInfoAndDirectory
			ReadFileStreamData(subarea, 0, headerSize);//ReadBlocks and ReadFiles
		}

		public override void Write(Stream stream)
		{
			EndianWriter writer = new EndianWriter(stream, EndianType.BigEndian);
			long basePosition = stream.Position;
			Header.Write(writer);
			long headerSize = stream.Position - basePosition;
			WriteFileStreamMetadata(stream, basePosition);
			WriteFileStreamData(stream, basePosition, headerSize);
		}

		private void ReadFileStreamMetadata(MemoryAreaAccessor stream, long basePosition)
		{
			if (Header.Version >= BundleVersion.BF_LargeFilesSupport)
			{
				stream.Align(16);
			}
			if (Header.Flags.GetBlocksInfoAtTheEnd())
			{
				stream.Position = basePosition + (Header.Size - Header.CompressedBlocksInfoSize);
			}

			CompressionType metaCompression = Header.Flags.GetCompression();
			switch (metaCompression)
			{
				case CompressionType.None:
					{
						ReadMetadata(stream, Header.UncompressedBlocksInfoSize);
					}
					break;

				case CompressionType.Lzma:
					{
						using var memwrapper = new MemoryMappedFileWrapper(Header.UncompressedBlocksInfoSize);
						var uncompressedStream = memwrapper.CreateAccessor();
						LzmaCompression.DecompressLzmaStream(stream, Header.CompressedBlocksInfoSize, uncompressedStream, Header.UncompressedBlocksInfoSize);

						uncompressedStream.Position = 0;
						ReadMetadata(uncompressedStream, Header.UncompressedBlocksInfoSize);
					}
					break;

				case CompressionType.Lz4:
				case CompressionType.Lz4HC:
					{
						int uncompressedSize = Header.UncompressedBlocksInfoSize;
						var memwrapper = new MemoryMappedFileWrapper(uncompressedSize);
						var uncompressedStream = memwrapper.CreateAccessor();
						var compressedBytes = stream.ReadBytes(Header.CompressedBlocksInfoSize);
						int bytesWritten = LZ4Codec.Decode(compressedBytes, uncompressedStream.WriteableSpan());
						if (bytesWritten != uncompressedSize)
						{
							throw new Exception($"Incorrect number of bytes written. {bytesWritten} instead of {uncompressedSize} for {compressedBytes.Length} compressed bytes");
						}
						ReadMetadata(uncompressedStream, uncompressedSize);
					}
					break;

				default:
					throw new NotSupportedException($"Bundle compression '{metaCompression}' isn't supported");
			}
		}

		private void ReadMetadata(MemoryAreaAccessor stream, int metadataSize)
		{
			long metadataPosition = stream.Position;
			var reader = new EndianReader(stream, EndianType.BigEndian);
			{
				BlocksInfo = BlocksInfo.Read(reader);
				if (Header.Flags.GetBlocksAndDirectoryInfoCombined())
				{
					DirectoryInfo = DirectoryInfo<FileStreamNode>.Read(reader);
				}
			}
			if (metadataSize > 0)
			{
				if (stream.Position - metadataPosition != metadataSize)
				{
					throw new Exception($"Read {stream.Position - metadataPosition} but expected {metadataSize} while reading bundle metadata");
				}
			}
		}

		private void ReadFileStreamData(MemoryAreaAccessor stream, long basePosition, long headerSize)
		{
			if (Header.Flags.GetBlocksInfoAtTheEnd())
			{
				stream.Position = basePosition + headerSize;
				if (Header.Version >= BundleVersion.BF_LargeFilesSupport)
				{
					stream.Align(16);
				}
			}
			if (Header.Flags.GetBlockInfoNeedPaddingAtStart())
			{
				stream.Align(16);
			}

			using BundleFileBlockReader blockReader = new BundleFileBlockReader(stream, BlocksInfo);
			foreach (FileStreamNode entry in DirectoryInfo.Nodes)
			{
				var entryStream = blockReader.ReadEntry(entry);
				AddResourceFile(new ResourceFile(entryStream.CreateAccessor(), FilePath, entry.Path));
			}
		}

		private void WriteFileStreamMetadata(Stream stream, long basePosition)
		{
			if (Header.Version >= BundleVersion.BF_LargeFilesSupport)
			{
				stream.Align(16);
			}
			if (Header.Flags.GetBlocksInfoAtTheEnd())
			{
				stream.Position = basePosition + (Header.Size - Header.CompressedBlocksInfoSize);
			}

			CompressionType metaCompression = Header.Flags.GetCompression();
			switch (metaCompression)
			{
				case CompressionType.None:
					{
						WriteMetadata(stream, Header.UncompressedBlocksInfoSize);
					}
					break;

				case CompressionType.Lzma:
					throw new NotImplementedException(nameof(CompressionType.Lzma));

				//These cases will likely need to be separated.
				case CompressionType.Lz4:
				case CompressionType.Lz4HC:
					{
						//These should be set after doing this calculation instead of before
						int uncompressedSize = Header.UncompressedBlocksInfoSize;
						int compressedSize = Header.CompressedBlocksInfoSize;

						byte[] uncompressedBytes = new byte[uncompressedSize];
						WriteMetadata(new MemoryStream(uncompressedBytes), uncompressedSize);
						byte[] compressedBytes = new byte[compressedSize];
						int bytesWritten = LZ4Codec.Encode(uncompressedBytes, compressedBytes, LZ4Level.L00_FAST);
						if (bytesWritten != compressedSize)
						{
							throw new Exception($"Incorrect number of bytes written. {bytesWritten} instead of {compressedSize} for {compressedBytes.Length} compressed bytes");
						}
						new BinaryWriter(stream).Write(compressedBytes);
					}
					break;

				default:
					throw new NotSupportedException($"Bundle compression '{metaCompression}' isn't supported");
			}
		}

		private void WriteMetadata(Stream stream, int metadataSize)
		{
			long metadataPosition = stream.Position;
			using (EndianWriter writer = new EndianWriter(stream, EndianType.BigEndian))
			{
				BlocksInfo.Write(writer);
				if (Header.Flags.GetBlocksAndDirectoryInfoCombined())
				{
					DirectoryInfo.Write(writer);
				}
			}
			if (metadataSize > 0)
			{
				if (stream.Position - metadataPosition != metadataSize)
				{
					throw new Exception($"Wrote {stream.Position - metadataPosition} but expected {metadataSize} while writing bundle metadata");
				}
			}
		}

		private void WriteFileStreamData(Stream stream, long basePosition, long headerSize)
		{
			if (Header.Flags.GetBlocksInfoAtTheEnd())
			{
				stream.Position = basePosition + headerSize;
				if (Header.Version >= BundleVersion.BF_LargeFilesSupport)
				{
					stream.Align(16);
				}
			}
			if (Header.Flags.GetBlockInfoNeedPaddingAtStart())
			{
				stream.Align(16);
			}

			throw new NotImplementedException();
		}
	}
}
