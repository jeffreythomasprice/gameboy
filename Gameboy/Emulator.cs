using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : RepeatableTask, ISteppable
{
	public delegate void OnTickDelegate(UInt64 clock);

	public event OnTickDelegate? OnTick;

	private readonly ILogger logger;

	// TODO JEFF put in preprocessor
	private readonly StopwatchCollection stopwatchCollection;
	private readonly StopwatchCollection.IStopwatch totalStopwatch;
	private readonly StopwatchCollection.IStopwatch serialIOStopwatch;
	private readonly StopwatchCollection.IStopwatch timerStopwatch;
	private readonly StopwatchCollection.IStopwatch videoStopwatch;
	private readonly StopwatchCollection.IStopwatch soundStopwatch;
	private readonly StopwatchCollection.IStopwatch keypadStopwatch;
	private readonly StopwatchCollection.IStopwatch interruptRegistersStopwatch;
	private readonly StopwatchCollection.IStopwatch memoryStopwatch;
	private readonly StopwatchCollection.IStopwatch cpuStopwatch;
	private TimeSpan emitDebugInterval = TimeSpan.FromSeconds(2);
	private UInt64 nextEmitDebugClock;

	private readonly SerialIO serialIO;
	private readonly Timer timer;
	private readonly Video video;
	private readonly Sound sound;
	private readonly Keypad keypad;
	private readonly InterruptRegisters interruptRegisters;
	private readonly Memory memory;
	private readonly CPU cpu;

	public Emulator(ILoggerFactory loggerFactory, StopwatchCollection stopwatchCollection, Cartridge cartridge, Video video) : base(loggerFactory)
	{
		logger = loggerFactory.CreateLogger<Emulator>();

		this.stopwatchCollection = stopwatchCollection;
		// TODO JEFF constants for names of stopwatches
		totalStopwatch = stopwatchCollection.Get("total");
		serialIOStopwatch = stopwatchCollection.Get("serial IO", "total");
		timerStopwatch = stopwatchCollection.Get("timer", "total");
		videoStopwatch = stopwatchCollection.Get("video", "total");
		soundStopwatch = stopwatchCollection.Get("sound", "total");
		keypadStopwatch = stopwatchCollection.Get("keypad", "total");
		interruptRegistersStopwatch = stopwatchCollection.Get("interrupt registers", "total");
		memoryStopwatch = stopwatchCollection.Get("memory", "total");
		cpuStopwatch = stopwatchCollection.Get("cpu", "total", new[] { "serial IO", "timer", "video", "sound", "keypad", "interrupt registers", "memory" });

		serialIO = new SerialIO(loggerFactory);
		timer = new Timer(loggerFactory);
		this.video = video;
		sound = new Sound(loggerFactory);
		keypad = new Keypad(loggerFactory);
		interruptRegisters = new InterruptRegisters(serialIO, timer, video, sound, keypad);
		memory = cartridge.CreateMemory(loggerFactory, serialIO, timer, video, sound, keypad, interruptRegisters);
		cpu = new CPU(
			loggerFactory,
			memory,
			interruptRegisters,
			() =>
			{
				serialIOStopwatch.Start();
				serialIO.StepTo(cpu!.Clock);
				serialIOStopwatch.Stop();

				timerStopwatch.Start();
				timer.StepTo(cpu.Clock);
				timerStopwatch.Stop();

				videoStopwatch.Start();
				video.StepTo(cpu.Clock);
				videoStopwatch.Stop();

				soundStopwatch.Start();
				sound.StepTo(cpu.Clock);
				soundStopwatch.Stop();

				keypadStopwatch.Start();
				keypad.StepTo(cpu.Clock);
				keypadStopwatch.Stop();

				interruptRegistersStopwatch.Start();
				interruptRegisters.StepTo(cpu.Clock);
				interruptRegistersStopwatch.Stop();

				memoryStopwatch.Start();
				memory.StepTo(cpu.Clock);
				memoryStopwatch.Stop();
			}
		);
	}

	public SerialIO SerialIO => serialIO;

	public Timer Timer => timer;

	public Sound Sound => sound;

	public Video Video => video;

	public Keypad Keypad => keypad;

	public InterruptRegisters InterruptRegisters => interruptRegisters;

	public IMemory Memory => memory;

	public CPU CPU => cpu;

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
		timer.Reset();
		video.Reset();
		sound.Reset();
		keypad.Reset();
		interruptRegisters.Reset();
		memory.Reset();
		cpu.Reset();

		totalStopwatch.Reset();
		serialIOStopwatch.Reset();
		timerStopwatch.Reset();
		videoStopwatch.Reset();
		soundStopwatch.Reset();
		interruptRegistersStopwatch.Reset();
		keypadStopwatch.Reset();
		memoryStopwatch.Reset();
		cpuStopwatch.Reset();
		nextEmitDebugClock = 0;
	}

	public void Step()
	{
		totalStopwatch.Start();

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

			var ratio = (TimeUtils.ToTimeSpan(cpu.Clock) / totalStopwatch.Elapsed * 100).ToString("0.00") + "%";
			logger.LogDebug($"""
			CPU state = {cpuState}
			total time = {totalStopwatch.Elapsed} ({cpu.Clock} clock ticks) ({ratio} real time)
			{stopwatchCollection}
			""");
		}
	}

	protected override void DisposeImpl()
	{
		interruptRegisters.Dispose();
	}

	protected override async Task ThreadRunImpl(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			Step();
			// TODO wait for configurable amounts of time if we want a speed other than "fastest possible"
			await Task.Delay(0, cancellationToken);
		}
	}
}