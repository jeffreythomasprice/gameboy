namespace Gameboy;

using Microsoft.Extensions.Logging;
using static NumberUtils;

public class CPU
{
	public const UInt16 InitialPC = 0x0100;

	private const byte ZeroFlagMask = 0b1000_0000;
	private const byte SubtractFlagMask = 0b0100_0000;
	private const byte HalfCarryFlagMask = 0b0010_0000;
	private const byte CarryFlagMask = 0b0001_0000;

	private ILogger logger;
	private readonly IMemory memory;

	private byte registerA;
	private byte registerB;
	private byte registerC;
	private byte registerD;
	private byte registerE;
	private byte registerF;
	private byte registerH;
	private byte registerL;
	private UInt16 registerSP;
	private UInt16 registerPC;
	private UInt64 clock;

	public CPU(ILoggerFactory loggerFactory, IMemory memory)
	{
		this.logger = loggerFactory.CreateLogger(GetType());
		this.memory = memory;
		Reset();
	}

	public byte RegisterA
	{
		get => registerA;
		set => registerA = value;
	}

	public byte RegisterB
	{
		get => registerB;
		set => registerB = value;
	}

	public byte RegisterC
	{
		get => registerC;
		set => registerC = value;
	}

	public byte RegisterD
	{
		get => registerD;
		set => registerD = value;
	}

	public byte RegisterE
	{
		get => registerE;
		set => registerE = value;
	}

	public byte RegisterF
	{
		get => registerF;
		set => registerF = value;
	}

	public byte RegisterH
	{
		get => registerH;
		set => registerH = value;
	}

	public byte RegisterL
	{
		get => registerL;
		set => registerL = value;
	}

	public UInt16 RegisterSP
	{
		get => registerSP;
		set => registerSP = value;
	}

	public UInt16 RegisterPC
	{
		get => registerPC;
		set => registerPC = value;
	}

	public UInt16 RegisterAF
	{
		get => (UInt16)((((UInt16)registerA) << 8) | (UInt16)registerF);
		set
		{
			registerA = (byte)((value & 0xff00) >> 8);
			registerF = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterBC
	{
		get => (UInt16)((((UInt16)registerB) << 8) | (UInt16)registerC);
		set
		{
			registerB = (byte)((value & 0xff00) >> 8);
			registerC = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterDE
	{
		get => (UInt16)((((UInt16)registerD) << 8) | (UInt16)registerE);
		set
		{
			registerD = (byte)((value & 0xff00) >> 8);
			registerE = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterHL
	{
		get => (UInt16)((((UInt16)registerH) << 8) | (UInt16)registerL);
		set
		{
			registerH = (byte)((value & 0xff00) >> 8);
			registerL = (byte)(value & 0xff);
		}
	}

	public bool ZeroFlag
	{
		get => (registerF & ZeroFlagMask) != 0;
		set
		{
			if (value)
			{
				registerF |= ZeroFlagMask;
			}
			else
			{
				registerF &= unchecked((byte)(~ZeroFlagMask));
			}
		}
	}

	public bool SubtractFlag
	{
		get => (registerF & SubtractFlagMask) != 0;
		set
		{
			if (value)
			{
				registerF |= SubtractFlagMask;
			}
			else
			{
				registerF &= unchecked((byte)(~SubtractFlagMask));
			}
		}
	}

	public bool HalfCarryFlag
	{
		get => (registerF & HalfCarryFlagMask) != 0;
		set
		{
			if (value)
			{
				registerF |= HalfCarryFlagMask;
			}
			else
			{
				registerF &= unchecked((byte)(~HalfCarryFlagMask));
			}
		}
	}

	public bool CarryFlag
	{
		get => (registerF & CarryFlagMask) != 0;
		set
		{
			if (value)
			{
				registerF |= CarryFlagMask;
			}
			else
			{
				registerF &= unchecked((byte)(~CarryFlagMask));
			}
		}
	}

	public UInt64 Clock
	{
		get => clock;
		set => clock = value;
	}

	public void Reset()
	{
		// TODO starting conditions
		registerA = 0;
		registerB = 0;
		registerC = 0;
		registerD = 0;
		registerE = 0;
		registerF = 0;
		registerH = 0;
		registerL = 0;
		registerSP = 0;
		registerPC = InitialPC;
		clock = 0;
	}

	public void ExecuteInstruction()
	{
		var instruction = ReadNextPCUInt8();
		switch (instruction)
		{
			case 0x00:
				logger.LogTrace("NOP");
				clock += 4;
				break;
			case 0x01:
			case 0x11:
			case 0x21:
			case 0x31:
				{
					var data = ReadNextPCUInt16();
					var registerIndex = (instruction & 0b0011_0000) >> 4;
					var (_, registerName) = registerIndex switch
					{
						0 => (RegisterBC = data, "BC"),
						1 => (RegisterDE = data, "DE"),
						2 => (RegisterHL = data, "HL"),
						_ => (RegisterSP = data, "SP"),
					};
					logger.LogTrace($"LD {registerName}, {ToHex(data)}");
					clock += 12;
				}
				break;
			case 0x02:
			case 0x12:
			case 0x22:
			case 0x32:
				{
					var reigsterIndex = (instruction & 0b0011_0000) >> 4;
					var (address, _, registerName) = reigsterIndex switch
					{
						0 => (RegisterBC, 0, "(BC)"),
						1 => (RegisterDE, 0, "(DE)"),
						2 => (RegisterHL, RegisterHL++, "(HL+)"),
						_ => (RegisterHL, RegisterHL--, "(HL-)"),
					};
					logger.LogTrace($"LD {registerName}, A");
					memory.Write(address, RegisterA);
					clock += 8;
				}
				break;
			default:
				throw new NotImplementedException($"unhandled instruction {ToHex(instruction)}");
		}
	}

	private byte ReadNextPCUInt8()
	{
		var result = memory.Read(RegisterPC);
		RegisterPC++;
		return result;
	}

	private UInt16 ReadNextPCUInt16()
	{
		var low = ReadNextPCUInt8();
		var high = ReadNextPCUInt8();
		return (UInt16)((high << 8) | low);
	}
}