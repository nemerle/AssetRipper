using AssetRipper.Assets.Collections;
using AssetRipper.IO;
using AssetRipper.IO.Endian;

namespace AssetRipper.Assets.IO.Reading
{
	public sealed class AssetReader : EndianReader
	{
        public AssetReader(MemoryAreaAccessor stream, AssetCollection assetCollection) : base(stream, assetCollection.EndianType, false)
		{
			AssetCollection = assetCollection;
		}

		public override string ReadString()
		{
			int length = ReadInt32();

			if (length == 0)
			{
				return string.Empty;
			}

			string ret = ReadString(length);
			AlignStream();
			//Strings have supposedly been aligned since 2.1.0,
			//which is earlier than the beginning of AssetRipper version support.
			return ret;
		}

		public AssetCollection AssetCollection { get; }
	}
}
