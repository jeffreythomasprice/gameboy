using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

// TODO figure out how to do automated testing of the test roms, this is a placeholder

public class TestROMs
{
	[Fact]
	public void Placeholder()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
		using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
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
		var serialDataOutput = new MemoryStream();
		emulator.SerialIO.DataAvailable += (value) =>
		{
			serialDataOutput.WriteByte(value);
		};
		// TODO don't guess how many key presses needed, run until serial IO stops getting added to
		for (var i = 0; i < 10; i++)
		{
			while (!emulator.CPU.IsStopped && !emulator.CPU.IsHalted)
			{
				emulator.Step();
				Console.Out.Flush();
			}

			// TODO should be an actual simulated key press
			// force the CPU back into action, like we pressed a key
			emulator.CPU.IsHalted = false;
			emulator.CPU.IsStopped = false;
		}
		logger.LogDebug($"TODO JEFF final clock: {emulator.Clock}");
		logger.LogDebug($"TODO JEFF wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogDebug($"TODO JEFF serial data as text: {System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray())}");
		}
		Console.Out.Flush();
	}
}