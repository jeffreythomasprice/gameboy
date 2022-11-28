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
	private Stopwatch memoryStopwatch = new();
	private Stopwatch serialIOStopwatch = new();
	private Stopwatch keypadStopwatch = new();
	private Stopwatch timerStopwatch = new();
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

		totalStopwatch.Reset();
		memoryStopwatch.Reset();
		serialIOStopwatch.Reset();
		keypadStopwatch.Reset();
		timerStopwatch.Reset();
		videoStopwatch.Reset();
		cpuStopwatch.Reset();
	}

	public void Step()
	{
		totalStopwatch.Start();

		memoryStopwatch.Start();
		Step(memory);
		memoryStopwatch.Stop();

		serialIOStopwatch.Start();
		Step(serialIO);
		serialIOStopwatch.Stop();

		keypadStopwatch.Start();
		Step(keypad);
		keypadStopwatch.Stop();

		timerStopwatch.Start();
		Step(timer);
		timerStopwatch.Stop();

		videoStopwatch.Start();
		Step(video);
		videoStopwatch.Stop();

		cpuStopwatch.Start();
		cpu.Step();
		cpuStopwatch.Stop();

		totalStopwatch.Stop();

		if (cpu.Clock % (CPU.ClockTicksPerSecond * 5) == 0)
		{
			logger.LogDebug($"""
			TODO JEFF total = {totalStopwatch.Elapsed}
				memory = {StopwatchString(memoryStopwatch, totalStopwatch)}
				serialIO = {StopwatchString(serialIOStopwatch, totalStopwatch)}
				keypad = {StopwatchString(keypadStopwatch, totalStopwatch)}
				timer = {StopwatchString(timerStopwatch, totalStopwatch)}
				video = {StopwatchString(videoStopwatch, totalStopwatch)}
					tile data = {StopwatchString(video.TileDataReadStopwatch, videoStopwatch)}
					bg and window = {StopwatchString(video.BackgroundAndWindowStopwatch, videoStopwatch)}
					sprites = {StopwatchString(video.SpritesStopwatch, videoStopwatch)}
					emit scanline = {StopwatchString(video.EmitScanlineStopwatch, videoStopwatch)}
				cpu = {StopwatchString(cpuStopwatch, totalStopwatch)}
			""");

			string StopwatchString(Stopwatch s1, Stopwatch s2)
			{
				var percentage = ((double)s1.ElapsedTicks / (double)s2.ElapsedTicks * 100.0).ToString("N2");
				return $"{s1.Elapsed} ({percentage}%)";
			}
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