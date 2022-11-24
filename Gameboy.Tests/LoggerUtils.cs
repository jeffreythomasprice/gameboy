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
				.AddSimpleConsole(options =>
				{
					options.TimestampFormat = "o";
				});
		});
	}
}