namespace Gameboy.Tests.TestROMs;

// TODO figure out how to do automated testing of the test roms, this is a placeholder

public class TestROMs
{
	[Fact]
	public void Placeholder()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
		var cart = new Cartridge(stream);

		Console.WriteLine($"TODO JEFF cart = {cart}");
		Console.WriteLine($"TODO JEFF total size of cart = {cart.Length}");
		Console.WriteLine($"TODO JEFF title = {cart.Title}");
		Console.WriteLine($"TODO JEFF is color? {cart.IsColorGameboy}");
		Console.WriteLine($"TODO JEFF is super? {cart.IsSuperGameboy}");
		Console.WriteLine($"TODO JEFF type = {cart.CartridgeType}");
		Console.WriteLine($"TODO JEFF ROM = {cart.ROMBanks}");
		Console.WriteLine($"TODO JEFF RAM = {cart.RAMBanks}");

		var memory = cart.CreateMemory();
		var cpu = new CPU(loggerFactory, memory);

		while (!cpu.IsHalted && !cpu.IsStopped)
		{
			cpu.Step();
			if (cpu.Clock % 1000 == 0)
			{
				Console.WriteLine($"TODO JEFF clock = {cpu.Clock}");
			}
		}
		Console.WriteLine($"TODO JEFF final clock = {cpu.Clock}");
	}
}