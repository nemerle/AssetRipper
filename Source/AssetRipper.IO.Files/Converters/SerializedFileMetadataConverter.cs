using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.IO.Files.SerializedFiles.Parser;

namespace AssetRipper.IO.Files.Converters
{
	public static class SerializedFileMetadataConverter
	{
		public static void CombineFormats(FormatVersion generation, SerializedFileMetadata origin)
		{
			if (!SerializedFileMetadata.HasEnableTypeTree(generation))
			{
				origin.EnableTypeTree = true;
			}
            if (generation < FormatVersion.RefactorTypeData) 
            {
                return;
            }

            foreach (var ob in origin.Object)
			{
                ob.Initialize(origin.Types);
			}
		}
	}
}
