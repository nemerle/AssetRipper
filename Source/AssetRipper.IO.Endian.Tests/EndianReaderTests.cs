using AssetRipper.IO.Endian;
namespace AssetRipper.IO.Endian.Tests;

public partial class EndianReaderTests
{
	[Theory]
	public void ReadStringThrowsForNegativeLength(EndianType endianType)
	{
		const int Length = -1;
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			var empty = new MemoryAreaAccessor(0)
			EndianReader reader = new EndianReader(empty, endianType);
			reader.ReadString(Length);
		});
		Assert.That(exception.ActualValue, Is.EqualTo(Length));
	}

	[Theory]
	public void ReadStringReturnsEmptyStringForLengthZero(EndianType endianType)
	{
		var empty = new MemoryAreaAccessor(0);
		EndianReader reader = new EndianReader(empty, endianType);
		Assert.That(reader.ReadString(0), Is.EqualTo(string.Empty));
	}
}
