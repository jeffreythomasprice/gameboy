using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : IDisposable, ISteppable
{
	private readonly ILogger logger;

	private readonly Memory memory;
	private readonly CPU cpu;
	private readonly SerialIO serialIO;
	private readonly Keypad keypad;
	private readonly Timer timer;
	private readonly Video video;

	private bool isDisposed = false;
	private Thread? thread = null;
	private bool threadShouldExit;

	// TODO JEFF timing debugging
	private Stopwatch totalStopwatch = new();
	private Stopwatch videoStopwatch = new();
	private Stopwatch cpuStopwatch = new();

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
		logger = loggerFactory.CreateLogger<Emulator>();

		memory = cartridge.CreateMemory(loggerFactory);
		cpu = new CPU(loggerFactory, memory);
		serialIO = new SerialIO(loggerFactory, memory);
		keypad = new Keypad(loggerFactory, memory);
		timer = new Timer(loggerFactory, memory);
		video = new Video(loggerFactory, memory);

		memory.IORegisterDIVWrite += (byte oldValue, ref byte newValue) =>
		{
			timer.RegisterDIVWrite(oldValue, ref newValue);
		};
		memory.IORegisterLYWrite += (byte oldValue, ref byte newValue) =>
		{
			video.RegisterLYWrite(oldValue, ref newValue);
		};

		video.SetVideoMemoryEnabled += (enabled) =>
		{
			memory.VideoMemoryEnabled = enabled;
		};
		video.SetSpriteAttributeMemoryEnabled += (enabled) =>
		{
			memory.SpriteAttributeMemoryEnabled = enabled;
		};
	}

	~Emulator()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(true);
	}

	public IMemory Memory => memory;

	public CPU CPU => cpu;

	public SerialIO SerialIO => serialIO;

	public Keypad Keypad => keypad;

	public Timer Timer => timer;

	public Video Video => video;

	public ulong Clock => cpu.Clock;

	public TimeSpan ClockTime => TimeUtils.ToTimeSpan(Clock);

	public void Reset()
	{
		memory.Reset();
		cpu.Reset();
		serialIO.Reset();
		keypad.Reset();
		timer.Reset();
		video.Reset();
	}

	public void Step()
	{
		totalStopwatch.Start();

		Step(memory);

		Step(serialIO);

		Step(keypad);

		Step(timer);

		videoStopwatch.Start();
		Step(video);
		videoStopwatch.Stop();

		cpuStopwatch.Start();
		cpu.Step();
		cpuStopwatch.Stop();

		totalStopwatch.Stop();

		if (cpu.Clock % CPU.ClockTicksPerSecond == 0)
		{
			var videoPercentage = ((double)videoStopwatch.ElapsedTicks / (double)totalStopwatch.ElapsedTicks * 100.0).ToString("N2") + "%";
			var cpuPercentage = ((double)cpuStopwatch.ElapsedTicks / (double)totalStopwatch.ElapsedTicks * 100.0).ToString("N2") + "%";
			logger.LogDebug($"TODO JEFF total={totalStopwatch.Elapsed}, video={videoStopwatch.Elapsed} ({videoPercentage}) cpu={cpuStopwatch.Elapsed} ({cpuPercentage})");
		}
	}

	public void Start()
	{
		lock (this)
		{
			if (thread != null)
			{
				logger.LogTrace("already running");
				return;
			}
			logger.LogDebug("starting emulation thread");
			threadShouldExit = false;
			thread = new Thread(() =>
			{
				while (!threadShouldExit)
				{
					Step();
					// TODO wait for configurable amounts of time if we want a speed other than "fastest possible"
					Thread.Yield();
				}
				logger.LogDebug("thread stopped");
				thread = null;
			});
			thread.Start();
		}
	}

	public void Stop()
	{
		lock (this)
		{
			if (thread == null)
			{
				logger.LogTrace("not running");
				return;
			}
			logger.LogDebug("stopping");
			threadShouldExit = true;
		}
	}

	public void Join(TimeSpan? timeout = null)
	{
		Stop();
		if (timeout.HasValue)
		{
			thread?.Join(timeout.Value);
		}
		else
		{
			thread?.Join();
		}
	}

	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			isDisposed = true;
			Stop();
		}
	}

	private void Step(ISteppable s)
	{
		while (s.Clock < cpu.Clock)
		{
			s.Step();
		}
	}
}