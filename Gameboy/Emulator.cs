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
		memory = cartridge.CreateMemory();
		cpu = new CPU(loggerFactory, memory);
		serialIO = new SerialIO(loggerFactory, memory);
		keypad = new Keypad(loggerFactory, memory);
		timer = new Timer(loggerFactory, memory);
		video = new Video(loggerFactory, memory);

		memory.IORegisterDIVWrite += (byte oldValue, ref byte newValue) =>
		{
			timer.ResetDIV();
			newValue = 0;
		};
		memory.IORegisterLYWrite += (byte oldValue, ref byte newValue) =>
		{
			video.ResetLY();
			newValue = 0;
		};
	}

	public IMemory Memory => memory;

	public CPU CPU => cpu;

	public SerialIO SerialIO => serialIO;

	public Keypad Keypad => keypad;

	public Timer Timer => timer;

	public Video Video => video;

	public ulong Clock => cpu.Clock;

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