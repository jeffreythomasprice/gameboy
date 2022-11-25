using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : ISteppable
{
	private readonly IMemory memory;
	private readonly CPU cpu;
	private readonly SerialIO serialIO;
	private readonly Keypad keypad;
	private readonly Timer timer;

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
		memory = cartridge.CreateMemory();
		cpu = new CPU(loggerFactory, memory);
		serialIO = new SerialIO(loggerFactory, memory);
		keypad = new Keypad(loggerFactory, memory);
		timer = new Timer(loggerFactory, memory);

		keypad.KeypadRegisterDelta += (oldValue, newValue) =>
		{
			cpu.Resume();
		};
	}

	public IMemory Memory => memory;

	public CPU CPU => cpu;

	public SerialIO SerialIO => serialIO;

	public Keypad Keypad => keypad;

	public Timer Timer => timer;

	public ulong Clock => cpu.Clock;

	public void Reset()
	{
		memory.Reset();
		cpu.Reset();
		serialIO.Reset();
		keypad.Reset();
		timer.Reset();
	}

	public void Step()
	{
		while (memory.Clock < cpu.Clock)
		{
			memory.Step();
		}
		while (serialIO.Clock < cpu.Clock)
		{
			serialIO.Step();
		}
		while (keypad.Clock < cpu.Clock)
		{
			keypad.Step();
		}
		while (timer.Clock < cpu.Clock)
		{
			timer.Step();
		}
		cpu.Step();
	}
}