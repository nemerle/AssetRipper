﻿using AssetRipper.IO.Files.CompressedFiles.GZip;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.Tests;

public static class GZipFileTests
{
	[Test]
	public static void GZipFileReadWriteSymmetry()
	{
		const string Name = "Path.Extension";
		const string FilePath = $"C://Some/Absolute/{Name}";
		const int UncompressedSize = 4067;
		byte[] randomData = RandomData.MakeRandomData(UncompressedSize);

		GZipFile file = new()
		{
			FilePath = FilePath,
			Name = Name,
		};
		file.UncompressedFile = new ResourceFile(SmartStream.CreateMemory(randomData), file.FilePath, file.Name);

		SmartStream memoryStream = SmartStream.CreateMemory();
		file.Write(memoryStream);
		Assert.Multiple(() =>
		{
			Assert.That(memoryStream.Position, Is.GreaterThan(0));
			Assert.That(memoryStream.IsNull, Is.False);
		});
		memoryStream.Position = 0;

		GZipFileScheme fileScheme = new();
		Assert.That(fileScheme.CanRead(memoryStream));
		GZipFile newFile = fileScheme.Read(memoryStream, FilePath, Name);
		Assert.Multiple(() =>
		{
			Assert.That(newFile.Name, Is.EqualTo(file.Name));
			Assert.That(newFile.FilePath, Is.EqualTo(file.FilePath));
			Assert.That(newFile.UncompressedFile, Is.Not.Null);
			Assert.That(memoryStream.Position, Is.GreaterThan(0));
			Assert.That(memoryStream.IsNull, Is.False);
		});
		Stream cl = newFile.UncompressedFile!.ToCleanStream();
		byte[] decompressedData = new byte[cl.Length];
		cl.Read(decompressedData);
		Assert.That(decompressedData, Is.EqualTo(randomData));
	}
}
