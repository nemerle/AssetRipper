namespace AssetRipper.IO.Endian.Tests;
using EndianReader = EndianReader;

public partial class EndianSpanReaderTests
{
	[Theory]
	public void EndianTypeTest(EndianType endianType)
	{
		EndianReader reader = new EndianReader(default, endianType);
		Assert.That(reader.Type, Is.EqualTo(endianType));

		EndianType otherType = endianType == EndianType.BigEndian ? EndianType.LittleEndian : EndianType.BigEndian;
		reader.Type = otherType;
		Assert.That(reader.Type, Is.EqualTo(otherType));
	}

	[Theory]
	public void ReadBytesIsTheSameForBothOverloads(EndianType endianType)
	{
		const int SourceLength = 32;
		const int DesiredLength = 64;
		ReadOnlySpan<byte> sourceSpan = RandomData.NextBytes(SourceLength);

		EndianReader reader = new EndianReader(sourceSpan, endianType);
		Assert.That(reader.Length, Is.EqualTo(SourceLength));

		ReadOnlySpan<byte> readSpan = reader.ReadBytes(DesiredLength);
		Assert.That(reader.Position, Is.EqualTo(SourceLength));
		Assert.That(readSpan.SequenceEqual(sourceSpan));

		reader.Position = 0;

		Span<byte> targetSpan = new byte[DesiredLength];
		reader.ReadBytes(targetSpan);
		Assert.That(reader.Position, Is.EqualTo(SourceLength));
		Assert.That(targetSpan.Slice(0, SourceLength).SequenceEqual(sourceSpan));
	}

	[Theory]
	public void ReadBytesExactIsTheSameForBothOverloads(EndianType endianType)
	{
		const int Length = 64;
		ReadOnlySpan<byte> sourceSpan = RandomData.NextBytes(Length);
		Span<byte> targetSpan = new byte[Length];

		EndianReader reader = new EndianReader(sourceSpan, endianType);
		Assert.That(reader.Length, Is.EqualTo(Length));

		ReadOnlySpan<byte> readSpan = reader.ReadBytesExact(Length);
		Assert.That(reader.Position, Is.EqualTo(Length));
		Assert.That(readSpan.SequenceEqual(sourceSpan));

		reader.Position = 0;

		reader.ReadBytesExact(targetSpan);
		Assert.That(reader.Position, Is.EqualTo(Length));
		Assert.That(targetSpan.SequenceEqual(sourceSpan));
	}

	[Theory]
	public void ReadBytesWorksForZeroLengthReading(EndianType endianType)
	{
		EndianReader reader = new EndianReader(default, endianType);
		Assert.That(reader.Length, Is.EqualTo(0));

		Assert.That(reader.ReadBytes(0).Length, Is.EqualTo(0));
		Assert.That(reader.ReadBytes(Span<byte>.Empty), Is.EqualTo(0));
		Assert.That(reader.ReadBytesExact(0).Length, Is.EqualTo(0));
		reader.ReadBytes(Span<byte>.Empty);
		Assert.That(reader.Position, Is.EqualTo(0));
	}

	[Theory]
	public void ReadBytesThrowsForNegativeLengthReading(EndianType endianType)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			EndianReader reader = new EndianReader(default, endianType);
			reader.ReadBytes(-1);
		});
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			EndianReader reader = new EndianReader(default, endianType);
			reader.ReadBytesExact(-1);
		});
	}

	[Theory]
	public void AlignDoesNotThrowForZeroLength(EndianType endianType)
	{
		Assert.DoesNotThrow(() =>
		{
			EndianReader reader = new EndianReader(default, endianType);
			reader.Align();
		});
	}

	[Theory]
	public void AlignMovesPositionToMultipleOfFour(EndianType endianType)
	{
		Span<byte> data = stackalloc byte[6];
		EndianReader reader = new EndianReader(data, endianType);
		for (int i = 1; i <= 4; i++)
		{
			reader.Position = i;
			reader.Align();
			Assert.That(reader.Position, Is.EqualTo(4));
		}
	}
}
