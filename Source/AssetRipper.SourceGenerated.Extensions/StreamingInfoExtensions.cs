using AssetRipper.Assets.Collections;
using AssetRipper.IO;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.SourceGenerated.Subclasses.StreamingInfo;

namespace AssetRipper.SourceGenerated.Extensions
{
	public static class StreamingInfoExtensions
	{
		public static bool IsSet(this IStreamingInfo streamingInfo) => streamingInfo.Path.Data!= MemoryAreaAccessor.Empty;

		public static bool CheckIntegrity(this IStreamingInfo streamingInfo, AssetCollection file)
		{
			if (!streamingInfo.IsSet())
			{
				return true;
			}
			return file.Bundle.ResolveResource(streamingInfo.Path.String) != null;
		}

		public static MemoryAreaAccessor GetContent(this IStreamingInfo streamingInfo, AssetCollection file)
		{
			ResourceFile? res = file.Bundle.ResolveResource(streamingInfo.Path.String);
			if (res == null)
			{
				return MemoryAreaAccessor.Empty;
			}

			var result = res.MemoryView.CloneClean();
			return result.CreateSubAccessor((long)streamingInfo.GetOffset(), streamingInfo.Size);
		}

		public static ulong GetOffset(this IStreamingInfo streamingInfo)
		{
			return streamingInfo.Has_Offset_UInt64() ? streamingInfo.Offset_UInt64 : streamingInfo.Offset_UInt32;
		}

		public static void SetOffset(this IStreamingInfo streamingInfo, ulong value)
		{
			if (streamingInfo.Has_Offset_UInt64())
			{
				streamingInfo.Offset_UInt64 = value;
			}
			else
			{
				streamingInfo.Offset_UInt32 = (uint)value;
			}
		}

		public static void CopyValues(this IStreamingInfo destination, IStreamingInfo source)
		{
			destination.SetOffset(source.GetOffset());
			destination.Size = source.Size;
			destination.Path.String = source.Path.String;
		}

		public static void ClearValues(this IStreamingInfo streamingInfo)
		{
			streamingInfo.Offset_UInt32 = default;
			streamingInfo.Offset_UInt64 = default;
			streamingInfo.Path.Data = MemoryAreaAccessor.Empty;
			streamingInfo.Size = default;
		}
	}
}
