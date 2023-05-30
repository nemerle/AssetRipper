using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.ResourceFiles
{
	public sealed class ResourceFile : FileBase
	{
        public ResourceFile(MemoryAreaAccessor stream, string filePath, string name)
		{
            MemoryView = stream.CreateSubAccessor();
			FilePath = filePath;
			Name = name;
		}

		public bool IsDefaultResourceFile() => IsDefaultResourceFile(Name);

		public static bool IsDefaultResourceFile(string fileName)
		{
			string extension = Path.GetExtension(fileName).ToLowerInvariant();
			return extension is ResourceFileExtension or StreamingFileExtension;
		}

		public override string ToString() => Name;


        public override void Read(MemoryAreaAccessor stream)
		{
			throw new NotSupportedException();
		}

		public override void Write(Stream stream)
		{
            MemoryView.CopyTo(stream);
		}

        public override MemoryAreaAccessor ToCleanStream()
		{
            return MemoryView.CloneClean();
		}

        public MemoryAreaAccessor? MemoryView { get; }

		public const string ResourceFileExtension = ".resource";
		public const string StreamingFileExtension = ".ress";
	}
}
