using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public class VideoBuffer : RepeatableTask
{
	/// <param name="pixels">RGB, will be exactly width * height * 3 in length</param>
	public delegate void PixelDataReadyDelegate(ReadOnlySpan<byte> pixels);

	public record struct Color(byte Red, byte Green, byte Blue) { }

	private abstract record Operation()
	{
		public record ScanLine(
			int Y,
			Video.Color[] Colors
		) : Operation
		{ }
		public record VSync() : Operation { }
	}

	private readonly ILogger logger;
	private readonly Video video;
	private readonly Color[] palette;
	// RGB format
	private readonly byte[] pixels;
	private Channel<Operation>? operations;

	public event PixelDataReadyDelegate? PixelDataReady;

	public VideoBuffer(ILoggerFactory loggerFactory, Video video, Color[] palette) : base(loggerFactory)
	{
		const int paletteSize = 4;
		if (palette.Length != paletteSize)
		{
			throw new ArgumentException($"must provide exactly {paletteSize} colors for palette");
		}

		logger = loggerFactory.CreateLogger<VideoBuffer>();
		this.video = video;
		this.palette = new Color[paletteSize];
		Array.Copy(palette, this.palette, paletteSize);
		pixels = new byte[Video.ScreenWidth * Video.ScreenHeight * 3];

		video.ScanlineAvailable += VideoScanlineAvailable;
		video.VSync += VideoVSync;

		operations = null;
	}

	public void VideoScanlineAvailable(int y, Video.Color[] data)
	{
		var copy = new Video.Color[data.Length];
		Array.Copy(data, copy, data.Length);
		BlockingEnqueue(new Operation.ScanLine(y, copy), CancellationToken.None);
	}

	public void VideoVSync()
	{
		BlockingEnqueue(new Operation.VSync(), CancellationToken.None);
	}

	protected override void DisposeImpl()
	{
		video.ScanlineAvailable -= VideoScanlineAvailable;
		video.VSync -= VideoVSync;
	}

	protected override async Task ThreadRunImpl(CancellationToken cancellationToken)
	{
		// we expect a series of scan lines, one for each pixel on the y axis, then a vsync
		operations = Channel.CreateBounded<Operation>(new BoundedChannelOptions(Video.ScreenHeight + 1)
		{
			SingleReader = true,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.Wait,
		});
		await foreach (var operation in operations.Reader.ReadAllAsync(cancellationToken))
		{
			try
			{
				switch (operation)
				{
					case Operation.ScanLine(var y, var data):
						{
							var offset = y * Video.ScreenWidth * 3;
							for (var x = 0; x < Video.ScreenWidth; x++)
							{
								var color = palette[data[x].Value];
								pixels[offset++] = color.Red;
								pixels[offset++] = color.Green;
								pixels[offset++] = color.Blue;
							}
						}
						break;
					case Operation.VSync:
						// TODO JEFF emit pixels async, keep multiple buffers allocated and check this buffer back in when event handler completes
						PixelDataReady?.Invoke(pixels);
						break;
				};
			}
			catch (Exception e)
			{
				logger.LogError(e, "error handling operation");
			}
		}
	}

	private void BlockingEnqueue(Operation operation, CancellationToken cancellationToken)
	{
		try
		{
			if (operations?.Writer.TryWrite(operation) == false)
			{
				Task.Run(() => operations.Writer.WriteAsync(operation, cancellationToken)).Wait();
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, $"error enqueueing {operation}");
		}
	}
}