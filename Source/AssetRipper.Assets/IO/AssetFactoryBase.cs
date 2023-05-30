using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO;
using AssetRipper.IO.Files.SerializedFiles.Parser;

namespace AssetRipper.Assets.IO;

public abstract class AssetFactoryBase
{
	public abstract IUnityObjectBase? ReadAsset(AssetInfo assetInfo, MemoryAreaAccessor assetData, SerializedType? assetType);
}
