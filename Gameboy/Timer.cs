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

	private bool expectedDIVUpdate;

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
	}

	public void Step()
	{
		// TODO JEFF can we cheat time and do 4 clocks at a time?
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
		var divHigh = RegisterDIV;
		var div16Before = (UInt16)((divHigh << 8) | divLow);
		var div16After = (UInt16)(div16Before + 1);
		RegisterDIV = (byte)((div16After & 0xff00) >> 8);
		divLow = (byte)(div16After & 0xff);

		/*
		TAC, TMA, TIMA
		if TAC enabled, increment TIMA based on TAC clock speed selector
		if overflows, load TMA into TIMA
		*/
		var enabled = (RegisterTAC & 0b0000_0100) != 0;
		if (enabled)
		{
			// if incrementing 16-bit div overflows specific bits, that means increment TIMA
			var maskToCheck = (RegisterTAC & 0b0000_0011) switch
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
				var timaBefore = RegisterTIMA;
				var timaAfter = (byte)(timaBefore + 1);
				if (timaAfter == 0)
				{
#if DEBUG
					logger.LogTrace("timer overflow");
#endif
					timaAfter = RegisterTMA;
					Overflow?.Invoke();
					// set interrupt flag
					RegisterIF = (byte)(RegisterIF | Memory.IF_MASK_TIMER);
				}
				RegisterTIMA = timaAfter;
			}
		}
	}

	public void RegisterDIVWrite(byte oldValue, ref byte newValue)
	{
		// if update flag is true we're updating this as part of normal timer increments
		// if it's false somebody else wrote to DIV and the CPU expects a reset
		if (expectedDIVUpdate)
		{
			expectedDIVUpdate = false;
		}
		else
		{
#if DEBUG
			logger.LogTrace("DIV reset");
#endif
			divLow = 0;
			newValue = 0;
		}
	}

	private byte RegisterDIV
	{
		get => memory.ReadUInt8(Memory.IO_DIV);
		set
		{
			// we need to update this register value, but memory will emit events when we do
			// this flag gets checked when we're handling the memory write event
			expectedDIVUpdate = true;
			memory.WriteUInt8(Memory.IO_DIV, value);
		}
	}

	private byte RegisterTAC =>
		memory.ReadUInt8(Memory.IO_TAC);

	private byte RegisterTIMA
	{
		get => memory.ReadUInt8(Memory.IO_TIMA);
		set => memory.WriteUInt8(Memory.IO_TIMA, value);
	}

	private byte RegisterTMA =>
		memory.ReadUInt8(Memory.IO_TMA);

	private byte RegisterIF
	{
		get => memory.ReadUInt8(Memory.IO_IF);
		set => memory.WriteUInt8(Memory.IO_IF, value);
	}
}