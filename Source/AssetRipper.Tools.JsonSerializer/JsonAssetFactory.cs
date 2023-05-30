using AssetRipper.Assets;
using AssetRipper.Assets.Exceptions;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.IO;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Endian;
using AssetRipper.IO;
using AssetRipper.IO.Files.SerializedFiles.Parser;

namespace AssetRipper.Tools.JsonSerializer;

public sealed class JsonAssetFactory : AssetFactoryBase
{
	public override IUnityObjectBase? ReadAsset(AssetInfo assetInfo, MemoryAreaAccessor assetData, SerializedType? assetType)
	{
		if (assetType?.OldType.Nodes.Count > 0)
		{
			EndianReader reader = new EndianReader(assetData, assetInfo.Collection.EndianType);
			SerializableEntry entry = SerializableEntry.FromTypeTree(assetType.OldType);
			JsonAsset asset = new JsonAsset(assetInfo);
			asset.Read(ref reader, entry);
			IncorrectByteCountException.ThrowIf(in reader);
			return asset;
		}
		else
		{
			Console.WriteLine($"Asset could not be read because it has no type tree. ClassID: {assetInfo.ClassID} PathID: {assetInfo.PathID}");
			return null;
		}
	}
}
