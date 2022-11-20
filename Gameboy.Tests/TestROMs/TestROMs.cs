namespace Gameboy.Tests.TestROMs;

// TODO figure out how to do automated testing of the test roms, this is a placeholder

public class TestROMs
{
	[Fact]
	public void Placeholder()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
		var cartridge = new Cartridge(stream);

		Console.WriteLine($"TODO JEFF cart = {cartridge}");
		Console.WriteLine($"TODO JEFF total size of cart = {cartridge.Length}");
		Console.WriteLine($"TODO JEFF title = {cartridge.Title}");
		Console.WriteLine($"TODO JEFF is color? {cartridge.IsColorGameboy}");
		Console.WriteLine($"TODO JEFF is super? {cartridge.IsSuperGameboy}");
		Console.WriteLine($"TODO JEFF type = {cartridge.CartridgeType}");
		Console.WriteLine($"TODO JEFF ROM = {cartridge.ROMBanks}");
		Console.WriteLine($"TODO JEFF RAM = {cartridge.RAMBanks}");

		var emulator = new Emulator(loggerFactory, cartridge);
		emulator.SerialIO.DataAvailable += (value) =>
		{
			Console.WriteLine($"TODO JEFF serial IO data: {NumberUtils.ToBinary(value)}");
			throw new Exception();
		};
		for (var i = 0; i < 100000; i++)
		{
			emulator.Step();
		}
	}
}