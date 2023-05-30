using AssetRipper.Assets.Export.Yaml;
using AssetRipper.Assets.IO.Writing;
using AssetRipper.IO.Endian;
using AssetRipper.IO.Files;

namespace AssetRipper.Assets.IO
{
	public interface IAsset : IEndianSpanReadable, IAssetWritable, IYamlExportable
	{
	}
}
