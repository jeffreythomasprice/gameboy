namespace Gameboy.Tests;

public class IMemoryExtensionsTest
{
	[Fact]
	public void ReadAndWriteArrays()
	{
		var memory = new SimpleMemory();
		memory.Write(0x100, new byte[] { 1, 2, 3 });
		Assert.Equal(1, memory.Read(0x100));
		Assert.Equal(2, memory.Read(0x101));
		Assert.Equal(3, memory.Read(0x102));
		Assert.Equal(new byte[] { 1, 2, 3 }, memory.Read(0x100, 3));
		Assert.Equal(new byte[] { 0, 1, 2, 3, 0, 0, }, memory.Read(0x0ff, 6));
		memory.Write(0x101, new byte[] { 4, 5, 6, 7 });
		Assert.Equal(new byte[] { 0, 1, 4, 5, 6, 7, 0, 0, 0, 0, }, memory.Read(0x0ff, 10));
	}
}