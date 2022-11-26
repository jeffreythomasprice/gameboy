using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

public class TestROMs
{
	[Fact]
	public void Combined()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/cpu_instrs.gb",
			1000000000,
			"""
			cpu_instrs

			01:ok  02:ok  03:ok  04:ok  05:ok  06:ok  07:ok  08:ok  09:ok  10:ok  11:ok
			"""
		);
	}

	[Fact]
	public void _01_Special()
	{
		PerformTest(
			"gb-test-roms/cpu_instrs/individual/01-special.gb",
			10000000,
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
			10000000,
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
			100000000,
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
			100000000,
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
			100000000,
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
			10000000,
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
			10000000,
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
			10000000,
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
			100000000,
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
			100000000,
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
			100000000,
			"""
			11-op a,(hl)


			Passed

			"""
		);
	}

	private void PerformTest(string path, UInt64 maxClock, string expectedSerialOutput)
	{
		using var stream = new FileStream(path, FileMode.Open);
		PerformTest(stream, maxClock, expectedSerialOutput);
	}

	private void PerformTest(Stream stream, UInt64 maxClock, string expectedSerialOutput)
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
		string serialIOAsText() => System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray());
		var foundSerialIOExitCondition = false;
		while (emulator.Clock < maxClock)
		{
			emulator.Keypad.ClearKeys();

			emulator.Step();
			Console.Out.Flush();

			// if we've output exactly the right thing, or the wrong charcters, or too many characters we can stop executing
			if (
				// first time only, that starts the final countdown until we time out
				!foundSerialIOExitCondition &&
				(
					// found exactly the right number of characters, or too many
					serialIOAsText().Length >= expectedSerialOutput.Length ||
					// found the wrong characters
					serialIOAsText() != expectedSerialOutput.Substring(0, serialIOAsText().Length)
				)
			)
			{
				foundSerialIOExitCondition = true;
				// keep executing a short time to see if we output any extra spurrious characters
				maxClock = emulator.Clock + 10000;
			}

			// test programs tend to HALT when they're testing interrupts like timer
			// STOP can only be resumed with a key press
			if (emulator.CPU.IsStopped)
			{
				emulator.Keypad.SetPressed(Key.Start, true);
				emulator.Step();
			}
		}
		logger.LogTrace($"wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogTrace($"serial data as text:\n{serialIOAsText()}");
		}
		Console.Out.Flush();
		Assert.Equal(expectedSerialOutput, serialIOAsText());
	}
}