namespace Gameboy.Tests;

public class IMemoryExtensionsTest
{
	[Fact]
	public void ReadAndWriteUInt16()
	{
		var memory = new SimpleMemory();
		memory.WriteUInt16(0x100, 0x0102);
		Assert.Equal(0x02, memory.ReadUInt8(0x100));
		Assert.Equal(0x01, memory.ReadUInt8(0x101));
		Assert.Equal(0x0102, memory.ReadUInt16(0x100));
		Assert.Equal(0x0200, memory.ReadUInt16(0x0ff));
		Assert.Equal(0x0001, memory.ReadUInt16(0x101));
	}

	[Fact]
	public void ReadAndWriteArrays()
	{
		var memory = new SimpleMemory();
		memory.WriteArray(0x100, new byte[] { 1, 2, 3 });
		Assert.Equal(1, memory.ReadUInt8(0x100));
		Assert.Equal(2, memory.ReadUInt8(0x101));
		Assert.Equal(3, memory.ReadUInt8(0x102));
		Assert.Equal(new byte[] { 1, 2, 3 }, memory.ReadArray(0x100, 3));
		Assert.Equal(new byte[] { 0, 1, 2, 3, 0, 0, }, memory.ReadArray(0x0ff, 6));
		memory.WriteArray(0x101, new byte[] { 4, 5, 6, 7 });
		Assert.Equal(new byte[] { 0, 1, 4, 5, 6, 7, 0, 0, 0, 0, }, memory.ReadArray(0x0ff, 10));
	}
}