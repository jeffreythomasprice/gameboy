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
		var cart = new Cartridge(stream);
		Assert.Equal(10, cart.Length);
		Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }, cart.GetSpan(0, 5).ToArray());
		Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, cart.GetSpan(5, 5).ToArray());
		Assert.Equal(new byte[] { 0x03, 0x04, 0x11, 0x22 }, cart.GetSpan(3, 4).ToArray());
		Assert.Throws<ArgumentOutOfRangeException>(() => cart.GetSpan(0, 20));
		Assert.Throws<ArgumentOutOfRangeException>(() => cart.GetSpan(-1, 5));
	}
}