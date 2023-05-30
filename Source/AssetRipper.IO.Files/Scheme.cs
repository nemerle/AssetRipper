namespace AssetRipper.IO.Files
{
	public abstract class Scheme<T> : IScheme where T : FileBase, new()
	{
		public abstract bool CanRead(MemoryAreaAccessor stream);

		public T Read(MemoryAreaAccessor stream, string filePath, string fileName)
		{
			T file = new();
			file.FilePath = filePath;
			file.Name = fileName;
			file.Read(stream);
			return file;
		}

		FileBase IScheme.Read(MemoryAreaAccessor stream, string filePath, string fileName) => Read(stream, filePath, fileName);
	}
}
