using Microsoft.Extensions.Logging;

namespace Gameboy.Tests;

public static class MemoryUtils
{
	public static (MemoryROM, InterruptRegisters) CreateMemoryROM(ILoggerFactory loggerFactory, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad, byte[] data)
	{
		const int maxLength = 1024 * 32;
		if (data.Length > maxLength)
		{
			throw new ArgumentException($"max ROM size {maxLength}, {data.Length} provided");
		}
		if (data.Length < maxLength)
		{
			var copy = new byte[maxLength];
			Array.Copy(data, copy, data.Length);
			data = copy;
		}
		using var stream = new MemoryStream(data);
		var interruptRegisters = new InterruptRegisters(serialIO, timer, video, sound, keypad);
		var memory = new MemoryROM(loggerFactory, new Cartridge(stream), serialIO, timer, video, sound, keypad, interruptRegisters);
		return (memory, interruptRegisters);
	}
}