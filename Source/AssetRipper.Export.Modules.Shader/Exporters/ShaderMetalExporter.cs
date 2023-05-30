using AssetRipper.Export.Modules.Shaders.IO;
using AssetRipper.Export.Modules.Shaders.ShaderBlob;
using AssetRipper.IO;
using AssetRipper.IO.Endian;
using AssetRipper.VersionUtilities;

namespace AssetRipper.Export.Modules.Shaders.Exporters
{
	public class ShaderMetalExporter : ShaderTextExporter
	{
		/// <summary>
		/// 5.3.0 and greater
		/// </summary>
		public static bool HasBlob(UnityVersion version) => version.IsGreaterEqual(5, 3);

		public override string Name => "ShaderMetalExporter";

		public override void Export(ShaderWriter writer, ref ShaderSubProgram subProgram)
		{
            var area = MemoryAreaAccessor.FromSpan(subProgram.ProgramData);
            var reader = new EndianReader(area,EndianType.LittleEndian);
			if (HasBlob(writer.Version))
			{
				long position = area.Position;
				uint fourCC = reader.ReadUInt32();
				if (fourCC == MetalFourCC)
				{
					int offset = reader.ReadInt32();
					area.Position = position + offset;
				}
				EntryName = reader.ReadStringZeroTerm();
			}

            ExportText(writer, area.GetSpanTyped<char>());
		}

		public string? EntryName { get; private set; }

		private const uint MetalFourCC = 0xf00dcafe;
	}
}
