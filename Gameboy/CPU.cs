namespace Gameboy;

using Microsoft.Extensions.Logging;
using static NumberUtils;

public class CPU
{
	private enum Register8
	{
		A,
		B,
		C,
		D,
		E,
		F,
		H,
		L,
	}

	private enum Register16
	{
		AF,
		BC,
		DE,
		HL,
		SP,
		PC,
	}

	private record struct Address(
		UInt16 Value,
		string Description
	)
	{
		public override string ToString() => $"({Description})";
	}

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
				{
					var data = ReadNextPCUInt16();
					SetToImmediate(Register16.BC, data);
				}
				break;
			case 0x11:
				{
					var data = ReadNextPCUInt16();
					SetToImmediate(Register16.DE, data);
				}
				break;
			case 0x21:
				{
					var data = ReadNextPCUInt16();
					SetToImmediate(Register16.HL, data);
				}
				break;
			case 0x31:
				{
					var data = ReadNextPCUInt16();
					SetToImmediate(Register16.SP, data);
				}
				break;

			case 0x02:
				{
					SetAddressToRegister(new Address(RegisterBC, Register16.BC.ToString()), Register8.A);
				}
				break;
			case 0x12:
				{
					SetAddressToRegister(new Address(RegisterDE, Register16.DE.ToString()), Register8.A);
				}
				break;
			case 0x22:
				{
					var destination = new Address(RegisterHL, $"{Register16.HL}+");
					RegisterHL++;
					SetAddressToRegister(destination, Register8.A);
				}
				break;
			case 0x32:
				{
					var destination = new Address(RegisterHL, $"{Register16.HL}-");
					RegisterHL--;
					SetAddressToRegister(destination, Register8.A);
				}
				break;

			case 0x0a:
				{
					SetRegisterToAddress(Register8.A, new Address(RegisterBC, Register16.BC.ToString()));
				}
				break;
			case 0x1a:
				{
					SetRegisterToAddress(Register8.A, new Address(RegisterDE, Register16.DE.ToString()));
				}
				break;
			case 0x2a:
				{
					var source = new Address(RegisterHL, $"{Register16.HL}+");
					RegisterHL++;
					SetRegisterToAddress(Register8.A, source);
				}
				break;
			case 0x3a:
				{
					var source = new Address(RegisterHL, $"{Register16.HL}-");
					RegisterHL--;
					SetRegisterToAddress(Register8.A, source);
				}
				break;

			case 0x03:
				{
					Increment(Register16.BC);
				}
				break;
			case 0x13:
				{
					Increment(Register16.DE);
				}
				break;
			case 0x23:
				{
					Increment(Register16.HL);
				}
				break;
			case 0x33:
				{
					Increment(Register16.SP);
				}
				break;

			case 0x0b:
				{
					Decrement(Register16.BC);
				}
				break;
			case 0x1b:
				{
					Decrement(Register16.DE);
				}
				break;
			case 0x2b:
				{
					Decrement(Register16.HL);
				}
				break;
			case 0x3b:
				{
					Decrement(Register16.SP);
				}
				break;

			case 0x04:
				{
					Increment(Register8.B);
				}
				break;
			case 0x0c:
				{
					Increment(Register8.C);
				}
				break;
			case 0x14:
				{
					Increment(Register8.D);
				}
				break;
			case 0x1c:
				{
					Increment(Register8.E);
				}
				break;
			case 0x24:
				{
					Increment(Register8.H);
				}
				break;
			case 0x2c:
				{
					Increment(Register8.L);
				}
				break;
			case 0x34:
				{
					Increment(new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x3c:
				{
					Increment(Register8.A);
				}
				break;

			case 0x05:
				{
					Decrement(Register8.B);
				}
				break;
			case 0x0d:
				{
					Decrement(Register8.C);
				}
				break;
			case 0x15:
				{
					Decrement(Register8.D);
				}
				break;
			case 0x1d:
				{
					Decrement(Register8.E);
				}
				break;
			case 0x25:
				{
					Decrement(Register8.H);
				}
				break;
			case 0x2d:
				{
					Decrement(Register8.L);
				}
				break;
			case 0x35:
				{
					Decrement(new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x3d:
				{
					Decrement(Register8.A);
				}
				break;

			case 0x06:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.B, data);
				}
				break;
			case 0x0e:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.C, data);
				}
				break;
			case 0x16:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.D, data);
				}
				break;
			case 0x1e:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.E, data);
				}
				break;
			case 0x26:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.H, data);
				}
				break;
			case 0x2e:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.L, data);
				}
				break;
			case 0x36:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(new Address(RegisterHL, Register16.HL.ToString()), data);
				}
				break;
			case 0x3e:
				{
					var data = ReadNextPCUInt8();
					SetToImmediate(Register8.A, data);
				}
				break;

			case 0x07:
				{
					logger.LogTrace("RLCA");
					var before = RegisterA;
					var after = (byte)((before << 1) | ((before & 0b1000_0000) >> 7));
					RegisterA = after;
					ZeroFlag = after == 0;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b1000_0000) != 0;
					clock += 4;
				}
				break;
			case 0x0f:
				{
					logger.LogTrace("RRCA");
					var before = RegisterA;
					var after = (byte)((before >> 1) | ((before & 0b0000_0001) << 7));
					RegisterA = after;
					ZeroFlag = after == 0;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b0000_0001) != 0;
					clock += 4;
				}
				break;
			case 0x17:
				{
					logger.LogTrace("RLA");
					var before = RegisterA;
					var after = (byte)((before << 1) | (CarryFlag ? 0b0000_0001 : 0));
					RegisterA = after;
					ZeroFlag = after == 0;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b1000_0000) != 0;
					clock += 4;
				}
				break;
			case 0x1f:
				{
					logger.LogTrace("RRA");
					var before = RegisterA;
					var after = (byte)((before >> 1) | (CarryFlag ? 0b1000_0000 : 0));
					RegisterA = after;
					ZeroFlag = after == 0;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b0000_0001) != 0;
					clock += 4;
				}
				break;

			case 0x08:
				{
					var address = ReadNextPCUInt16();
					logger.LogTrace($"LD ({ToHex(address)}), {Register16.SP}");
					memory.WriteUInt16(address, RegisterSP);
					clock += 20;
				}
				break;

			case 0x09:
				{
					Add(Register16.HL, Register16.BC);
				}
				break;
			case 0x19:
				{
					Add(Register16.HL, Register16.DE);
				}
				break;
			case 0x29:
				{
					Add(Register16.HL, Register16.HL);
				}
				break;
			case 0x39:
				{
					Add(Register16.HL, Register16.SP);
				}
				break;

			default:
				throw new NotImplementedException($"unhandled instruction {ToHex(instruction)}");
		}
	}

	private void SetToImmediate(Register8 destination, byte source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		SetRegister(destination, source);
		clock += 8;
	}

	private void SetToImmediate(Register16 destination, UInt16 source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		SetRegister(destination, source);
		clock += 12;
	}

	private void SetToImmediate(Address destination, byte source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		memory.WriteUInt8(destination.Value, source);
		clock += 12;
	}

	private void SetAddressToRegister(Address destination, Register8 source)
	{
		logger.LogTrace($"LD {destination}, {source}");
		memory.WriteUInt8(destination.Value, GetRegister(source));
		clock += 8;
	}

	private void SetRegisterToAddress(Register8 destination, Address source)
	{
		logger.LogTrace($"LD {destination}, {source}");
		SetRegister(destination, memory.ReadUInt8(source.Value));
		clock += 8;
	}

	private void Increment(Register8 destinationAndSource)
	{
		logger.LogTrace($"INC {destinationAndSource}");
		var before = GetRegister(destinationAndSource);
		var after = (byte)(before + 1);
		SetRegister(destinationAndSource, after);
		clock += 4;
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = (after & 0b0000_1111) < (before & 0b0000_1111);
	}

	private void Increment(Register16 destinationAndSource)
	{
		logger.LogTrace($"INC {destinationAndSource}");
		var before = GetRegister(destinationAndSource);
		var after = (UInt16)(before + 1);
		SetRegister(destinationAndSource, after);
		clock += 8;
	}

	private void Increment(Address destinationAndSource)
	{
		logger.LogTrace($"INC {destinationAndSource}");
		var before = memory.ReadUInt8(destinationAndSource.Value);
		var after = (byte)(before + 1);
		memory.WriteUInt8(destinationAndSource.Value, after);
		clock += 12;
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = (after & 0b0000_1111) < (before & 0b0000_1111);
	}

	private void Decrement(Register8 destinationAndSource)
	{
		logger.LogTrace($"DEC {destinationAndSource}");
		var before = GetRegister(destinationAndSource);
		var after = (byte)(before - 1);
		SetRegister(destinationAndSource, after);
		clock += 4;
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (after & 0b1111_0000) == (before & 0b1111_0000);
	}

	private void Decrement(Register16 destinationAndSource)
	{
		logger.LogTrace($"DEC {destinationAndSource}");
		var before = GetRegister(destinationAndSource);
		var after = (UInt16)(before - 1);
		SetRegister(destinationAndSource, after);
		clock += 8;
	}

	private void Decrement(Address destinationAndSource)
	{
		logger.LogTrace($"DEC {destinationAndSource}");
		var before = memory.ReadUInt8(destinationAndSource.Value);
		var after = (byte)(before - 1);
		memory.WriteUInt8(destinationAndSource.Value, after);
		clock += 12;
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (after & 0b1111_0000) == (before & 0b1111_0000);
	}

	private void Add(Register16 destination, Register16 source)
	{
		logger.LogTrace($"ADD {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (UInt32)before + (UInt32)sourceValue;
		SetRegister(destination, (UInt16)after);
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111_1111_1111) + (sourceValue & 0b0000_1111_1111_1111) > 0b0000_1111_1111_1111;
		CarryFlag = after > 0b1111_1111_1111_1111;
		clock += 8;
	}

	private byte GetRegister(Register8 r)
	{
		switch (r)
		{
			case Register8.A:
				return RegisterA;
			case Register8.B:
				return RegisterB;
			case Register8.C:
				return RegisterC;
			case Register8.D:
				return RegisterD;
			case Register8.E:
				return RegisterE;
			case Register8.F:
				return RegisterF;
			case Register8.H:
				return RegisterH;
			case Register8.L:
				return RegisterL;
			default:
				throw new ArgumentException($"unhandled {r}");
		}
	}

	private void SetRegister(Register8 r, byte value)
	{
		switch (r)
		{
			case Register8.A:
				RegisterA = value;
				break;
			case Register8.B:
				RegisterB = value;
				break;
			case Register8.C:
				RegisterC = value;
				break;
			case Register8.D:
				RegisterD = value;
				break;
			case Register8.E:
				RegisterE = value;
				break;
			case Register8.F:
				RegisterF = value;
				break;
			case Register8.H:
				RegisterH = value;
				break;
			case Register8.L:
				RegisterL = value;
				break;
			default:
				throw new ArgumentException($"unhandled {r}");
		}
	}

	private UInt16 GetRegister(Register16 r)
	{
		switch (r)
		{
			case Register16.AF:
				return RegisterAF;
			case Register16.BC:
				return RegisterBC;
			case Register16.DE:
				return RegisterDE;
			case Register16.HL:
				return RegisterHL;
			case Register16.SP:
				return RegisterSP;
			case Register16.PC:
				return RegisterPC;
			default:
				throw new ArgumentException($"unhandled {r}");
		}
	}

	private void SetRegister(Register16 r, UInt16 value)
	{
		switch (r)
		{
			case Register16.AF:
				RegisterAF = value;
				break;
			case Register16.BC:
				RegisterBC = value;
				break;
			case Register16.DE:
				RegisterDE = value;
				break;
			case Register16.HL:
				RegisterHL = value;
				break;
			case Register16.SP:
				RegisterSP = value;
				break;
			case Register16.PC:
				RegisterPC = value;
				break;
			default:
				throw new ArgumentException($"unhandled {r}");
		}
	}

	private byte ReadNextPCUInt8()
	{
		var result = memory.ReadUInt8(RegisterPC);
		RegisterPC++;
		return result;
	}

	private UInt16 ReadNextPCUInt16()
	{
		var result = memory.ReadUInt16(RegisterPC);
		RegisterPC += 2;
		return result;
	}
}