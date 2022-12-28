using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Timer : ISteppable
{
	public delegate void OverflowDelegate();

	public event OverflowDelegate? OnOverflow;

	private readonly ILogger logger;

	private UInt64 clock;

	private byte registerDIV;
	private byte registerTIMA;
	private byte registerTMA;
	private byte registerTAC;

	private byte divLow;
	// the mask to apply to the full 16-bit div to determine if an overflow has occurred
	private UInt16 tacMask;

	public Timer(ILoggerFactory loggerFactory)
	{
		logger = loggerFactory.CreateLogger<Timer>();
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		clock = 0;

		RegisterDIV = 0;
		RegisterTIMA = 0;
		RegisterTMA = 0;
		RegisterTAC = 0;
	}

	public void Step()
	{
		// advancing multiple clock ticks at once is fine because the mask is never looking at the low few bits
		// we still detect overflow in the higher bits after adding a full instruction cycle at a time
		const int clocksToAdd = 4;
		Clock += clocksToAdd;

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
		var div16After = (UInt16)(div16Before + clocksToAdd);
		// don't use the accessor, we don't want to trigger the div clear behavior
		registerDIV = (byte)((div16After & 0xff00) >> 8);
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
			// did that bit go from high to low?
			if (((div16Before & tacMask) != 0) && ((div16After & tacMask) == 0))
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
					OnOverflow?.Invoke();
				}
				RegisterTIMA = timaAfter;
			}
		}
	}

	public byte RegisterDIV
	{
		get => registerDIV;
		set
		{
			registerDIV = 0;
			divLow = 0;
		}
	}

	public byte RegisterTIMA
	{
		get => registerTIMA;
		set => registerTIMA = value;
	}

	public byte RegisterTMA
	{
		get => registerTMA;
		set => registerTMA = value;
	}

	public byte RegisterTAC
	{
		get => registerTAC;
		set
		{
			registerTAC = value;
			tacMask = (RegisterTAC & 0b0000_0011) switch
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
		}
	}
}