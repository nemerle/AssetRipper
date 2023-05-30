using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.CompressedFiles.Brotli
{
	public sealed class BrotliFileScheme : Scheme<BrotliFile>
	{
		public override bool CanRead(MemoryAreaAccessor stream)
		{
			var reader = new EndianReader(stream, EndianType.BigEndian);
			return BrotliFile.IsBrotliFile(reader);
		}
	}
}
