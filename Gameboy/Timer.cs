using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Timer : ISteppable
{
	public delegate void OverflowDelegate();

	public event OverflowDelegate? Overflow;

	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;
	private byte divLow;
	private byte lastDivHigh;

	public Timer(ILoggerFactory loggerFactory, IMemory memory)
	{
		logger = loggerFactory.CreateLogger<Timer>();
		this.memory = memory;
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		clock = 0;
		divLow = 0;
		lastDivHigh = 0;
	}

	public void Step()
	{
		Clock++;

		/*
		note that reference material seems fond of using Hz as a timing instead of CPU clocks, but different documents seem to disagree
		about what those numbers should be
		assuming here that nice round numbers of clock cycles are the real values

		DIV is always incrementing every 256 CPU clocks
		this means that the low byte of the current clock as the low byte of a 16-bit unsigned integer, where DIV is the upper byte
		the full 16-bit DIV is therefore incrementing every 1 CPU clock

		DIV isn't controllable, it just always goes up
		*/
		var divHigh = memory.ReadUInt8(Memory.IO_DIV);
		// if div was written to reset it
		if (divHigh != lastDivHigh)
		{
			divHigh = 0;
			divLow = 0;
			logger.LogTrace("DIV reset");
		}
		var div16Before = (UInt16)((divHigh << 8) | divLow);
		var div16After = (UInt16)(div16Before + 1);
		lastDivHigh = (byte)((div16After & 0xff00) >> 8);
		memory.WriteUInt8(Memory.IO_DIV, lastDivHigh);
		divLow = (byte)(div16After & 0xff);

		/*
		TAC, TMA, TIMA
		if TAC enabled, increment TIMA based on TAC clock speed selector
		if overflows, load TMA into TIMA
		*/
		var tac = memory.ReadUInt8(Memory.IO_TAC);
		var enabled = (tac & 0b0000_0100) != 0;
		if (enabled)
		{
			// if incrementing 16-bit div overflows specific bits, that means increment TIMA
			var maskToCheck = (tac & 0b0000_0011) switch
			{
				// every 1024 CPU clocks, so check bit 9 of 16-bit DIV
				0b0000_0000 => 0b0000_0010_0000_0000,
				// every 16 CPU clocks, so check bit 3 of 16-bit DIV
				0b0000_0001 => 0b0000_0000_0000_1000,
				// every 64 CPU clocks, so check bit 5 of 16-bit DIV
				0b0000_0010 => 0b0000_0000_0010_0000,
				// every 256 CPU clocks, so check bit 7 of 16-bit DIV
				// 0b0000_0011
				_ => 0b0000_0000_1000_0000,
			};
			// did that bit go from high to low?
			if (((div16Before & maskToCheck) != 0) && ((div16After & maskToCheck) == 0))
			{
				// increment TIMA
				var timaBefore = memory.ReadUInt8(Memory.IO_TIMA);
				var timaAfter = (byte)(timaBefore + 1);
				if (timaAfter == 0)
				{
					logger.LogTrace("timer overflow");
					timaAfter = memory.ReadUInt8(Memory.IO_TMA);
					Overflow?.Invoke();
					// set interrupt flag
					memory.WriteUInt8(Memory.IO_IF, (byte)(memory.ReadUInt8(Memory.IO_IF) | Memory.IF_MASK_TIMER));
				}
				memory.WriteUInt8(Memory.IO_TIMA, timaAfter);
			}
		}
	}
}