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