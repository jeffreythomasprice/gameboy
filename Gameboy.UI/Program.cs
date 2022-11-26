namespace Gameboy.UI;

using System.Drawing;
using Gameboy;
using Microsoft.Extensions.Logging;

public class Program
{
	public static void Main()
	{
		using var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder
				.SetMinimumLevel(LogLevel.Information)
				.AddFilter("Gameboy", LogLevel.Debug)
				.AddFilter("Gameboy.UI", LogLevel.Trace)
				.AddSimpleConsole(options =>
				{
					options.TimestampFormat = "o";
				});
		});
		var logger = loggerFactory.CreateLogger<Program>();
		try
		{
			using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
			var cartridge = new Cartridge(stream);
			var emulator = new Emulator(loggerFactory, cartridge);

			using var window = new Window(loggerFactory);

			// TODO multiple palettes to switch between
			var palette = new Color[]
			{
				// approximately 0.8
				Color.FromArgb(205,205,205),
				// approximately 0.6
				Color.FromArgb(154,154,154),
				// approximately 0.4
				Color.FromArgb(102,102,102),
				// approximately 0.2
				Color.FromArgb(51,51,51),
			};
			emulator.Video.ScanlineAvailable += (y, data) =>
			{
				window.ScanlineAvailable(y, data.Select(color => palette[color]).ToArray());
			};

			window.Run();
		}
		catch (Exception e)
		{
			logger.LogError(e, "oops");
		}
	}
}