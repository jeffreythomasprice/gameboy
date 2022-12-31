using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Gameboy.Tests;

public class SkiaSharpImageVideo : Video
{
	public readonly SKBitmap Bitmap;

	private readonly SKColor[] palette = new SKColor[] {
		new(255,255,255),
		new(192,192,192),
		new(128,128,128),
		new(64,64,64),
	};

	public SkiaSharpImageVideo(ILoggerFactory loggerFactory, StopwatchCollection stopwatchCollection) : base(loggerFactory, stopwatchCollection)
	{
		Bitmap = new SKBitmap(Video.ScreenWidth, Video.ScreenHeight, SKColorType.Rgb888x, SKAlphaType.Opaque);
	}

	protected override void SetPixel(int x, int y, Color color)
	{
		Bitmap.SetPixel(x, y, palette[color.Value]);
	}

	protected override void ScanLineAvailable(int y)
	{
		// nothing to do
	}

	protected override void VSync()
	{
		// nothing to do
	}
}