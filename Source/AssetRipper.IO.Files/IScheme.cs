using AssetRipper.IO.Files.Streams.MultiFile;
using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files
{
	public interface IScheme
	{
		bool CanRead(MemoryAreaAccessor stream);
		FileBase Read(MemoryAreaAccessor stream, string filePath, string fileName);
	}
}
