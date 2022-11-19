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
	private bool isStopped;
	private bool isHalted;
	private bool interruptsEnabled;

	public CPU(ILoggerFactory loggerFactory, IMemory memory)
	{
		this.logger = loggerFactory.CreateLogger(GetType());
		this.memory = memory;
		Reset();
	}

	public byte RegisterA
	{
		get => registerA;
		internal set => registerA = value;
	}

	public byte RegisterB
	{
		get => registerB;
		internal set => registerB = value;
	}

	public byte RegisterC
	{
		get => registerC;
		internal set => registerC = value;
	}

	public byte RegisterD
	{
		get => registerD;
		internal set => registerD = value;
	}

	public byte RegisterE
	{
		get => registerE;
		internal set => registerE = value;
	}

	public byte RegisterF
	{
		get => registerF;
		internal set => registerF = value;
	}

	public byte RegisterH
	{
		get => registerH;
		internal set => registerH = value;
	}

	public byte RegisterL
	{
		get => registerL;
		internal set => registerL = value;
	}

	public UInt16 RegisterSP
	{
		get => registerSP;
		internal set => registerSP = value;
	}

	public UInt16 RegisterPC
	{
		get => registerPC;
		internal set => registerPC = value;
	}

	public UInt16 RegisterAF
	{
		get => (UInt16)((((UInt16)registerA) << 8) | (UInt16)registerF);
		internal set
		{
			registerA = (byte)((value & 0xff00) >> 8);
			registerF = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterBC
	{
		get => (UInt16)((((UInt16)registerB) << 8) | (UInt16)registerC);
		internal set
		{
			registerB = (byte)((value & 0xff00) >> 8);
			registerC = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterDE
	{
		get => (UInt16)((((UInt16)registerD) << 8) | (UInt16)registerE);
		internal set
		{
			registerD = (byte)((value & 0xff00) >> 8);
			registerE = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterHL
	{
		get => (UInt16)((((UInt16)registerH) << 8) | (UInt16)registerL);
		internal set
		{
			registerH = (byte)((value & 0xff00) >> 8);
			registerL = (byte)(value & 0xff);
		}
	}

	public bool ZeroFlag
	{
		get => (registerF & ZeroFlagMask) != 0;
		internal set
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
		internal set
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
		internal set
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
		internal set
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
		internal set => clock = value;
	}

	public bool IsStopped
	{
		get => isStopped;
		internal set => isStopped = value;
	}

	public bool IsHalted
	{
		get => isHalted;
		internal set => isHalted = value;
	}

	public bool InterruptsEnabled
	{
		get => interruptsEnabled;
		internal set => interruptsEnabled = value;
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
		isStopped = false;
		isHalted = false;
		interruptsEnabled = true;
	}

	public void Step()
	{
		if (IsStopped)
		{
			logger.LogTrace("skipping instruction, previous STOP");
			return;
		}
		if (IsHalted)
		{
			logger.LogTrace("skipping instruction, previous HALT");
			return;
		}
		ExecuteInstruction();
	}

	private void ExecuteInstruction()
	{
		var instruction = ReadNextPCUInt8();
		switch (instruction)
		{
			case 0x00:
				logger.LogTrace("NOP");
				Clock += 4;
				break;

			case 0x01:
				{
					var data = ReadNextPCUInt16();
					SetTo(Register16.BC, data);
				}
				break;
			case 0x11:
				{
					var data = ReadNextPCUInt16();
					SetTo(Register16.DE, data);
				}
				break;
			case 0x21:
				{
					var data = ReadNextPCUInt16();
					SetTo(Register16.HL, data);
				}
				break;
			case 0x31:
				{
					var data = ReadNextPCUInt16();
					SetTo(Register16.SP, data);
				}
				break;

			case 0x02:
				{
					SetTo(new Address(RegisterBC, Register16.BC.ToString()), Register8.A);
				}
				break;
			case 0x12:
				{
					SetTo(new Address(RegisterDE, Register16.DE.ToString()), Register8.A);
				}
				break;
			case 0x22:
				{
					var destination = new Address(RegisterHL, $"{Register16.HL}+");
					RegisterHL++;
					SetTo(destination, Register8.A);
				}
				break;
			case 0x32:
				{
					var destination = new Address(RegisterHL, $"{Register16.HL}-");
					RegisterHL--;
					SetTo(destination, Register8.A);
				}
				break;

			case 0x0a:
				{
					SetTo(Register8.A, new Address(RegisterBC, Register16.BC.ToString()));
				}
				break;
			case 0x1a:
				{
					SetTo(Register8.A, new Address(RegisterDE, Register16.DE.ToString()));
				}
				break;
			case 0x2a:
				{
					var source = new Address(RegisterHL, $"{Register16.HL}+");
					RegisterHL++;
					SetTo(Register8.A, source);
				}
				break;
			case 0x3a:
				{
					var source = new Address(RegisterHL, $"{Register16.HL}-");
					RegisterHL--;
					SetTo(Register8.A, source);
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
					SetTo(Register8.B, data);
				}
				break;
			case 0x0e:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.C, data);
				}
				break;
			case 0x16:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.D, data);
				}
				break;
			case 0x1e:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.E, data);
				}
				break;
			case 0x26:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.H, data);
				}
				break;
			case 0x2e:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.L, data);
				}
				break;
			case 0x36:
				{
					var data = ReadNextPCUInt8();
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), data);
				}
				break;
			case 0x3e:
				{
					var data = ReadNextPCUInt8();
					SetTo(Register8.A, data);
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
					Clock += 4;
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
					Clock += 4;
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
					Clock += 4;
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
					Clock += 4;
				}
				break;

			case 0x08:
				{
					var address = ReadNextPCUInt16();
					logger.LogTrace($"LD ({ToHex(address)}), {Register16.SP}");
					memory.WriteUInt16(address, RegisterSP);
					Clock += 20;
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

			case 0x10:
				{
					logger.LogTrace("STOP");
					// convention is that the next byte is 0x00, but unchecked
					ReadNextPCUInt8();
					IsStopped = true;
					Clock += 4;
				}
				break;

			case 0x18:
				{
					var delta = ReadNextPCInt8();
					logger.LogTrace($"JR {delta}");
					RegisterPC = (UInt16)((int)RegisterPC + (int)delta);
					Clock += 12;
				}
				break;
			case 0x20:
				{
					ConditionalJumpInt8(!ZeroFlag, "NZ", ReadNextPCInt8());
				}
				break;
			case 0x28:
				{
					ConditionalJumpInt8(ZeroFlag, "Z", ReadNextPCInt8());
				}
				break;
			case 0x30:
				{
					ConditionalJumpInt8(!CarryFlag, "NC", ReadNextPCInt8());
				}
				break;
			case 0x38:
				{
					ConditionalJumpInt8(CarryFlag, "C", ReadNextPCInt8());
				}
				break;

			case 0x27:
				{
					// https://forums.nesdev.org/viewtopic.php?p=196282&sid=20ffd9ebbfc1973358a81b9a3c59857b#p196282
					logger.LogTrace("DAA");
					if (SubtractFlag)
					{
						if (CarryFlag)
						{
							RegisterA -= 0x60;
						}
						if (HalfCarryFlag)
						{
							RegisterA -= 0x06;
						}
					}
					else
					{
						if (CarryFlag || RegisterA > 0x99)
						{
							RegisterA += 0x60;
							CarryFlag = true;
						}
						if (HalfCarryFlag || (RegisterA & 0x0f) > 0x09)
						{
							RegisterA += 0x06;
						}
					}
					ZeroFlag = RegisterA == 0;
					HalfCarryFlag = false;
					Clock += 4;
				}
				break;

			case 0x2f:
				{
					logger.LogTrace("CPL");
					RegisterA = (byte)(~RegisterA);
					SubtractFlag = true;
					HalfCarryFlag = true;
					Clock += 4;
				}
				break;

			case 0x37:
				{
					logger.LogTrace("SCF");
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = true;
					Clock += 4;
				}
				break;
			case 0x3f:
				{
					logger.LogTrace("CCF");
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = !CarryFlag;
					Clock += 4;
				}
				break;

			case 0x40:
				{
					SetTo(Register8.B, Register8.B);
				}
				break;
			case 0x41:
				{
					SetTo(Register8.B, Register8.C);
				}
				break;
			case 0x42:
				{
					SetTo(Register8.B, Register8.D);
				}
				break;
			case 0x43:
				{
					SetTo(Register8.B, Register8.E);
				}
				break;
			case 0x44:
				{
					SetTo(Register8.B, Register8.H);
				}
				break;
			case 0x45:
				{
					SetTo(Register8.B, Register8.L);
				}
				break;
			case 0x46:
				{
					SetTo(Register8.B, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x47:
				{
					SetTo(Register8.B, Register8.A);
				}
				break;

			case 0x48:
				{
					SetTo(Register8.C, Register8.B);
				}
				break;
			case 0x49:
				{
					SetTo(Register8.C, Register8.C);
				}
				break;
			case 0x4a:
				{
					SetTo(Register8.C, Register8.D);
				}
				break;
			case 0x4b:
				{
					SetTo(Register8.C, Register8.E);
				}
				break;
			case 0x4c:
				{
					SetTo(Register8.C, Register8.H);
				}
				break;
			case 0x4d:
				{
					SetTo(Register8.C, Register8.L);
				}
				break;
			case 0x4e:
				{
					SetTo(Register8.C, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x4f:
				{
					SetTo(Register8.C, Register8.A);
				}
				break;

			case 0x50:
				{
					SetTo(Register8.D, Register8.B);
				}
				break;
			case 0x51:
				{
					SetTo(Register8.D, Register8.C);
				}
				break;
			case 0x52:
				{
					SetTo(Register8.D, Register8.D);
				}
				break;
			case 0x53:
				{
					SetTo(Register8.D, Register8.E);
				}
				break;
			case 0x54:
				{
					SetTo(Register8.D, Register8.H);
				}
				break;
			case 0x55:
				{
					SetTo(Register8.D, Register8.L);
				}
				break;
			case 0x56:
				{
					SetTo(Register8.D, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x57:
				{
					SetTo(Register8.D, Register8.A);
				}
				break;

			case 0x58:
				{
					SetTo(Register8.E, Register8.B);
				}
				break;
			case 0x59:
				{
					SetTo(Register8.E, Register8.C);
				}
				break;
			case 0x5a:
				{
					SetTo(Register8.E, Register8.D);
				}
				break;
			case 0x5b:
				{
					SetTo(Register8.E, Register8.E);
				}
				break;
			case 0x5c:
				{
					SetTo(Register8.E, Register8.H);
				}
				break;
			case 0x5d:
				{
					SetTo(Register8.E, Register8.L);
				}
				break;
			case 0x5e:
				{
					SetTo(Register8.E, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x5f:
				{
					SetTo(Register8.E, Register8.A);
				}
				break;

			case 0x60:
				{
					SetTo(Register8.H, Register8.B);
				}
				break;
			case 0x61:
				{
					SetTo(Register8.H, Register8.C);
				}
				break;
			case 0x62:
				{
					SetTo(Register8.H, Register8.D);
				}
				break;
			case 0x63:
				{
					SetTo(Register8.H, Register8.E);
				}
				break;
			case 0x64:
				{
					SetTo(Register8.H, Register8.H);
				}
				break;
			case 0x65:
				{
					SetTo(Register8.H, Register8.L);
				}
				break;
			case 0x66:
				{
					SetTo(Register8.H, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x67:
				{
					SetTo(Register8.H, Register8.A);
				}
				break;

			case 0x68:
				{
					SetTo(Register8.L, Register8.B);
				}
				break;
			case 0x69:
				{
					SetTo(Register8.L, Register8.C);
				}
				break;
			case 0x6a:
				{
					SetTo(Register8.L, Register8.D);
				}
				break;
			case 0x6b:
				{
					SetTo(Register8.L, Register8.E);
				}
				break;
			case 0x6c:
				{
					SetTo(Register8.L, Register8.H);
				}
				break;
			case 0x6d:
				{
					SetTo(Register8.L, Register8.L);
				}
				break;
			case 0x6e:
				{
					SetTo(Register8.L, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x6f:
				{
					SetTo(Register8.L, Register8.A);
				}
				break;

			case 0x70:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.B);
				}
				break;
			case 0x71:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.C);
				}
				break;
			case 0x72:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.D);
				}
				break;
			case 0x73:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.E);
				}
				break;
			case 0x74:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.H);
				}
				break;
			case 0x75:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.L);
				}
				break;
			case 0x77:
				{
					SetTo(new Address(RegisterHL, Register16.HL.ToString()), Register8.A);
				}
				break;

			case 0x78:
				{
					SetTo(Register8.A, Register8.B);
				}
				break;
			case 0x79:
				{
					SetTo(Register8.A, Register8.C);
				}
				break;
			case 0x7a:
				{
					SetTo(Register8.A, Register8.D);
				}
				break;
			case 0x7b:
				{
					SetTo(Register8.A, Register8.E);
				}
				break;
			case 0x7c:
				{
					SetTo(Register8.A, Register8.H);
				}
				break;
			case 0x7d:
				{
					SetTo(Register8.A, Register8.L);
				}
				break;
			case 0x7e:
				{
					SetTo(Register8.A, new Address(RegisterHL, RegisterHL.ToString()));
				}
				break;
			case 0x7f:
				{
					SetTo(Register8.A, Register8.A);
				}
				break;

			case 0x76:
				{
					logger.LogTrace("HALT");
					IsHalted = true;
					Clock += 4;
				}
				break;

			case 0x80:
				{
					Add(Register8.A, Register8.B);
				}
				break;
			case 0x81:
				{
					Add(Register8.A, Register8.C);
				}
				break;
			case 0x82:
				{
					Add(Register8.A, Register8.D);
				}
				break;
			case 0x83:
				{
					Add(Register8.A, Register8.E);
				}
				break;
			case 0x84:
				{
					Add(Register8.A, Register8.H);
				}
				break;
			case 0x85:
				{
					Add(Register8.A, Register8.L);
				}
				break;
			case 0x86:
				{
					Add(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x87:
				{
					Add(Register8.A, Register8.A);
				}
				break;

			case 0x88:
				{
					Add(Register8.A, Register8.B, CarryFlag);
				}
				break;
			case 0x89:
				{
					Add(Register8.A, Register8.C, CarryFlag);
				}
				break;
			case 0x8a:
				{
					Add(Register8.A, Register8.D, CarryFlag);
				}
				break;
			case 0x8b:
				{
					Add(Register8.A, Register8.E, CarryFlag);
				}
				break;
			case 0x8c:
				{
					Add(Register8.A, Register8.H, CarryFlag);
				}
				break;
			case 0x8d:
				{
					Add(Register8.A, Register8.L, CarryFlag);
				}
				break;
			case 0x8e:
				{
					Add(Register8.A, new Address(RegisterHL, Register16.HL.ToString()), CarryFlag);
				}
				break;
			case 0x8f:
				{
					Add(Register8.A, Register8.A, CarryFlag);
				}
				break;

			case 0x90:
				{
					Subtract(Register8.A, Register8.B);
				}
				break;
			case 0x91:
				{
					Subtract(Register8.A, Register8.C);
				}
				break;
			case 0x92:
				{
					Subtract(Register8.A, Register8.D);
				}
				break;
			case 0x93:
				{
					Subtract(Register8.A, Register8.E);
				}
				break;
			case 0x94:
				{
					Subtract(Register8.A, Register8.H);
				}
				break;
			case 0x95:
				{
					Subtract(Register8.A, Register8.L);
				}
				break;
			case 0x96:
				{
					Subtract(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x97:
				{
					Subtract(Register8.A, Register8.A);
				}
				break;

			case 0x98:
				{
					Subtract(Register8.A, Register8.B, CarryFlag);
				}
				break;
			case 0x99:
				{
					Subtract(Register8.A, Register8.C, CarryFlag);
				}
				break;
			case 0x9a:
				{
					Subtract(Register8.A, Register8.D, CarryFlag);
				}
				break;
			case 0x9b:
				{
					Subtract(Register8.A, Register8.E, CarryFlag);
				}
				break;
			case 0x9c:
				{
					Subtract(Register8.A, Register8.H, CarryFlag);
				}
				break;
			case 0x9d:
				{
					Subtract(Register8.A, Register8.L, CarryFlag);
				}
				break;
			case 0x9e:
				{
					Subtract(Register8.A, new Address(RegisterHL, Register16.HL.ToString()), CarryFlag);
				}
				break;
			case 0x9f:
				{
					Subtract(Register8.A, Register8.A, CarryFlag);
				}
				break;

			case 0xa0:
				{
					And(Register8.A, Register8.B);
				}
				break;
			case 0xa1:
				{
					And(Register8.A, Register8.C);
				}
				break;
			case 0xa2:
				{
					And(Register8.A, Register8.D);
				}
				break;
			case 0xa3:
				{
					And(Register8.A, Register8.E);
				}
				break;
			case 0xa4:
				{
					And(Register8.A, Register8.H);
				}
				break;
			case 0xa5:
				{
					And(Register8.A, Register8.L);
				}
				break;
			case 0xa6:
				{
					And(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0xa7:
				{
					And(Register8.A, Register8.A);
				}
				break;

			case 0xa8:
				{
					Xor(Register8.A, Register8.B);
				}
				break;
			case 0xa9:
				{
					Xor(Register8.A, Register8.C);
				}
				break;
			case 0xaa:
				{
					Xor(Register8.A, Register8.D);
				}
				break;
			case 0xab:
				{
					Xor(Register8.A, Register8.E);
				}
				break;
			case 0xac:
				{
					Xor(Register8.A, Register8.H);
				}
				break;
			case 0xad:
				{
					Xor(Register8.A, Register8.L);
				}
				break;
			case 0xae:
				{
					Xor(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0xaf:
				{
					Xor(Register8.A, Register8.A);
				}
				break;

			case 0xb0:
				{
					Or(Register8.A, Register8.B);
				}
				break;
			case 0xb1:
				{
					Or(Register8.A, Register8.C);
				}
				break;
			case 0xb2:
				{
					Or(Register8.A, Register8.D);
				}
				break;
			case 0xb3:
				{
					Or(Register8.A, Register8.E);
				}
				break;
			case 0xb4:
				{
					Or(Register8.A, Register8.H);
				}
				break;
			case 0xb5:
				{
					Or(Register8.A, Register8.L);
				}
				break;
			case 0xb6:
				{
					Or(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0xb7:
				{
					Or(Register8.A, Register8.A);
				}
				break;

			case 0xb8:
				{
					Compare(Register8.A, Register8.B);
				}
				break;
			case 0xb9:
				{
					Compare(Register8.A, Register8.C);
				}
				break;
			case 0xba:
				{
					Compare(Register8.A, Register8.D);
				}
				break;
			case 0xbb:
				{
					Compare(Register8.A, Register8.E);
				}
				break;
			case 0xbc:
				{
					Compare(Register8.A, Register8.H);
				}
				break;
			case 0xbd:
				{
					Compare(Register8.A, Register8.L);
				}
				break;
			case 0xbe:
				{
					Compare(Register8.A, new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0xbf:
				{
					Compare(Register8.A, Register8.A);
				}
				break;

			case 0xc0:
				{
					ConditionalReturn(!ZeroFlag, "NZ");
				}
				break;
			case 0xc8:
				{
					ConditionalReturn(ZeroFlag, "Z");
				}
				break;
			case 0xd0:
				{
					ConditionalReturn(!CarryFlag, "NC");
				}
				break;
			case 0xd8:
				{
					ConditionalReturn(CarryFlag, "C");
				}
				break;

			case 0xc1:
				{
					Pop(Register16.BC);
				}
				break;
			case 0xd1:
				{
					Pop(Register16.DE);
				}
				break;
			case 0xe1:
				{
					Pop(Register16.HL);
				}
				break;
			case 0xf1:
				{
					Pop(Register16.AF);
				}
				break;

			case 0xc2:
				{
					var address = ReadNextPCUInt16();
					ConditionalJumpUInt16(!ZeroFlag, "NZ", address);
				}
				break;
			case 0xca:
				{
					var address = ReadNextPCUInt16();
					ConditionalJumpUInt16(ZeroFlag, "Z", address);
				}
				break;
			case 0xd2:
				{
					var address = ReadNextPCUInt16();
					ConditionalJumpUInt16(!CarryFlag, "NC", address);
				}
				break;
			case 0xda:
				{
					var address = ReadNextPCUInt16();
					ConditionalJumpUInt16(CarryFlag, "C", address);
				}
				break;

			case 0xc3:
				{
					var address = ReadNextPCUInt16();
					JumpUInt16(address);
				}
				break;

			case 0xc4:
				{
					var address = ReadNextPCUInt16();
					ConditionalCallUInt16(!ZeroFlag, "NZ", address);
				}
				break;
			case 0xcc:
				{
					var address = ReadNextPCUInt16();
					ConditionalCallUInt16(ZeroFlag, "Z", address);
				}
				break;
			case 0xd4:
				{
					var address = ReadNextPCUInt16();
					ConditionalCallUInt16(!CarryFlag, "NC", address);
				}
				break;
			case 0xdc:
				{
					var address = ReadNextPCUInt16();
					ConditionalCallUInt16(CarryFlag, "C", address);
				}
				break;

			case 0xc5:
				{
					Push(Register16.BC);
				}
				break;
			case 0xd5:
				{
					Push(Register16.DE);
				}
				break;
			case 0xe5:
				{
					Push(Register16.HL);
				}
				break;
			case 0xf5:
				{
					Push(Register16.AF);
				}
				break;

			case 0xc6:
				{
					var data = ReadNextPCUInt8();
					Add(Register8.A, data);
				}
				break;
			case 0xce:
				{
					var data = ReadNextPCUInt8();
					Add(Register8.A, data, CarryFlag);
				}
				break;
			case 0xd6:
				{
					var data = ReadNextPCUInt8();
					Subtract(Register8.A, data);
				}
				break;
			case 0xde:
				{
					var data = ReadNextPCUInt8();
					Subtract(Register8.A, data, CarryFlag);
				}
				break;
			case 0xe6:
				{
					var data = ReadNextPCUInt8();
					And(Register8.A, data);
				}
				break;
			case 0xee:
				{
					var data = ReadNextPCUInt8();
					Xor(Register8.A, data);
				}
				break;
			case 0xf6:
				{
					var data = ReadNextPCUInt8();
					Or(Register8.A, data);
				}
				break;
			case 0xfe:
				{
					var data = ReadNextPCUInt8();
					Compare(Register8.A, data);
				}
				break;

			case 0xc7:
				{
					RestartCall(0x00);
				}
				break;
			case 0xcf:
				{
					RestartCall(0x08);
				}
				break;
			case 0xd7:
				{
					RestartCall(0x10);
				}
				break;
			case 0xdf:
				{
					RestartCall(0x18);
				}
				break;
			case 0xe7:
				{
					RestartCall(0x20);
				}
				break;
			case 0xef:
				{
					RestartCall(0x28);
				}
				break;
			case 0xf7:
				{
					RestartCall(0x30);
				}
				break;
			case 0xff:
				{
					RestartCall(0x38);
				}
				break;

			case 0xc9:
				{
					Return();
				}
				break;
			case 0xd9:
				{
					ReturnAndEnableInterrupts();
				}
				break;

			case 0xcb:
				{
					ExecutePrefixInstruction();
				}
				break;

			case 0xcd:
				{
					var address = ReadNextPCUInt16();
					Call(address);
				}
				break;

			case 0xe0:
				{
					// TODO JEFF LDH (a8), A
				}
				break;
			case 0xf0:
				{
					// TODO JEFF LDH A, (a8)
				}
				break;

			case 0xe2:
				{
					// TODO JEFF LC (C), A
				}
				break;
			case 0xf2:
				{
					// TODO JEFF LC A, (C)
				}
				break;

			case 0xe8:
				{
					// TODO JEFF ADD SP, r8
				}
				break;

			case 0xe9:
				{
					// TODO JEFF ADD SP, r8
				}
				break;

			case 0xea:
				{
					// TODO JEFF LD (a16), A
				}
				break;
			case 0xfa:
				{
					// TODO JEFF LD A, (a16)
				}
				break;

			case 0xf3:
				{
					// TODO JEFF DI
				}
				break;
			case 0xfb:
				{
					// TODO JEFF EI
				}
				break;

			case 0xf8:
				{
					// TODO JEFF LD HL, SP + r8
				}
				break;
			case 0xf9:
				{
					// TODO JEFF LD SP, HL
				}
				break;


			case 0xd3:
			case 0xdb:
			case 0xdd:
			case 0xe3:
			case 0xe4:
			case 0xeb:
			case 0xec:
			case 0xed:
			case 0xf4:
			case 0xfc:
			case 0xfd:
				{
					logger.LogWarning($"INVALID OPCODE {ToHex(instruction)}");
					Clock += 4;
				}
				break;

			default:
				throw new NotImplementedException($"unhandled instruction {ToHex(instruction)}");
		}
	}

	private void ExecutePrefixInstruction()
	{
		// TODO implement the 0xcb prefix instructions
	}

	private void ConditionalJumpInt8(bool condition, string conditionString, sbyte delta)
	{
		logger.LogTrace($"JR {condition}, {delta}");
		if (condition)
		{
			RegisterPC = (UInt16)((int)RegisterPC + (int)delta);
			Clock += 12;
		}
		else
		{
			Clock += 8;
		}
	}

	private void ConditionalJumpUInt16(bool condition, string conditionString, UInt16 address)
	{
		logger.LogTrace($"JP {conditionString}, {ToHex(address)}");
		if (condition)
		{
			RegisterPC = address;
			Clock += 16;
		}
		else
		{
			Clock += 12;
		}
	}

	private void JumpUInt16(UInt16 address)
	{
		logger.LogTrace($"JP {ToHex(address)}");
		RegisterPC = address;
		Clock += 16;
	}

	private void ConditionalReturn(bool condition, string conditionString)
	{
		logger.LogTrace($"RET {conditionString}");
		if (condition)
		{
			RegisterPC = PopUInt16();
			Clock += 20;
		}
		else
		{
			Clock += 8;
		}
	}

	private void Return()
	{
		logger.LogTrace("RET");
		RegisterPC = PopUInt16();
		Clock += 16;
	}

	private void ReturnAndEnableInterrupts()
	{
		logger.LogTrace("RETI");
		RegisterPC = PopUInt16();
		interruptsEnabled = true;
		Clock += 16;
	}

	private void ConditionalCallUInt16(bool condition, string conditionString, UInt16 address)
	{
		logger.LogTrace($"CALL {conditionString}, {ToHex(address)}");
		if (condition)
		{
			Clock += 24;
			PushUInt16(RegisterPC);
			RegisterPC = address;
		}
		else
		{
			Clock += 12;
		}
	}

	private void Call(UInt16 address)
	{
		logger.LogTrace($"CALL {ToHex(address)}");
		PushUInt16(RegisterPC);
		RegisterPC = address;
		Clock += 24;
	}

	private void RestartCall(byte address)
	{
		logger.LogTrace($"RST {ToHex(address)}");
		PushUInt16(RegisterPC);
		RegisterPC = address;
		Clock += 16;
	}

	private void SetTo(Register8 destination, byte source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		SetRegister(destination, source);
		Clock += 8;
	}

	private void SetTo(Register16 destination, UInt16 source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		SetRegister(destination, source);
		Clock += 12;
	}

	private void SetTo(Address destination, byte source)
	{
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
		memory.WriteUInt8(destination.Value, source);
		Clock += 12;
	}

	private void SetTo(Address destination, Register8 source)
	{
		logger.LogTrace($"LD {destination}, {source}");
		memory.WriteUInt8(destination.Value, GetRegister(source));
		Clock += 8;
	}

	private void SetTo(Register8 destination, Address source)
	{
		logger.LogTrace($"LD {destination}, {source}");
		SetRegister(destination, memory.ReadUInt8(source.Value));
		Clock += 8;
	}

	private void SetTo(Register8 destination, Register8 source)
	{
		logger.LogTrace($"LD {destination}, {source}");
		SetRegister(destination, GetRegister(source));
		Clock += 4;
	}

	private void Increment(Register8 destinationAndSource)
	{
		logger.LogTrace($"INC {destinationAndSource}");
		var before = GetRegister(destinationAndSource);
		var after = (byte)(before + 1);
		SetRegister(destinationAndSource, after);
		Clock += 4;
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
		Clock += 8;
	}

	private void Increment(Address destinationAndSource)
	{
		logger.LogTrace($"INC {destinationAndSource}");
		var before = memory.ReadUInt8(destinationAndSource.Value);
		var after = (byte)(before + 1);
		memory.WriteUInt8(destinationAndSource.Value, after);
		Clock += 12;
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
		Clock += 4;
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
		Clock += 8;
	}

	private void Decrement(Address destinationAndSource)
	{
		logger.LogTrace($"DEC {destinationAndSource}");
		var before = memory.ReadUInt8(destinationAndSource.Value);
		var after = (byte)(before - 1);
		memory.WriteUInt8(destinationAndSource.Value, after);
		Clock += 12;
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (after & 0b1111_0000) == (before & 0b1111_0000);
	}

	private void Add(Register8 destination, Register8 source)
	{
		logger.LogTrace($"ADD {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after16 = (UInt16)((UInt16)before + (UInt16)sourceValue);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (sourceValue & 0b0000_1111) > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 4;
	}

	private void Add(Register8 destination, Register8 source, bool sourceCarry)
	{
		logger.LogTrace($"ADC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after16 = (UInt16)((UInt16)before + (UInt16)sourceValue + sourceCarryValue);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (sourceValue & 0b0000_1111) + sourceCarryValue > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 4;
	}

	private void Add(Register8 destination, Address source)
	{
		logger.LogTrace($"ADD {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after16 = (UInt16)((UInt16)before + (UInt16)sourceValue);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (sourceValue & 0b0000_1111) > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 8;
	}

	private void Add(Register8 destination, Address source, bool sourceCarry)
	{
		logger.LogTrace($"ADC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after16 = (UInt16)((UInt16)before + (UInt16)sourceValue + sourceCarryValue);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (sourceValue & 0b0000_1111) + sourceCarryValue > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 8;
	}

	private void Add(Register8 destination, byte source)
	{
		logger.LogTrace($"ADD {destination}, {source}");
		var before = GetRegister(destination);
		var after16 = (UInt16)((UInt16)before + (UInt16)source);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (source & 0b0000_1111) > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 8;
	}

	private void Add(Register8 destination, byte source, bool sourceCarry)
	{
		logger.LogTrace($"ADC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after16 = (UInt16)((UInt16)before + (UInt16)source + sourceCarryValue);
		var after8 = (byte)after16;
		SetRegister(destination, after8);
		ZeroFlag = after8 == 0;
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111) + (source & 0b0000_1111) + sourceCarryValue > 0b0000_1111;
		CarryFlag = after16 > 0b1111_1111;
		Clock += 8;
	}

	private void Add(Register16 destination, Register16 source)
	{
		logger.LogTrace($"ADD {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after32 = (UInt32)before + (UInt32)sourceValue;
		var after16 = (UInt16)after32;
		SetRegister(destination, after16);
		SubtractFlag = false;
		HalfCarryFlag = (before & 0b0000_1111_1111_1111) + (sourceValue & 0b0000_1111_1111_1111) > 0b0000_1111_1111_1111;
		CarryFlag = after32 > 0b1111_1111_1111_1111;
		Clock += 8;
	}

	private void Subtract(Register8 destination, Register8 source)
	{
		logger.LogTrace($"SUB {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (byte)(before - sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (sourceValue & 0b0000_1111);
		CarryFlag = before < sourceValue;
		Clock += 4;
	}

	private void Subtract(Register8 destination, Register8 source, bool sourceCarry)
	{
		logger.LogTrace($"SBC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after = (byte)(before - sourceValue - sourceCarryValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < ((sourceValue & 0b0000_1111) + sourceCarryValue);
		CarryFlag = before < (sourceValue + sourceCarryValue);
		Clock += 4;
	}

	private void Subtract(Register8 destination, Address source)
	{
		logger.LogTrace($"SUB {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after = (byte)(before - sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (sourceValue & 0b0000_1111);
		CarryFlag = before < sourceValue;
		Clock += 8;
	}

	private void Subtract(Register8 destination, Address source, bool sourceCarry)
	{
		logger.LogTrace($"SBC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after = (byte)(before - sourceValue - sourceCarryValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < ((sourceValue & 0b0000_1111) + sourceCarryValue);
		CarryFlag = before < (sourceValue + sourceCarryValue);
		Clock += 8;
	}

	private void Subtract(Register8 destination, byte source)
	{
		logger.LogTrace($"SUB {destination}, {source}");
		var before = GetRegister(destination);
		var after = (byte)(before - source);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (source & 0b0000_1111);
		CarryFlag = before < source;
		Clock += 8;
	}

	private void Subtract(Register8 destination, byte source, bool sourceCarry)
	{
		logger.LogTrace($"SBC {destination}, {source}");
		var before = GetRegister(destination);
		var sourceCarryValue = sourceCarry ? 1 : 0;
		var after = (byte)(before - source - sourceCarryValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < ((source & 0b0000_1111) + sourceCarryValue);
		CarryFlag = before < (source + sourceCarryValue);
		Clock += 8;
	}

	private void Compare(Register8 destination, Register8 source)
	{
		logger.LogTrace($"CP {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (byte)(before - sourceValue);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (sourceValue & 0b0000_1111);
		CarryFlag = before < sourceValue;
		Clock += 4;
	}

	private void Compare(Register8 destination, Address source)
	{
		logger.LogTrace($"CP {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after = (byte)(before - sourceValue);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (sourceValue & 0b0000_1111);
		CarryFlag = before < sourceValue;
		Clock += 8;
	}

	private void Compare(Register8 destination, byte source)
	{
		logger.LogTrace($"CP {destination}, {source}");
		var before = GetRegister(destination);
		var after = (byte)(before - source);
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (before & 0b0000_1111) < (source & 0b0000_1111);
		CarryFlag = before < source;
		Clock += 8;
	}

	private void And(Register8 destination, Register8 source)
	{
		logger.LogTrace($"AND {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (byte)(before & sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = true;
		CarryFlag = false;
		Clock += 4;
	}

	private void And(Register8 destination, Address source)
	{
		logger.LogTrace($"AND {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after = (byte)(before & sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = true;
		CarryFlag = false;
		Clock += 8;
	}

	private void And(Register8 destination, byte source)
	{
		logger.LogTrace($"AND {destination}, {source}");
		var before = GetRegister(destination);
		var after = (byte)(before & source);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = true;
		CarryFlag = false;
		Clock += 8;
	}

	private void Xor(Register8 destination, Register8 source)
	{
		logger.LogTrace($"XOR {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (byte)(before ^ sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 4;
	}

	private void Xor(Register8 destination, Address source)
	{
		logger.LogTrace($"XOR {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after = (byte)(before ^ sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
	}

	private void Xor(Register8 destination, byte source)
	{
		logger.LogTrace($"XOR {destination}, {source}");
		var before = GetRegister(destination);
		var after = (byte)(before ^ source);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
	}

	private void Or(Register8 destination, Register8 source)
	{
		logger.LogTrace($"OR {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after = (byte)(before | sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 4;
	}

	private void Or(Register8 destination, Address source)
	{
		logger.LogTrace($"OR {destination}, {source}");
		var before = GetRegister(destination);
		var sourceValue = memory.ReadUInt8(source.Value);
		var after = (byte)(before | sourceValue);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
	}

	private void Or(Register8 destination, byte source)
	{
		logger.LogTrace($"OR {destination}, {source}");
		var before = GetRegister(destination);
		var after = (byte)(before | source);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
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

	private void Pop(Register16 destination)
	{
		logger.LogTrace($"POP {destination}");
		SetRegister(destination, PopUInt16());
		clock += 12;
	}

	private void Push(Register16 source)
	{
		logger.LogTrace($"PUSH {source}");
		PushUInt16(GetRegister(source));
		clock += 16;
	}

	private byte ReadNextPCUInt8()
	{
		var result = memory.ReadUInt8(RegisterPC);
		RegisterPC++;
		return result;
	}

	private sbyte ReadNextPCInt8()
	{
		return (sbyte)ReadNextPCUInt8();
	}

	private UInt16 ReadNextPCUInt16()
	{
		var result = memory.ReadUInt16(RegisterPC);
		RegisterPC += 2;
		return result;
	}

	private void PushUInt16(UInt16 value)
	{
		RegisterSP -= 2;
		memory.WriteUInt16(RegisterSP, value);
	}

	private UInt16 PopUInt16()
	{
		var result = memory.ReadUInt16(RegisterSP);
		RegisterSP += 2;
		return result;
	}
}