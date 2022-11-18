namespace Gameboy.Tests;

public class NumberUtilsTest
{
	[Fact]
	public void ToBinary()
	{
		Assert.Equal("#00000001", NumberUtils.ToBinary((byte)1));
		Assert.Equal("#00000010", NumberUtils.ToBinary((byte)2));
		Assert.Equal("#00101010", NumberUtils.ToBinary((byte)42));
		Assert.Equal("#10000000", NumberUtils.ToBinary((byte)128));
		Assert.Equal("#0000000000000001", NumberUtils.ToBinary((UInt16)1));
		Assert.Equal("#0000000000000010", NumberUtils.ToBinary((UInt16)2));
		Assert.Equal("#1000000000000000", NumberUtils.ToBinary((UInt16)32768));
	}

	[Fact]
	public void ToHex()
	{
		Assert.Equal("$01", NumberUtils.ToHex((byte)1));
		Assert.Equal("$0a", NumberUtils.ToHex((byte)10));
		Assert.Equal("$40", NumberUtils.ToHex((byte)64));
		Assert.Equal("$0001", NumberUtils.ToHex((UInt16)1));
		Assert.Equal("$8000", NumberUtils.ToHex((UInt16)32768));
	}
}