using AssetRipper.IO.Endian;
using AssetRipper.IO.Files;
using System.Text.Json.Nodes;

namespace AssetRipper.Tools.JsonSerializer;

public sealed class SerializableTypelessData : SerializableEntry
{
	public override JsonNode? Read(ref EndianReader reader)
	{
		int size = reader.ReadInt32();
		var data = reader.Accessor.ReadBytes(size);
		if (data.Length != size)
		{
			throw new EndOfStreamException();
		}
		MaybeAlign(ref reader);
		return JsonValue.Create(Convert.ToBase64String(data));
	}
}
