using Microsoft.Extensions.Logging;

namespace Gameboy;

public class RGBVideo : Video
{
	public new record struct Color(byte Red, byte Green, byte Blue) { }

	// TODO multiple palettes to switch between
	private readonly Color[] palette = new Color[]
		{
			// approximately 0.8
			new(205,205,205),
			// approximately 0.6
			new(154,154,154),
			// approximately 0.4
			new(102,102,102),
			// approximately 0.2
			new(51,51,51),
		};

	public readonly byte[] Data;

	public RGBVideo(ILoggerFactory loggerFactory, StopwatchCollection stopwatchCollection) : base(loggerFactory, stopwatchCollection)
	{
		Data = new byte[Video.ScreenWidth * Video.ScreenHeight * 3];
	}

	public Color GetColor(Video.Color color)
	{
		return palette[color.Value];
	}

	protected override void SetPixel(int x, int y, Video.Color color)
	{
		var i = 3 * (x + y * Video.ScreenWidth);
		var c = GetColor(color);
		Data[i++] = c.Red;
		Data[i++] = c.Green;
		Data[i++] = c.Blue;
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