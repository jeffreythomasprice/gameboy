using SkiaSharp;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Gameboy.Tests.TestROMs;

public static class TestROMUtils
{
	public static void PerformTest(string path, TimeSpan minTime, TimeSpan maxTime, string expectedSerialOutput, string expectedVideoHash)
	{
		using var stream = new FileStream(path, FileMode.Open);
		PerformTest(stream, minTime, maxTime, expectedSerialOutput, expectedVideoHash);
	}

	public static void PerformTest(Stream stream, TimeSpan minTime, TimeSpan maxTime, string expectedSerialOutput, string expectedVideoHash)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var logger = loggerFactory.CreateLogger(typeof(TestROMUtils).FullName!);
		var cartridge = new Cartridge(stream);
		var emulator = new Emulator(loggerFactory, cartridge);

		// helper for determining if clock deltas are bigger than a reference amount
		// we'll use this to exit early once output stablize
		bool clockHasNotChangedInAWhile(UInt64 clock) =>
			TimeUtils.ToTimeSpan(emulator.Clock - clock) > TimeSpan.FromSeconds(1);

		// keep everything output to the serial port
		var serialDataOutput = new MemoryStream();
		string serialIOAsText() => System.Text.Encoding.ASCII.GetString(serialDataOutput.ToArray());
		UInt64 lastSerialClock = 0;
		emulator.SerialIO.DataAvailable += (value) =>
		{
			serialDataOutput.WriteByte(value);
			lastSerialClock = emulator.Clock;
		};

		// keep a video buffer up to date
		using var videoBitmap = new SKBitmap(Video.ScreenWidth, Video.ScreenHeight, SKColorType.Rgb888x, SKAlphaType.Unknown);
		// color palette chosen to make it obvious which kind of graphics are in each pixel
		var palettes = new Dictionary<Video.Palette, SKColor[]>
		{
			{
				Video.Palette.Background,
				new SKColor[] {
					new(255,255,255),
					new(192,192,192),
					new(128,128,128),
					new(64,64,64),
				}
			},
			{
				Video.Palette.Window,
				new SKColor[] {
					new(255,0,0),
					new(192,0,0),
					new(128,0,0),
					new(64,0,0),
				}
			},
			{
				Video.Palette.SpriteOBJ0,
				new SKColor[] {
					new(0,255,0),
					new(0,192,0),
					new(0,128,0),
					new(0,64,0),
				}
			},
			{
				Video.Palette.SpriteOBJ1,
				new SKColor[] {
					new(0,0,255),
					new(0,0,192),
					new(0,0,128),
					new(0,0,64),
				}
			},
		};
		UInt64 lastVideoClock = 0;
		string? lastVideoHash = null;
		emulator.Video.ScanlineAvailable += (y, data) =>
		{
			for (var x = 0; x < Video.ScreenWidth; x++)
			{
				var color = palettes[data[x].Palette][data[x].Value];
				videoBitmap.SetPixel(x, y, color);
			}
		};
		using var hashAlgorithm = SHA1.Create();
		emulator.Video.VSync += () =>
		{
			hashAlgorithm.Initialize();
			var currentHash = string.Join("", hashAlgorithm.ComputeHash(videoBitmap.Bytes).Select(x => x.ToString("x2")));
			if (currentHash != lastVideoHash)
			{
				lastVideoClock = emulator.Clock;
				lastVideoHash = currentHash;
			}
		};

		// wait until the exit condition
		emulator.OnTick += (clock) =>
		{
			// if we have the output we expect and we've spent the min amount of time waiting
			if (emulator.ClockTime > minTime && clockHasNotChangedInAWhile(lastSerialClock) && clockHasNotChangedInAWhile(lastVideoClock))
			{
				logger.LogDebug("min time reached, outputs are stable, exiting");
				emulator.Stop();
			}
			// we've spent too much time waiting
			else if (emulator.ClockTime > maxTime)
			{
				logger.LogWarning("reached max clock, exiting");
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
		logger.LogDebug($"final clock = {emulator.Clock} = {emulator.ClockTime}");

		// validate the serial output
		logger.LogDebug($"last serial output at clock = {lastSerialClock} = {TimeUtils.ToTimeSpan(lastSerialClock)}");
		logger.LogTrace($"wrote {serialDataOutput.Length} bytes to serial IO");
		if (serialDataOutput.Length > 0)
		{
			logger.LogTrace($"serial data as text:\n{serialIOAsText()}");
		}
		Console.Out.Flush();
		Assert.Equal(expectedSerialOutput, serialIOAsText());

		// validate the video output
		logger.LogDebug($"video hash last updated at clock = {lastVideoClock} = {TimeUtils.ToTimeSpan(lastVideoClock)}, hash = {lastVideoHash}");
		try
		{
			Assert.Equal(expectedVideoHash, lastVideoHash);
		}
		catch
		{
			// output image to file
			var tempFilePath = Path.GetTempFileName();
			logger.LogDebug($"writing image to {tempFilePath}");
			using var tempFileStream = new FileStream(tempFilePath, FileMode.Truncate, FileAccess.Write);
			if (!videoBitmap.Encode(tempFileStream, SKEncodedImageFormat.Png, 100))
			{
				throw new Exception("error writing image data to file");
			}
			throw;
		}
	}
}