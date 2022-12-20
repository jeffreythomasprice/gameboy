using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : IDisposable, ISteppable
{
	public delegate void OnTickDelegate(UInt64 clock);

	public event OnTickDelegate? OnTick;

	private readonly ILogger logger;

	private readonly SerialIO serialIO;
	private readonly Memory memory;
	private readonly CPU cpu;
	private readonly Keypad keypad;
	private readonly Timer timer;
	private readonly Video video;

	private bool isDisposed = false;
	private Thread? thread = null;
	private bool threadShouldExit;

	// TODO JEFF timing debugging
	private Stopwatch totalStopwatch = new();
	private Stopwatch serialIOStopwatch = new();
	private Stopwatch memoryStopwatch = new();
	private Stopwatch keypadStopwatch = new();
	private Stopwatch timerStopwatch = new();
	private Stopwatch videoStopwatch = new();
	private Stopwatch cpuStopwatch = new();
	private TimeSpan emitDebugInterval = TimeSpan.FromSeconds(2);
	private UInt64 nextEmitDebugClock;

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
		logger = loggerFactory.CreateLogger<Emulator>();

		serialIO = new SerialIO(loggerFactory);
		timer = new Timer(loggerFactory);
		memory = cartridge.CreateMemory(loggerFactory, serialIO, timer);
		cpu = new CPU(loggerFactory, memory);
		keypad = new Keypad(loggerFactory, memory);
		video = new Video(loggerFactory, memory);

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

	/// <summary>
	/// If true periodically logs debug info
	/// </summary>
	public bool EmitDebugStatsEnabled { get; set; } = false;

	/// <summary>
	/// The interval on which to emit debug info. Measured in CPU time, not real time.
	/// </summary>
	public TimeSpan EmitDebugInterval
	{
		get => emitDebugInterval;
		set
		{
			emitDebugInterval = value;
			nextEmitDebugClock = 0;
		}
	}

	public void Reset()
	{
		serialIO.Reset();
		memory.Reset();
		cpu.Reset();
		keypad.Reset();
		timer.Reset();
		video.Reset();

		totalStopwatch.Reset();
		serialIOStopwatch.Reset();
		memoryStopwatch.Reset();
		keypadStopwatch.Reset();
		timerStopwatch.Reset();
		videoStopwatch.Reset();
		cpuStopwatch.Reset();
		nextEmitDebugClock = 0;
	}

	public void Step()
	{
		totalStopwatch.Start();

		serialIOStopwatch.Start();
		Step(serialIO);
		serialIOStopwatch.Stop();

		memoryStopwatch.Start();
		Step(memory);
		memoryStopwatch.Stop();

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

		OnTick?.Invoke(Clock);

		if (EmitDebugStatsEnabled && Clock >= nextEmitDebugClock)
		{
			nextEmitDebugClock = Clock + TimeUtils.ToClockTicks(EmitDebugInterval);

			string cpuState;
			if (cpu.IsStopped)
			{
				cpuState = "STOP";
			}
			else if (cpu.IsHalted)
			{
				cpuState = "HALT";
			}
			else
			{
				cpuState = "active";
			}

			logger.LogDebug($"""
			CPU state = {cpuState}
			total time = {totalStopwatch.Elapsed} ({cpu.Clock} clock ticks)
				serialIO = {StopwatchString(serialIOStopwatch, totalStopwatch)}
				memory = {StopwatchString(memoryStopwatch, totalStopwatch)}
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
		while (s.Clock <= cpu.Clock)
		{
			s.Step();
		}
	}
}