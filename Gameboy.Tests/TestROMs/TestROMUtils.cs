using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

public static class TestROMUtils
{
	public static void PerformTest(string path, UInt64 maxClock, string expectedSerialOutput)
	{
		using var stream = new FileStream(path, FileMode.Open);
		PerformTest(stream, maxClock, expectedSerialOutput);
	}

	public static void PerformTest(Stream stream, UInt64 maxClock, string expectedSerialOutput)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var logger = loggerFactory.CreateLogger(typeof(TestROMUtils).FullName!);
		var cartridge = new Cartridge(stream);
		var emulator = new Emulator(loggerFactory, cartridge);

		var serialDataOutput = new MemoryStream();
		string serialIOAsText() => System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray());
		var serialDataComplete = false;
		emulator.SerialIO.DataAvailable += (value) =>
		{
			serialDataOutput.WriteByte(value);
			if (!serialDataComplete)
			{
				const UInt64 extraClocks = 100000;
				var actual = serialIOAsText();
				if (actual == expectedSerialOutput)
				{
					logger.LogTrace($"found the expected serial output, marking done in {extraClocks} additional clock ticks");
					maxClock = emulator.Clock + extraClocks;
					serialDataComplete = true;
				}
				else if (expectedSerialOutput.Length >= actual.Length && !expectedSerialOutput.StartsWith(actual))
				{
					logger.LogWarning($"found mismatch in expected serial output, marking done in {extraClocks} additional clock ticks");
					maxClock = emulator.Clock + extraClocks;
					serialDataComplete = true;
				}
			}
		};

		// wait until the exit condition
		emulator.OnTick += (clock) =>
		{
			if (clock >= maxClock)
			{
				logger.LogTrace("reached max clock, exiting");
				emulator.Stop();
			}
			// test programs tend to HALT when they're testing interrupts like timer
			// STOP can only be resumed with a key press
			else if (emulator.CPU.IsStopped)
			{
				emulator.Keypad.SetPressed(Key.Start, true);
			}
			// just waiting for one of those other conditions, so clear the keypad so we can trigger a key press again later if needed
			else
			{
				emulator.Keypad.ClearKeys();
			}
		};

		// run it, and wait for it to finish
		emulator.Start();
		emulator.Join();

		logger.LogTrace($"wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogTrace($"serial data as text:\n{serialIOAsText()}");
		}
		Console.Out.Flush();
		Assert.Equal(expectedSerialOutput, serialIOAsText());
	}
}