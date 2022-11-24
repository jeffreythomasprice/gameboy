using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : ISteppable
{
	private readonly IMemory memory;
	private readonly CPU cpu;
	private readonly SerialIO serialIO;
	private readonly Keypad keypad;

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
		memory = cartridge.CreateMemory();
		cpu = new CPU(loggerFactory, memory);
		serialIO = new SerialIO(loggerFactory, memory);
		keypad = new Keypad(loggerFactory, memory);

		serialIO.DataAvailable += (value) =>
		{
			cpu.SerialIOCompleteInterrupt();
		};
		keypad.KeypadRegisterDelta += (oldValue, newValue) =>
		{
			cpu.KeypadInterrupt();
		};
	}

	public IMemory Memory => memory;

	public CPU CPU => cpu;

	public SerialIO SerialIO => serialIO;

	public Keypad Keypad => keypad;

	public ulong Clock => cpu.Clock;

	public void Reset()
	{
		memory.Reset();
		cpu.Reset();
		serialIO.Reset();
		keypad.Reset();
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
		cpu.Step();
	}
}