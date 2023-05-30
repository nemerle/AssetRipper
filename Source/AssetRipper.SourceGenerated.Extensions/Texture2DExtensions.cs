﻿using AssetRipper.IO;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Enums;

namespace AssetRipper.SourceGenerated.Extensions
{
	public static class Texture2DExtensions
	{
		public static int GetCompleteImageSize(this ITexture2D texture)
		{
			if (texture.Has_CompleteImageSize_C28_UInt32())
			{
				return (int)texture.CompleteImageSize_C28_UInt32;//No texture is larger than 2GB
			}
			else
			{
				return texture.CompleteImageSize_C28_Int32;
			}
		}

		public static bool GetMips(this ITexture2D texture)
		{
			return texture.MipMap_C28 || texture.MipCount_C28 > 0;
		}

		public static bool CheckAssetIntegrity(this ITexture2D texture)
		{
			if (!texture.ImageData_C28.IsNullOrEmpty())
			{
				return true;
			}
			else if (texture.StreamData_C28 is not null)
			{
				return texture.StreamData_C28.CheckIntegrity(texture.Collection);
			}
			else
			{
				return false;
			}
		}

		public static ReadOnlySpan<byte> GetImageData(this ITexture2D texture)
		{
			ReadOnlySpan<byte> data = texture.ImageData_C28.CleanSpan();

			if (data.Length != 0)
			{
				return data;
			}
			else if (texture.StreamData_C28 is not null && texture.StreamData_C28.IsSet())
			{
				data = texture.StreamData_C28.GetContent(texture.Collection).CleanSpan();
			}

			if (IsSwapBytes(texture.Collection.Platform, texture.Format_C28E))
			{
				var changed_data = data.ToArray();
				for (int i = 0; i < data.Length; i += 2)
				{
					(changed_data[i], changed_data[i + 1]) = (changed_data[i + 1], changed_data[i]);
				}
				return changed_data;
			}

			return data;
		}

		public static bool IsSwapBytes(IO.Files.BuildTarget platform, TextureFormat format)
		{
			if (platform == IO.Files.BuildTarget.XBox360)
			{
				switch (format)
				{
					case TextureFormat.ARGB4444:
					case TextureFormat.RGB565:
					case TextureFormat.DXT1:
					case TextureFormat.DXT1Crunched:
					case TextureFormat.DXT3:
					case TextureFormat.DXT5:
					case TextureFormat.DXT5Crunched:
						return true;
				}
			}
			return false;
		}
	}
}
