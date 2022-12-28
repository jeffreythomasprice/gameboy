using Gameboy.Tests.TestROMs;
using Microsoft.Extensions.Logging;

namespace Gameboy.Tests;

public static class LoggerUtils
{
	public static ILoggerFactory CreateLoggerFactory()
	{
		return LoggerFactory.Create(builder =>
		{
			builder
				.SetMinimumLevel(LogLevel.Debug)
				.AddFilter(typeof(TestROMUtils).FullName, LogLevel.Information)
				.AddSimpleConsole(options =>
				{
					options.TimestampFormat = "o";
				});
		});
	}
}