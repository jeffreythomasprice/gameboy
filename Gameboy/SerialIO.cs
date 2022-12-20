using Microsoft.Extensions.Logging;

namespace Gameboy;

public class SerialIO : ISteppable
{
	public delegate void DataAvailableDelegate(byte value);

	public event DataAvailableDelegate? DataAvailable;

	private ILogger logger;

	private UInt64 clock;
	// how many clock ticks are required to finish the current byte
	private int transferClocksRemaining;
	private byte outgoingByte;

	public SerialIO(ILoggerFactory loggerFactory)
	{
		logger = loggerFactory.CreateLogger<SerialIO>();
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		Clock = 0;
		transferClocksRemaining = 0;
		outgoingByte = 0;
		RegisterSB = 0;
		RegisterSC = 0;
	}

	public void Step()
	{
		// TODO JEFF can we cheat time and do 4 clocks at a time?
		Clock++;

		// if no transfer is in progress see if we need to start one
		if (transferClocksRemaining == 0)
		{
			// 0 = external clock, 1 = internal clock
			var clockModeIsInternal = (RegisterSC & 0b0000_0001) != 0;
			// 1 = start a new transfer
			var transferStart = (RegisterSC & 0b1000_0000) != 0;
			if (transferStart && clockModeIsInternal)
			{
				transferClocksRemaining = 8;
			}
		}

		// is there a transfer in progress?
		if (transferClocksRemaining > 0)
		{
			// shift the MSB of the serial bus into our local
			outgoingByte = (byte)((outgoingByte << 1) | ((RegisterSB & 0b1000_0000) >> 7));
			// the serial bus has lost that MSB
			RegisterSB <<= 1;
			// advance time, see if we're done
			transferClocksRemaining--;
			if (transferClocksRemaining == 0)
			{
				// reset the transfer start flag
				RegisterSC &= 0b0111_1111;
				// emit the result
				DataAvailable?.Invoke(outgoingByte);
				outgoingByte = 0;
			}
		}
	}

	public byte RegisterSB { get; set; }

	public byte RegisterSC { get; set; }
}