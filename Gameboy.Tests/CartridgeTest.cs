namespace Gameboy.Tests;

public class CartridgeTest
{
	[Fact]
	public void TestRangesOfBytes()
	{
		using var stream = new MemoryStream(new byte[] {
			0x00, 0x01, 0x02, 0x03, 0x04,
			0x11, 0x22, 0x33, 0x44, 0x55,
		});
		var cartridge = new Cartridge(stream);
		Assert.Equal(10, cartridge.Length);
		Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }, cartridge.GetBytes(0, 5).ToArray());
		Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, cartridge.GetBytes(5, 5).ToArray());
		Assert.Equal(new byte[] { 0x03, 0x04, 0x11, 0x22 }, cartridge.GetBytes(3, 4).ToArray());
		Assert.Throws<ArgumentOutOfRangeException>(() => cartridge.GetBytes(0, 20));
		Assert.Throws<ArgumentOutOfRangeException>(() => cartridge.GetBytes(-1, 5));
	}
}