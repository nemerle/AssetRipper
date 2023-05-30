using AssetRipper.Assets.Interfaces;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO;

namespace AssetRipper.Import.AssetCreation
{
	public sealed class UnknownObject : RawDataObject, IHasNameString
	{
		public string NameString
		{
			get => $"Unknown{ClassName}_{RawDataHash:X}";
			set { }
		}

		public UnknownObject(AssetInfo assetInfo, MemoryAreaAccessor data) : base(assetInfo, data) { }
	}
}
