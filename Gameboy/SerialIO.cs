using Microsoft.Extensions.Logging;

namespace Gameboy;

public class SerialIO : ISteppable
{
	public delegate void DataAvailableDelegate(byte value);

	public event DataAvailableDelegate? OnDataAvailable;

	private ILogger logger;

	private UInt64 clock;
	private byte registerSB;
	private byte registerSC;

	// bits from SC
	private bool transferStartFlag;
	private bool clockModeFlagIsInternal;

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

	public byte RegisterSB
	{
		get => registerSB;
		set => registerSB = value;
	}

	public byte RegisterSC
	{
		get => registerSC;
		set
		{
			registerSC = value;
			transferStartFlag = (value & 0b1000_0000) != 0;
			clockModeFlagIsInternal = (value & 0b0000_0001) != 0;
		}
	}

	public void Reset()
	{
		Clock = 0;
		RegisterSB = 0;
		RegisterSC = 0;
		transferClocksRemaining = 0;
		outgoingByte = 0;
	}

	public void Step()
	{
		// if no transfer is in progress see if we need to start one
		if (TransferStartFlag && ClockModeFlagIsInternal && transferClocksRemaining == 0)
		{
			transferClocksRemaining = 8;
		}

		if (transferClocksRemaining == 0)
		{
			Clock += 4;
			return;
		}
		Clock++;

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
				TransferStartFlag = false;
				// emit the result
				OnDataAvailable?.Invoke(outgoingByte);
				outgoingByte = 0;
			}
		}
	}

	private bool TransferStartFlag
	{
		get => transferStartFlag;
		set
		{
			if (value)
			{
				RegisterSC |= 0b1000_0000;
			}
			else
			{
				RegisterSC &= 0b0111_1111;
			}
		}
	}

	private bool ClockModeFlagIsInternal
	{
		get => clockModeFlagIsInternal;
		set
		{
			if (value)
			{
				RegisterSC |= 0b0000_0001;
			}
			else
			{
				RegisterSC &= 0b1111_1110;
			}
		}
	}
}