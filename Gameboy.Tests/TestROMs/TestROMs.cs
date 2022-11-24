using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

// TODO figure out how to do automated testing of the test roms, this is a placeholder

public class TestROMs
{
	[Fact]
	public void Placeholder()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		// TODO JEFF not all CPU tests are passing
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/02-interrupts.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/03-op sp,hl.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/04-op r,imm.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/05-op rp.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/06-ld r,r.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/08-misc instrs.gb", FileMode.Open);
		using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/09-op r,r.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/10-bit ops.gb", FileMode.Open);
		// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/11-op a,(hl).gb", FileMode.Open);
		var cartridge = new Cartridge(stream);

		var logger = loggerFactory.CreateLogger(GetType().FullName!);

		logger.LogDebug($"cart = {cartridge}");
		logger.LogDebug($"total size of cart = {cartridge.Length}");
		logger.LogDebug($"title = {cartridge.Title}");
		logger.LogDebug($"is color? {cartridge.IsColorGameboy}");
		logger.LogDebug($"is super? {cartridge.IsSuperGameboy}");
		logger.LogDebug($"type = {cartridge.CartridgeType}");
		logger.LogDebug($"ROM = {cartridge.ROMBanks}");
		logger.LogDebug($"RAM = {cartridge.RAMBanks}");

		var emulator = new Emulator(loggerFactory, cartridge);
		var serialDataOutput = new MemoryStream();
		emulator.SerialIO.DataAvailable += (value) =>
		{
			serialDataOutput.WriteByte(value);
		};
		// keep track of the last values of the program counter to try to detect CPU cycles
		long lastSerialIOLength = 0;
		var registerPCHistoryLastTimeWePressedAKey = new List<UInt16>();
		var registerPCHistory = new List<UInt16>();
		while (true)
		{
			emulator.Step();
			Console.Out.Flush();

			// keep track of program counter, see if it stays on the same value for a while
			registerPCHistory.Add(emulator.CPU.RegisterPC);
			var pcIsStuck = false;
			if (registerPCHistory.Count > 10)
			{
				registerPCHistory.RemoveAt(0);
				pcIsStuck = registerPCHistory.ToHashSet().Count == 1;
			}

			// if we're waiting on keypad input, or the pc is just hung try pressing a key
			if (emulator.CPU.IsStopped || emulator.CPU.IsHalted || pcIsStuck)
			{
				var makingProgress = false;
				if (!registerPCHistory.SequenceEqual(registerPCHistoryLastTimeWePressedAKey))
				{
					logger.LogTrace("CPU is stuck, but PC is actively changing");
					makingProgress = true;
				}
				else if (serialDataOutput.Length != lastSerialIOLength)
				{
					logger.LogTrace("CPU is stuck, but serial IO is being written to");
					makingProgress = true;
				}
				if (!makingProgress)
				{
					logger.LogDebug("detected PC loop that key presses aren't fixing, aborting");
					break;
				}
				logger.LogDebug("pressing key to try to unstick it");
				lastSerialIOLength = serialDataOutput.Length;
				registerPCHistoryLastTimeWePressedAKey.Clear();
				registerPCHistoryLastTimeWePressedAKey.AddRange(registerPCHistory);
				registerPCHistory.Clear();
				emulator.Keypad.SetPressed(Key.A, true);
				emulator.Step();
				emulator.Keypad.ClearKeys();
				Console.Out.Flush();
			}
		}
		logger.LogDebug($"wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogDebug($"serial data as text:\n{System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray())}");
		}
		Console.Out.Flush();
	}
}