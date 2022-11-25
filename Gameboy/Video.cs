using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Video : ISteppable
{
	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;

	public Video(ILoggerFactory loggerFactory, IMemory memory)
	{
		logger = loggerFactory.CreateLogger<Video>();
		this.memory = memory;
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		Clock = 0;
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;

		// TODO JEFF VIDEO!
	}
}