using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

// TODO figure out how to do automated testing of the test roms, this is a placeholder

public class TestROMs
{
	[Fact]
	public void Placeholder()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
		var cartridge = new Cartridge(stream);

		var logger = loggerFactory.CreateLogger(GetType().FullName!);

		logger.LogDebug($"TODO JEFF cart = {cartridge}");
		logger.LogDebug($"TODO JEFF total size of cart = {cartridge.Length}");
		logger.LogDebug($"TODO JEFF title = {cartridge.Title}");
		logger.LogDebug($"TODO JEFF is color? {cartridge.IsColorGameboy}");
		logger.LogDebug($"TODO JEFF is super? {cartridge.IsSuperGameboy}");
		logger.LogDebug($"TODO JEFF type = {cartridge.CartridgeType}");
		logger.LogDebug($"TODO JEFF ROM = {cartridge.ROMBanks}");
		logger.LogDebug($"TODO JEFF RAM = {cartridge.RAMBanks}");

		var emulator = new Emulator(loggerFactory, cartridge);
		emulator.SerialIO.DataAvailable += (value) =>
		{
			logger.LogDebug($"TODO JEFF serial IO data: {NumberUtils.ToBinary(value)}");
			throw new Exception();
		};
		for (var i = 0; i < 10000000; i++)
		{
			emulator.Step();
			if (i % 100 == 0)
			{
				logger.LogDebug($"TODO JEFF clock: {emulator.Clock}");
			}
			Console.Out.Flush();
		}
		logger.LogDebug($"TODO JEFF final clock: {emulator.Clock}");
	}
}