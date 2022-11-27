using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : ISteppable
{
	private readonly Memory memory;
	private readonly CPU cpu;
	private readonly SerialIO serialIO;
	private readonly Keypad keypad;
	private readonly Timer timer;
	private readonly Video video;

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
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
		Step(memory);
		Step(serialIO);
		Step(keypad);
		Step(timer);
		Step(video);
		cpu.Step();
	}

	private void Step(ISteppable s)
	{
		while (s.Clock < cpu.Clock)
		{
			s.Step();
		}
	}
}