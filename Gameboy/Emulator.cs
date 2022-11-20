using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Emulator : ISteppable
{
	private readonly IMemory memory;
	private readonly CPU cpu;
	private readonly SerialIO serialIO;

	public Emulator(ILoggerFactory loggerFactory, Cartridge cartridge)
	{
		memory = cartridge.CreateMemory();
		cpu = new CPU(loggerFactory, memory);
		serialIO = new SerialIO(loggerFactory, memory);

		serialIO.DataAvailable += (value) =>
		{
			cpu.SerialIOCompleteInterrupt();
		};
	}

	public IMemory Memory => memory;

	public CPU CPU => cpu;

	public SerialIO SerialIO => serialIO;

	public ulong Clock => cpu.Clock;

	public void Reset()
	{
		memory.Reset();
		cpu.Reset();
		serialIO.Reset();
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
		cpu.Step();
	}
}