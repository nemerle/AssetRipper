using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.WebFiles
{
	public sealed class WebFileScheme : Scheme<WebFile>
	{
		public override bool CanRead(MemoryAreaAccessor stream)
		{
			var reader = new EndianReader(stream, EndianType.LittleEndian);
			return WebFile.IsWebFile(reader);
		}
	}
}
