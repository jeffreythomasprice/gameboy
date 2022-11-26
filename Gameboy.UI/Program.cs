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
			// using var stream = new FileStream("gb-test-roms/cpu_instrs/individual/01-special.gb", FileMode.Open);
			using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
			var cartridge = new Cartridge(stream);
			var emulator = new Emulator(loggerFactory, cartridge);

			using var window = new Window(loggerFactory, emulator.Video, emulator.Keypad);

			emulator.SerialIO.DataAvailable += (data) =>
			{
				logger.LogTrace($"TODO JEFF serial IO data = {data}");
			};

			emulator.Keypad.KeypadRegisterDelta += (oldValue, newValue) =>
			{
				logger.LogTrace($"TODO JEFF keypad register {NumberUtils.ToBinary(newValue)}");
			};

			var emulatorThreadRunning = true;
			var emulatorThread = new Thread(() =>
			{
				var logInterval = TimeSpan.FromSeconds(1);
				var nextLogTime = DateTime.Now + logInterval;
				while (emulatorThreadRunning)
				{
					emulator.Step();

					var now = DateTime.Now;
					if (now >= nextLogTime)
					{
						nextLogTime = now + logInterval;
						var state = emulator.CPU.IsStopped ? "STOP" : (emulator.CPU.IsHalted ? "HALT" : "running");
						logger.LogTrace($"TODO JEFF clock time = {emulator.ClockTime}, state: {state}");
					}
				}
			});
			logger.LogTrace("starting emulating thread");
			emulatorThread.Start();

			logger.LogTrace("starting window");
			window.Run();

			logger.LogDebug("stopping emulation thread");
			emulatorThreadRunning = false;
			emulatorThread.Join();
			logger.LogDebug("emulation thread stopped");
		}
		catch (Exception e)
		{
			logger.LogError(e, "oops");
		}
	}
}