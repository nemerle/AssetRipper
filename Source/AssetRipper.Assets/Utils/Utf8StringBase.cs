using AssetRipper.Assets.Export;
using AssetRipper.IO;
using AssetRipper.Yaml;
using System.Text;

namespace AssetRipper.Assets.Utils
{
	public abstract class Utf8StringBase : UnityAssetBase, IEquatable<Utf8StringBase>, IEquatable<string>
	{
        public abstract MemoryAreaAccessor Data { get; set; }

		public string String
		{
            get => Encoding.UTF8.GetString(Data.CleanSpan());
            set => Data = MemoryAreaAccessor.FromSpan(Encoding.UTF8.GetBytes(value));
		}

		public bool IsEmpty => Data.Length == 0;

		public static bool operator ==(Utf8StringBase? utf8String, string? str) => utf8String?.String == str;
		public static bool operator !=(Utf8StringBase? utf8String, string? str) => utf8String?.String != str;
		public static bool operator ==(string? str, Utf8StringBase? utf8String) => utf8String?.String == str;
		public static bool operator !=(string? str, Utf8StringBase? utf8String) => utf8String?.String != str;
		public static bool operator ==(Utf8StringBase? str1, Utf8StringBase? str2)
		{
			if (str1 is null || str2 is null)
			{
				return str1 is null && str2 is null;
			}

			return str1.Data == str2.Data;
		}
		public static bool operator !=(Utf8StringBase? str1, Utf8StringBase? str2) => !(str1 == str2);

		public bool Equals(Utf8StringBase? other) => this == other;

		public bool Equals(string? other) => String.Equals(other, StringComparison.Ordinal);

		public override YamlNode ExportYamlEditor(IExportContainer container)
		{
			return new YamlScalarNode(String);
		}

		public override YamlNode ExportYamlRelease(IExportContainer container)
		{
			return new YamlScalarNode(String);
		}

		public bool CopyIfNullOrEmpty(Utf8StringBase? other)
		{
			if (Data.Length == 0)
			{
				Data = CopyData(other?.Data);
				return true;
			}
			return false;
		}

		private static byte[] CopyData(byte[]? source)
		{
			if (source is null || source.Length == 0)
			{
				return Array.Empty<byte>();
			}
			else
			{
				byte[] destination = new byte[source.Length];
				Array.Copy(source!, destination, source.Length);
				return destination;
			}
		}

        private static MemoryAreaAccessor CopyData(MemoryAreaAccessor? source)
        {
			if (source is null || source.Length == 0)
            {
                return MemoryAreaAccessor.Empty;
            }
            else
            {
                var res = new MemoryAreaAccessor(source.Length);
                res.CopyDataFrom(source);
                return res;
            }
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
			{
				return false;
			}
			else if (obj is Utf8StringBase utf8String)
			{
				return Equals(utf8String);
			}
			else if (obj is string str)
			{
				return Equals(str);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return unchecked((int)CrcUtils.CalculateDigest(Data.CleanSpan()));
		}

		public override string ToString()
		{
			return String;
		}
	}
}
