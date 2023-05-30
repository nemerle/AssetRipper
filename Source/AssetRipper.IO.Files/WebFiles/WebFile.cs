using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.MultiFile;
using AssetRipper.IO.Files.Streams.Smart;
using System.Diagnostics;

namespace AssetRipper.IO.Files.WebFiles
{
	public sealed class WebFile : FileContainer
	{
		private const string Signature = "UnityWebData1.0";

		public static bool IsWebFile(MemoryAreaAccessor stream)
		{
			var reader = new EndianReader(stream, EndianType.LittleEndian);
			return IsWebFile(reader);
		}

		public override void Read(MemoryAreaAccessor stream)
		{
			long basePosition = stream.Position;
			List<WebFileEntry> entries = new();
			var reader = new EndianReader(stream, EndianType.LittleEndian);
			string signature = reader.ReadStringZeroTerm();
			Debug.Assert(signature == Signature, $"Signature '{signature}' doesn't match to '{Signature}'");

			int headerLength = reader.ReadInt32(); //total size of the header including the signature and all the entries.
			while (reader.Accessor.Position - basePosition < headerLength)
			{
				entries.Add(WebFileEntry.Read(reader));
			}

			foreach (WebFileEntry entry in entries)
			{
				stream.Position = entry.Offset + basePosition;
				var buffer = stream.CreateSubAccessor(0,entry.Size);
				ResourceFile file = new ResourceFile(buffer, FilePath, entry.Name);
				AddResourceFile(file);
			}
		}

		public override void Write(Stream stream)
		{
			using EndianWriter writer = new EndianWriter(stream, EndianType.LittleEndian);
			Write(writer);
		}

		public void Write(EndianWriter writer, bool alignEntries = true)
		{
			long basePosition = writer.BaseStream.Position;
			writer.WriteStringZeroTerm(Signature);
			long headerSizePosition = writer.BaseStream.Position;

			List<(string, MemoryAreaAccessor)> entryDataList = AllFiles.Select(f => (f.Name, f.ToCleanStream())).ToList();

			//Write entries
			long entriesStartPosition = headerSizePosition + sizeof(int);
			writer.BaseStream.Position = entriesStartPosition;
			int currentOffset = (int)(entriesStartPosition - basePosition);
			long[] offsetPositions = new long[entryDataList.Count];
			for (int i = 0; i < entryDataList.Count; i++)
			{
				(string entryName, MemoryAreaAccessor entryData) = entryDataList[i];
				offsetPositions[i] = writer.BaseStream.Position;
				writer.BaseStream.Position += sizeof(int);
				writer.Write(entryData.Length);
				writer.Write(entryName);
				currentOffset += (int)entryData.Length;
			}
			long entriesEndPosition = writer.BaseStream.Position;

			//Write header size
			writer.BaseStream.Position = headerSizePosition;
			writer.Write((int)(entriesEndPosition - basePosition));
			writer.BaseStream.Position = entriesEndPosition;

			//Write data for the entries
			for (int i = 0; i < entryDataList.Count; i++)
			{
				MemoryAreaAccessor entryData = entryDataList[i].Item2;
				if (alignEntries)
				{
					writer.AlignStream();//Optional, but data alignment is generally a good thing.
				}
				long dataPosition = writer.BaseStream.Position;
				writer.BaseStream.Position = offsetPositions[i];
				writer.Write((int)(dataPosition - basePosition));
				writer.BaseStream.Position = dataPosition;
				entryData.CopyTo(writer.BaseStream);
			}
		}

		internal static bool IsWebFile(EndianReader reader)
		{
			if (reader.Accessor.Length - reader.Accessor.Position > Signature.Length)
			{
				long position = reader.Accessor.Position;
				bool isRead = reader.ReadStringZeroTerm(Signature.Length + 1, out string? signature);
				reader.Accessor.Position = position;
				if (isRead)
				{
					return signature == Signature;
				}
			}
			return false;
		}
	}
}
