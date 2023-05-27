using AssetRipper.IO.Endian;
using System.Text;

namespace AssetRipper.SourceGenerated.Extensions
{
	public static class ByteArrayExtensions
	{
		public static string ToFormattedHex(this byte[] _this)
		{
			StringBuilder sb = new StringBuilder();
			int count = 0;
			foreach (byte b in _this)
			{
				sb.Append(b.ToString("X2"));
				count++;
				if (count >= 16)
				{
					sb.AppendLine();
					count = 0;
				}
				else if (count % 4 == 0)
				{
					sb.Append('\t');
				}
				else
				{
					sb.Append(' ');
				}
			}
			return sb.ToString();
		}
	}
}
