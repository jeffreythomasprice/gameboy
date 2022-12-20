using Microsoft.Extensions.Logging;

namespace Gameboy.Tests;

public static class MemoryUtils
{
	public static MemoryROM CreateMemoryROM(ILoggerFactory loggerFactory, SerialIO serialIO, Timer timer, Video video, byte[] data)
	{
		const int maxLength = 1024 * 16;
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
		return new MemoryROM(loggerFactory, new Cartridge(stream), serialIO, timer, video);
	}
}