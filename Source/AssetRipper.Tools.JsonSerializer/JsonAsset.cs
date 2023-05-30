using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Endian;
using AssetRipper.IO.Files;
using System.Text.Json.Nodes;

namespace AssetRipper.Tools.JsonSerializer;

public sealed class JsonAsset : UnityObjectBase
{
	public JsonNode? Contents { get; private set; }

	public JsonAsset(AssetInfo assetInfo) : base(assetInfo)
	{
	}

	public void Read(ref EndianReader reader, SerializableEntry serializableType)
	{
		Contents = serializableType.Read(ref reader);
	}
}
