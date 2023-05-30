using AssetRipper.Assets;
using AssetRipper.Assets.Interfaces;
using AssetRipper.Assets.IO.Writing;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets.Utils;
using AssetRipper.IO;
using AssetRipper.SourceGenerated;

namespace AssetRipper.Import.AssetCreation
{
	public abstract class RawDataObject : NullObject
	{
		public sealed override string ClassName => ((ClassIDType)ClassID).ToString();
		public MemoryAreaAccessor RawData { get; }
		/// <summary>
		/// A Crc32 hash of <see cref="RawData"/>
		/// </summary>
		public uint RawDataHash => CrcUtils.CalculateDigest(RawData.GetSpan());

		public RawDataObject(AssetInfo assetInfo, MemoryAreaAccessor data) : base(assetInfo)
		{
			RawData = data.CreateSubAccessor();
		}

		public sealed override void WriteEditor(AssetWriter writer) => writer.Write(RawData.GetSpan());

		public sealed override void WriteRelease(AssetWriter writer) => writer.Write(RawData.GetSpan());
	}
}
