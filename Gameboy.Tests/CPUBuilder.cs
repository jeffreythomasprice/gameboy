using Microsoft.Extensions.Logging;

namespace Gameboy.Tests;

public class CPUBuilder
{
	private CPU cpu;

	public CPUBuilder(ILoggerFactory loggerFactory, IMemory memory)
	{
		cpu = new CPU(loggerFactory, memory);
	}

	public CPU CPU => cpu;

	public CPUBuilder Copy(CPU source)
	{
		cpu.RegisterA = source.RegisterA;
		cpu.RegisterB = source.RegisterB;
		cpu.RegisterC = source.RegisterC;
		cpu.RegisterD = source.RegisterD;
		cpu.RegisterE = source.RegisterE;
		cpu.RegisterF = source.RegisterF;
		cpu.RegisterH = source.RegisterH;
		cpu.RegisterL = source.RegisterL;
		cpu.RegisterSP = source.RegisterSP;
		cpu.RegisterPC = source.RegisterPC;
		cpu.Clock = source.Clock;
		cpu.IsStopped = source.IsStopped;
		cpu.IsHalted = source.IsHalted;
		cpu.IME = source.IME;
		return this;
	}

	public CPUBuilder RegisterA(byte value)
	{
		cpu.RegisterA = value;
		return this;
	}

	public CPUBuilder RegisterB(byte value)
	{
		cpu.RegisterB = value;
		return this;
	}

	public CPUBuilder RegisterC(byte value)
	{
		cpu.RegisterC = value;
		return this;
	}

	public CPUBuilder RegisterD(byte value)
	{
		cpu.RegisterD = value;
		return this;
	}

	public CPUBuilder RegisterE(byte value)
	{
		cpu.RegisterE = value;
		return this;
	}

	public CPUBuilder RegisterF(byte value)
	{
		cpu.RegisterF = value;
		return this;
	}

	public CPUBuilder RegisterH(byte value)
	{
		cpu.RegisterH = value;
		return this;
	}

	public CPUBuilder RegisterL(byte value)
	{
		cpu.RegisterL = value;
		return this;
	}

	public CPUBuilder RegisterSP(UInt16 value)
	{
		cpu.RegisterSP = value;
		return this;
	}

	public CPUBuilder RegisterPC(UInt16 value)
	{
		cpu.RegisterPC = value;
		return this;
	}

	public CPUBuilder RegisterAF(UInt16 value)
	{
		cpu.RegisterAF = value;
		return this;
	}

	public CPUBuilder RegisterBC(UInt16 value)
	{
		cpu.RegisterBC = value;
		return this;
	}

	public CPUBuilder RegisterDE(UInt16 value)
	{
		cpu.RegisterDE = value;
		return this;
	}

	public CPUBuilder RegisterHL(UInt16 value)
	{
		cpu.RegisterHL = value;
		return this;
	}

	public CPUBuilder ZeroFlag(bool value)
	{
		cpu.ZeroFlag = value;
		return this;
	}

	public CPUBuilder SubtractFlag(bool value)
	{
		cpu.SubtractFlag = value;
		return this;
	}

	public CPUBuilder HalfCarryFlag(bool value)
	{
		cpu.HalfCarryFlag = value;
		return this;
	}

	public CPUBuilder CarryFlag(bool value)
	{
		cpu.CarryFlag = value;
		return this;
	}

	public CPUBuilder AddPC(int delta)
	{
		cpu.RegisterPC = (UInt16)((int)cpu.RegisterPC + delta);
		return this;
	}

	public CPUBuilder AddClock(int delta)
	{
		cpu.Clock = cpu.Clock + (UInt64)delta;
		return this;
	}

	public CPUBuilder IsHalted(bool value)
	{
		cpu.IsHalted = value;
		return this;
	}

	public CPUBuilder IsStopped(bool value)
	{
		cpu.IsStopped = value;
		return this;
	}

	public CPUBuilder InterruptsEnabled(bool value)
	{
		cpu.IME = value;
		return this;
	}
}