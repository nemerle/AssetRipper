using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.Converters;
using AssetRipper.IO.Files.SerializedFiles.Parser;
using AssetRipper.IO.Files.Streams.MultiFile;
using AssetRipper.IO.Files.Streams.Smart;
using AssetRipper.IO.Files.Utils;

namespace AssetRipper.IO.Files.SerializedFiles
{
	/// <summary>
	/// Serialized files contain binary serialized objects and optional run-time type information.
	/// They have file name extensions like .asset, .assets, .sharedAssets but may also have no extension at all
	/// </summary>
	public sealed class SerializedFile : FileBase
	{
		public SerializedFileHeader Header { get; } = new();
		public SerializedFileMetadata Metadata { get; } = new();
		public UnityVersion Version 
		{
			get => Metadata.UnityVersion;
			set => Metadata.UnityVersion = value;
		}
		public BuildTarget Platform
		{
			get => Metadata.TargetPlatform;
			set => Metadata.TargetPlatform = value;
		}
		public TransferInstructionFlags Flags
		{
			get
			{
				TransferInstructionFlags flags;
				if (SerializedFileMetadata.HasPlatform(Header.Version) && Metadata.TargetPlatform == BuildTarget.NoTarget)
				{
					if (FilePath.EndsWith(".unity", StringComparison.Ordinal))
					{
						flags = TransferInstructionFlags.SerializeEditorMinimalScene;
					}
					else
					{
						flags = TransferInstructionFlags.NoTransferInstructionFlags;
					}
				}
				else
				{
					flags = TransferInstructionFlags.SerializeGameRelease;
				}

				if (FilenameUtils.IsEngineResource(Name) || (Header.Version < FormatVersion.Unknown_10 && FilenameUtils.IsBuiltinExtra(Name)))
				{
					flags |= TransferInstructionFlags.IsBuiltinResourcesFile;
				}
				if (Header.Endianess || Metadata.SwapEndianess)
				{
					flags |= TransferInstructionFlags.SwapEndianess;
				}
				return flags;
			}
		}
		public EndianType EndianType
		{
			get
			{
				bool swapEndianess = SerializedFileHeader.HasEndianess(Header.Version) ? Header.Endianess : Metadata.SwapEndianess;
				return swapEndianess ? EndianType.BigEndian : EndianType.LittleEndian;
			}
		}

		public IReadOnlyList<FileIdentifier> Dependencies => Metadata.Externals;
		private readonly Dictionary<long, int> m_assetEntryLookup = new();
		public IReadOnlyDictionary<long, int> AssetEntryLookup => m_assetEntryLookup;

		public static bool IsSerializedFile(string filePath)
		{
            using var stream = MultiFileStream.OpenReadSingle(filePath);
            return IsSerializedFile(stream.CreateAccessor());
		}

        public static bool IsSerializedFile(MemoryAreaAccessor stream)
		{
            var reader = new EndianReader(stream, EndianType.BigEndian);
			return SerializedFileHeader.IsSerializedFileHeader(reader, stream.Length);
		}

		public ObjectInfo GetAssetEntry(long pathID)
		{
			return Metadata.Object[m_assetEntryLookup[pathID]];
		}

		public override string ToString()
		{
			return NameFixed;
		}

        public override void Read(MemoryAreaAccessor stream)
		{
			m_assetEntryLookup.Clear();

            var reader = new EndianReader(stream, EndianType.BigEndian);
			{
				Header.Read(reader);
			}
			if (SerializedFileMetadata.IsMetadataAtTheEnd(Header.Version))
			{
				stream.Position = Header.FileSize - Header.MetadataSize;
			}
			Metadata.Read(stream, Header);

			SerializedFileMetadataConverter.CombineFormats(Header.Version, Metadata);

            stream.Position = Header.DataOffset;
			for (int i = 0; i < Metadata.Object.Length; i++)
			{
				ObjectInfo objectInfo = Metadata.Object[i];
				m_assetEntryLookup.Add(objectInfo.FileID, i);

                objectInfo.ObjectData = stream.CreateSubAccessor(objectInfo.ByteStart, objectInfo.ByteSize);
			}
		}

		public override void Write(Stream stream)
		{
			throw new NotImplementedException();
		}

		public static SerializedFile FromFile(string filePath)
		{
			string fileName = Path.GetFileName(filePath);
            using var stream = MultiFileStream.OpenReadSingle(filePath);
            return SerializedFileScheme.Default.Read(stream.CreateAccessor(), filePath, fileName);
		}

		/// <summary>
		/// Check if <see langword="this"/> references another <see cref="SerializedFile"/>.
		/// </summary>
		/// <remarks>
		/// This does not resolve intermediate references.
		/// If <see langword="this"/> only references <paramref name="other"/> transiently, it will return <see langword="false"/>.
		/// </remarks>
		/// <param name="other">Another <see cref="SerializedFile"/></param>
		/// <returns>True if <see langword="this"/> directly references <paramref name="other"/>.</returns>
		public bool References(SerializedFile other)
		{
			foreach (FileIdentifier dependency in Dependencies)
			{
				if (dependency.GetFilePath() == other.NameFixed)
				{
					return true;
				}
			}

			return false;
		}
	}
}
