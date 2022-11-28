using Microsoft.Extensions.Logging;

namespace Gameboy;

public class SerialIO : ISteppable
{
	public delegate void DataAvailableDelegate(byte value);

	public event DataAvailableDelegate? DataAvailable;

	private ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;
	// how many clock ticks are required to finish the current byte
	private int transferClocksRemaining;
	private byte outgoingByte;

	public SerialIO(ILoggerFactory loggerFactory, IMemory memory)
	{
		logger = loggerFactory.CreateLogger<SerialIO>();
		this.memory = memory;
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
	}

	public void Step()
	{
		// TODO JEFF can we cheat time and do 4 clocks at a time?
		Clock++;

		// current value of transfer control
		var serialControl = memory.ReadUInt8(Memory.IO_SC);

		// if no transfer is in progress see if we need to start one
		if (transferClocksRemaining == 0)
		{
			// 0 = external clock, 1 = internal clock
			var clockModeIsInternal = (serialControl & 0b0000_0001) != 0;
			// 1 = start a new transfer
			var transferStart = (serialControl & 0b1000_0000) != 0;
			if (transferStart && clockModeIsInternal)
			{
				transferClocksRemaining = 8;
			}
		}

		// is there a transfer in progress?
		if (transferClocksRemaining > 0)
		{
			// shift the next bit out
			var serialData = memory.ReadUInt8(Memory.IO_SB);
			// shift the MSB of the serial bus into our local
			outgoingByte = (byte)((outgoingByte << 1) | ((serialData & 0b1000_0000) >> 7));
			// the serial bus has lost that MSB
			serialData <<= 1;
			memory.WriteUInt8(Memory.IO_SB, serialData);
			// advance time, see if we're done
			transferClocksRemaining--;
			if (transferClocksRemaining == 0)
			{
				// reset the transfer start flag
				serialControl &= 0b0111_1111;
				memory.WriteUInt8(Memory.IO_SC, serialControl);
				// emit the result
				DataAvailable?.Invoke(outgoingByte);
				outgoingByte = 0;
				// set interrupt flag
				memory.WriteUInt8(Memory.IO_IF, (byte)(memory.ReadUInt8(Memory.IO_IF) | Memory.IF_MASK_SERIAL));
			}
		}
	}
}