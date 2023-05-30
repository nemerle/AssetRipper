using AssetRipper.Assets.Interfaces;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO;

namespace AssetRipper.Import.AssetCreation
{
	public sealed class UnreadableObject : RawDataObject, IHasNameString
	{
		private string nameString = "";

		public string NameString
		{
			get
			{
				return string.IsNullOrWhiteSpace(nameString)
					? $"Unreadable{ClassName}_{RawDataHash:X}"
					: nameString;
			}

			set => nameString = value;
		}

		public UnreadableObject(AssetInfo assetInfo, MemoryAreaAccessor data) : base(assetInfo, data) { }
	}
}
