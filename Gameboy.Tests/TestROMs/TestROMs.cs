using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

public class TestROMs
{
	[Fact]
	public void Combined()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/cpu_instrs.gb",
			"""
			cpu_instrs

			01:ok  02:ok  03:ok  04:ok  05:ok  06:ok  07:ok  08:ok  98:ok  10:ok  11:ok

			"""
		);
	}

	[Fact]
	public void _01_Special()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/01-special.gb",
			"""
			01-special


			Passed

			"""
		);
	}

	[Fact]
	public void _02_Interrupts()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/02-interrupts.gb",
			"""
			02-interrupts


			Passed

			"""
		);
	}

	[Fact]
	public void _03_Op_Sp_Hl()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/03-op sp,hl.gb",
			"""
			03-op sp,hl


			Passed

			"""
		);
	}

	[Fact]
	public void _04_Op_R_Imm()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/04-op r,imm.gb",
			"""
			04-op r,imm


			Passed

			"""
		);
	}

	[Fact]
	public void _05_Op_Rp()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/05-op rp.gb",
			"""
			05-op rp


			Passed

			"""
		);
	}

	[Fact]
	public void _06_Ld_R_R()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/06-ld r,r.gb",
			"""
			06-ld r,r


			Passed

			"""
		);
	}

	[Fact]
	public void _07_Jr_Jp_Call_Ret_Rst()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb",
			"""
			07-jr,jp,call,ret,rst


			Passed

			"""
		);
	}

	[Fact]
	public void _08_Misc_Instrs()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/08-misc instrs.gb",
			"""
			08-misc instrs


			Passed

			"""
		);
	}

	[Fact]
	public void _09_Op_R_R()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/09-op r,r.gb",
			"""
			09-op r,r


			Passed

			"""
		);
	}

	[Fact]
	public void _10_Bit_Ops()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/10-bit ops.gb",
			"""
			10-bit ops


			Passed

			"""
		);
	}

	[Fact]
	public void _11_Op_A_Hl()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/11-op a,(hl).gb",
			"""
			11-op a,(hl)


			Passed

			"""
		);
	}

	private void PerformTest(string path, string expectedSerialOutput)
	{
		using var stream = new FileStream(path, FileMode.Open);
		PerformTest(stream, expectedSerialOutput);
	}

	private void PerformTest(Stream stream, string expectedSerialOutput)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var logger = loggerFactory.CreateLogger(GetType().FullName!);
		var cartridge = new Cartridge(stream);
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
				logger.LogTrace("pressing key to try to unstick it");
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
		logger.LogTrace($"wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogTrace($"serial data as text:\n{System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray())}");
		}
		Console.Out.Flush();
		Assert.Equal(expectedSerialOutput, System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray()));
	}
}