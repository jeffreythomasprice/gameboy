namespace Gameboy.UI;

using Gameboy;
using Microsoft.Extensions.Logging;

public class Program
{
	public static void Main(string[] args)
	{
		using var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder
				.SetMinimumLevel(LogLevel.Information)
				.AddFilter(typeof(Program).Namespace, LogLevel.Debug)
				.AddFilter(typeof(Emulator).Namespace, LogLevel.Debug)
				.AddSimpleConsole(options =>
				{
					options.TimestampFormat = "o";
				});
		});
		var logger = loggerFactory.CreateLogger<Program>();
		try
		{
			if (args.Length != 1)
			{
				logger.LogError("provide a rom path");
				return;
			}
			using var stream = new FileStream(args[0], FileMode.Open, FileAccess.Read);
			var cartridge = new Cartridge(stream);
			logger.LogDebug($"""
			title = {cartridge.Title}
			type = {cartridge.CartridgeType}
			ROM = {cartridge.ROMBanks}
			RAM = {cartridge.RAMBanks}
			color? {cartridge.IsColorGameboy}
			super? {cartridge.IsSuperGameboy}
			""");

			var emulator = new Emulator(loggerFactory, cartridge);
			emulator.EmitDebugStatsEnabled = true;

			using var window = new Window(loggerFactory, emulator.Video, emulator.Keypad);

			emulator.Start();

			logger.LogTrace("starting window");
			window.Run();

			emulator.Stop();
			emulator.Wait(CancellationToken.None);
		}
		catch (Exception e)
		{
			logger.LogError(e, "oops");
		}
	}
}