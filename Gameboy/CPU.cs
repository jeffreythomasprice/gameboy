namespace Gameboy;

using Microsoft.Extensions.Logging;
using static NumberUtils;

public class CPU : ISteppable
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

	// interrupts get enabled or disabled after the instruction that follows is executed
	// so keep track of a desired clock value, as long as the current clock is at least that value we execute the delta
	private record struct InterruptEnableDelta(
		bool Value,
		UInt64 Clock
	)
	{ }

	/// <summary>
	/// How many clock ticks the CPU expects to go through per second of real time.
	/// </summary>
	public const UInt64 ClockTicksPerSecond = 4194304;

	public const UInt16 InitialPC = 0x0100;

	private const byte ZeroFlagMask = 0b1000_0000;
	private const byte SubtractFlagMask = 0b0100_0000;
	private const byte HalfCarryFlagMask = 0b0010_0000;
	private const byte CarryFlagMask = 0b0001_0000;
	private const byte UsableBitsFlagMask = 0b1111_0000;

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
	private bool ime;
	private readonly Queue<InterruptEnableDelta> interruptEnableDeltas = new();

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
		internal set => registerF = (byte)(value & UsableBitsFlagMask);
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
		get => (UInt16)((((UInt16)RegisterA) << 8) | (UInt16)RegisterF);
		internal set
		{
			RegisterA = (byte)((value & 0xff00) >> 8);
			RegisterF = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterBC
	{
		get => (UInt16)((((UInt16)RegisterB) << 8) | (UInt16)RegisterC);
		internal set
		{
			RegisterB = (byte)((value & 0xff00) >> 8);
			RegisterC = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterDE
	{
		get => (UInt16)((((UInt16)RegisterD) << 8) | (UInt16)RegisterE);
		internal set
		{
			RegisterD = (byte)((value & 0xff00) >> 8);
			RegisterE = (byte)(value & 0xff);
		}
	}

	public UInt16 RegisterHL
	{
		get => (UInt16)((((UInt16)RegisterH) << 8) | (UInt16)RegisterL);
		internal set
		{
			RegisterH = (byte)((value & 0xff00) >> 8);
			RegisterL = (byte)(value & 0xff);
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

	/// <summary>
	/// Interrupt Master Enable
	/// </summary>
	public bool IME
	{
		get => ime;
		internal set => ime = value;
	}

	public void Reset()
	{
		// A would be 0x11 for color
		registerA = 0x01;
		registerB = 0x00;
		registerC = 0x13;
		registerD = 0x00;
		registerE = 0xd8;
		registerF = 0xb0;
		registerH = 0x01;
		registerL = 0x4d;
		registerSP = 0xfffe;
		registerPC = InitialPC;
		clock = 0;
		isStopped = false;
		isHalted = false;
		ime = true;
		interruptEnableDeltas.Clear();
	}

	public void Step()
	{
		var registerPCBefore = RegisterPC;
		var shouldResetPC = false;

		// special case for STOP, resumes on any key press
		if (IsStopped && (memory.ReadUInt8(Memory.IO_IF) & Memory.IF_MASK_KEYPAD) != 0)
		{
#if DEBUG
			logger.LogTrace("resuming from state STOP, keyboard interrupt flag set");
#endif
			IsStopped = false;
		}

		// interrupts fire if the relevant flag in the IO register is set AND the master enable flag on the CPU is set
		if (IME || IsHalted)
		{
			var interruptEnableRegister = memory.ReadUInt8(Memory.IO_IE);
			var interruptFlag = memory.ReadUInt8(Memory.IO_IF);
			foreach (var (log, mask, address) in new[] {
				("v-blank", Memory.IF_MASK_VBLANK, (UInt16)0x0040),
				("LCDC", Memory.IF_MASK_LCDC, (UInt16)0x0048),
				("timer", Memory.IF_MASK_TIMER, (UInt16)0x0050),
				("serial", Memory.IF_MASK_SERIAL, (UInt16)0x0058),
				("keypad", Memory.IF_MASK_KEYPAD, (UInt16)0x0060),
			})
			{
				var enabled = (interruptEnableRegister & mask) != 0;
				var triggered = (interruptFlag & mask) != 0;
				if (enabled && triggered)
				{
					if (IsHalted)
					{
#if DEBUG
						logger.LogTrace($"resuming from HALT because of {log} interrupt");
#endif
						IsHalted = false;
						if (!IME)
						{
							shouldResetPC = true;
						}
					}
					else
					{
#if DEBUG
						logger.LogTrace($"{log} interrupt handled");
#endif
						memory.WriteUInt8(Memory.IO_IF, (byte)(interruptFlag & (~mask)));
						IME = false;
						PushUInt16(RegisterPC);
						RegisterPC = address;
					}
					break;
				}
			}
		}

		if (IsStopped)
		{
#if DEBUG
			logger.LogTrace("CPU is in state STOP");
#endif
			// stopped CPU has no clock running, only keypad will break out of this
			Clock += 4;
		}
		else if (IsHalted)
		{
#if DEBUG
			logger.LogTrace("CPU is in state HALT");
#endif
			// stop does advance time, because we might be jumped out by a timer interrupt
			Clock += 4;
		}
		else
		{
			ExecuteInstruction();
		}

		if (interruptEnableDeltas.TryPeek(out var next) && Clock > next.Clock)
		{
			IME = interruptEnableDeltas.Dequeue().Value;
#if DEBUG
			logger.LogTrace($"interrupts enabled = {IME}");
#endif
		}

		if (shouldResetPC)
		{
#if DEBUG
			logger.LogTrace($"PC stuck, resetting back to {ToHex(registerPCBefore)}");
#endif
			RegisterPC = registerPCBefore;
		}
	}

	private void ExecuteInstruction()
	{
		var instruction = ReadNextPCUInt8();
		switch (instruction)
		{
			case 0x00:
#if DEBUG
				logger.LogTrace("NOP");
#endif
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
#if DEBUG
					logger.LogTrace("RLCA");
#endif
					var before = RegisterA;
					var after = (byte)((before << 1) | ((before & 0b1000_0000) >> 7));
					RegisterA = after;
					ZeroFlag = false;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b1000_0000) != 0;
					Clock += 4;
				}
				break;
			case 0x0f:
				{
#if DEBUG
					logger.LogTrace("RRCA");
#endif
					var before = RegisterA;
					var after = (byte)((before >> 1) | ((before & 0b0000_0001) << 7));
					RegisterA = after;
					ZeroFlag = false;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b0000_0001) != 0;
					Clock += 4;
				}
				break;
			case 0x17:
				{
#if DEBUG
					logger.LogTrace("RLA");
#endif
					var before = RegisterA;
					var after = (byte)((before << 1) | (CarryFlag ? 0b0000_0001 : 0));
					RegisterA = after;
					ZeroFlag = false; ;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b1000_0000) != 0;
					Clock += 4;
				}
				break;
			case 0x1f:
				{
#if DEBUG
					logger.LogTrace("RRA");
#endif
					var before = RegisterA;
					var after = (byte)((before >> 1) | (CarryFlag ? 0b1000_0000 : 0));
					RegisterA = after;
					ZeroFlag = false;
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (before & 0b0000_0001) != 0;
					Clock += 4;
				}
				break;

			case 0x08:
				{
					var address = ReadNextPCUInt16();
#if DEBUG
					logger.LogTrace($"LD ({ToHex(address)}), {Register16.SP}");
#endif
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
#if DEBUG
					logger.LogTrace("STOP");
#endif
					// convention is that the next byte is 0x00, but unchecked
					ReadNextPCUInt8();
					IsStopped = true;
					Clock += 4;
				}
				break;

			case 0x18:
				{
					var delta = ReadNextPCInt8();
#if DEBUG
					logger.LogTrace($"JR {delta}");
#endif
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
#if DEBUG
					logger.LogTrace("DAA");
#endif
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
#if DEBUG
					logger.LogTrace("CPL");
#endif
					RegisterA = (byte)(~RegisterA);
					SubtractFlag = true;
					HalfCarryFlag = true;
					Clock += 4;
				}
				break;

			case 0x37:
				{
#if DEBUG
					logger.LogTrace("SCF");
#endif
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = true;
					Clock += 4;
				}
				break;
			case 0x3f:
				{
#if DEBUG
					logger.LogTrace("CCF");
#endif
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
#if DEBUG
					logger.LogTrace("HALT");
#endif
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
					var offset = ReadNextPCUInt8();
#if DEBUG
					logger.LogTrace($"LDH ({ToHex(0xff00)}+{ToHex(offset)}), A");
#endif
					memory.WriteUInt8((UInt16)(0xff00 + offset), RegisterA);
					Clock += 12;
				}
				break;
			case 0xf0:
				{
					var offset = ReadNextPCUInt8();
#if DEBUG
					logger.LogTrace($"LDH A, ({ToHex(0xff00)}+{ToHex(offset)})");
#endif
					RegisterA = memory.ReadUInt8((UInt16)(0xff00 + offset));
					Clock += 12;
				}
				break;

			case 0xe2:
				{
#if DEBUG
					logger.LogTrace($"LD ({ToHex(0xff00)}+c), A");
#endif
					memory.WriteUInt8((UInt16)(0xff00 + RegisterC), RegisterA);
					Clock += 8;
				}
				break;
			case 0xf2:
				{
#if DEBUG
					logger.LogTrace($"LD A, ({ToHex(0xff00)}+c)");
#endif
					RegisterA = memory.ReadUInt8((UInt16)(0xff00 + RegisterC));
					Clock += 8;
				}
				break;

			case 0xe8:
				{
					var data = ReadNextPCInt8();
					Add(Register16.SP, data);
				}
				break;

			case 0xe9:
				{
					Jump(Register16.HL);
				}
				break;

			case 0xea:
				{
					var address = ReadNextPCUInt16();
#if DEBUG
					logger.LogTrace($"LD ({ToHex(address)}), A");
#endif
					memory.WriteUInt8(address, RegisterA);
					Clock += 16;
				}
				break;
			case 0xfa:
				{
					var address = ReadNextPCUInt16();
#if DEBUG
					logger.LogTrace($"LD A, ({ToHex(address)})");
#endif
					RegisterA = memory.ReadUInt8(address);
					Clock += 16;
				}
				break;

			case 0xf3:
				{
					EnqueueInterruptsEnabled(false);
				}
				break;
			case 0xfb:
				{
					EnqueueInterruptsEnabled(true);
				}
				break;

			case 0xf8:
				{
					var offset = ReadNextPCInt8();
#if DEBUG
					logger.LogTrace($"LD HL, SP+{offset}");
#endif
					var before = RegisterSP;
					var after = (UInt16)(before + offset);
					RegisterHL = after;
					ZeroFlag = false;
					SubtractFlag = false;
					// this kind of add involves the carry bits determined from the low byte of the result
					HalfCarryFlag = (before & 0b0000_0000_0000_1111) + (((byte)offset) & 0b0000_1111) > 0b0000_0000_0000_1111;
					CarryFlag = (before & 0b0000_0000_1111_1111) + (((byte)offset) & 0b1111_1111) > 0b0000_0000_1111_1111;
					Clock += 12;
				}
				break;

			case 0xf9:
				{
					RegisterSP = RegisterHL;
					Clock += 8;
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
					InvalidInstruction(instruction);
				}
				break;

			default:
				throw new NotImplementedException($"unhandled instruction {ToHex(instruction)}");
		}
	}

	private void ExecutePrefixInstruction()
	{
		var instruction = ReadNextPCUInt8();
		switch (instruction)
		{
			case 0x00:
				{
					RotateLeftDontIncludeCarry(Register8.B);
				}
				break;
			case 0x01:
				{
					RotateLeftDontIncludeCarry(Register8.C);
				}
				break;
			case 0x02:
				{
					RotateLeftDontIncludeCarry(Register8.D);
				}
				break;
			case 0x03:
				{
					RotateLeftDontIncludeCarry(Register8.E);
				}
				break;
			case 0x04:
				{
					RotateLeftDontIncludeCarry(Register8.H);
				}
				break;
			case 0x05:
				{
					RotateLeftDontIncludeCarry(Register8.L);
				}
				break;
			case 0x06:
				{
					RotateLeftDontIncludeCarry(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x07:
				{
					RotateLeftDontIncludeCarry(Register8.A);
				}
				break;

			case 0x08:
				{
					RotateRightDontIncludeCarry(Register8.B);
				}
				break;
			case 0x09:
				{
					RotateRightDontIncludeCarry(Register8.C);
				}
				break;
			case 0x0a:
				{
					RotateRightDontIncludeCarry(Register8.D);
				}
				break;
			case 0x0b:
				{
					RotateRightDontIncludeCarry(Register8.E);
				}
				break;
			case 0x0c:
				{
					RotateRightDontIncludeCarry(Register8.H);
				}
				break;
			case 0x0d:
				{
					RotateRightDontIncludeCarry(Register8.L);
				}
				break;
			case 0x0e:
				{
					RotateRightDontIncludeCarry(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x0f:
				{
					RotateRightDontIncludeCarry(Register8.A);
				}
				break;

			case 0x10:
				{
					RotateLeftThroughCarry(Register8.B);
				}
				break;
			case 0x11:
				{
					RotateLeftThroughCarry(Register8.C);
				}
				break;
			case 0x12:
				{
					RotateLeftThroughCarry(Register8.D);
				}
				break;
			case 0x13:
				{
					RotateLeftThroughCarry(Register8.E);
				}
				break;
			case 0x14:
				{
					RotateLeftThroughCarry(Register8.H);
				}
				break;
			case 0x15:
				{
					RotateLeftThroughCarry(Register8.L);
				}
				break;
			case 0x16:
				{
					RotateLeftThroughCarry(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x17:
				{
					RotateLeftThroughCarry(Register8.A);
				}
				break;

			case 0x18:
				{
					RotateRightThroughCarry(Register8.B);
				}
				break;
			case 0x19:
				{
					RotateRightThroughCarry(Register8.C);
				}
				break;
			case 0x1a:
				{
					RotateRightThroughCarry(Register8.D);
				}
				break;
			case 0x1b:
				{
					RotateRightThroughCarry(Register8.E);
				}
				break;
			case 0x1c:
				{
					RotateRightThroughCarry(Register8.H);
				}
				break;
			case 0x1d:
				{
					RotateRightThroughCarry(Register8.L);
				}
				break;
			case 0x1e:
				{
					RotateRightThroughCarry(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x1f:
				{
					RotateRightThroughCarry(Register8.A);
				}
				break;

			case 0x20:
				{
					ShiftLeftIntoCarryResetLsb(Register8.B);
				}
				break;
			case 0x21:
				{
					ShiftLeftIntoCarryResetLsb(Register8.C);
				}
				break;
			case 0x22:
				{
					ShiftLeftIntoCarryResetLsb(Register8.D);
				}
				break;
			case 0x23:
				{
					ShiftLeftIntoCarryResetLsb(Register8.E);
				}
				break;
			case 0x24:
				{
					ShiftLeftIntoCarryResetLsb(Register8.H);
				}
				break;
			case 0x25:
				{
					ShiftLeftIntoCarryResetLsb(Register8.L);
				}
				break;
			case 0x26:
				{
					ShiftLeftIntoCarryResetLsb(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x27:
				{
					ShiftLeftIntoCarryResetLsb(Register8.A);
				}
				break;

			case 0x28:
				{
					ShiftRightIntoCarryKeepMsb(Register8.B);
				}
				break;
			case 0x29:
				{
					ShiftRightIntoCarryKeepMsb(Register8.C);
				}
				break;
			case 0x2a:
				{
					ShiftRightIntoCarryKeepMsb(Register8.D);
				}
				break;
			case 0x2b:
				{
					ShiftRightIntoCarryKeepMsb(Register8.E);
				}
				break;
			case 0x2c:
				{
					ShiftRightIntoCarryKeepMsb(Register8.H);
				}
				break;
			case 0x2d:
				{
					ShiftRightIntoCarryKeepMsb(Register8.L);
				}
				break;
			case 0x2e:
				{
					ShiftRightIntoCarryKeepMsb(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x2f:
				{
					ShiftRightIntoCarryKeepMsb(Register8.A);
				}
				break;

			case 0x30:
				{
					SwapUpperAndLowerNibbles(Register8.B);
				}
				break;
			case 0x31:
				{
					SwapUpperAndLowerNibbles(Register8.C);
				}
				break;
			case 0x32:
				{
					SwapUpperAndLowerNibbles(Register8.D);
				}
				break;
			case 0x33:
				{
					SwapUpperAndLowerNibbles(Register8.E);
				}
				break;
			case 0x34:
				{
					SwapUpperAndLowerNibbles(Register8.H);
				}
				break;
			case 0x35:
				{
					SwapUpperAndLowerNibbles(Register8.L);
				}
				break;
			case 0x36:
				{
					SwapUpperAndLowerNibbles(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x37:
				{
					SwapUpperAndLowerNibbles(Register8.A);
				}
				break;

			case 0x38:
				{
					ShiftRightIntoCarryResetMsb(Register8.B);
				}
				break;
			case 0x39:
				{
					ShiftRightIntoCarryResetMsb(Register8.C);
				}
				break;
			case 0x3a:
				{
					ShiftRightIntoCarryResetMsb(Register8.D);
				}
				break;
			case 0x3b:
				{
					ShiftRightIntoCarryResetMsb(Register8.E);
				}
				break;
			case 0x3c:
				{
					ShiftRightIntoCarryResetMsb(Register8.H);
				}
				break;
			case 0x3d:
				{
					ShiftRightIntoCarryResetMsb(Register8.L);
				}
				break;
			case 0x3e:
				{
					ShiftRightIntoCarryResetMsb(new Address(RegisterHL, Register16.HL.ToString()));
				}
				break;
			case 0x3f:
				{
					ShiftRightIntoCarryResetMsb(Register8.A);
				}
				break;

			case 0x40:
				{
					GetBit(Register8.B, 0);
				}
				break;
			case 0x41:
				{
					GetBit(Register8.C, 0);
				}
				break;
			case 0x42:
				{
					GetBit(Register8.D, 0);
				}
				break;
			case 0x43:
				{
					GetBit(Register8.E, 0);
				}
				break;
			case 0x44:
				{
					GetBit(Register8.H, 0);
				}
				break;
			case 0x45:
				{
					GetBit(Register8.L, 0);
				}
				break;
			case 0x46:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 0);
				}
				break;
			case 0x47:
				{
					GetBit(Register8.A, 0);
				}
				break;
			case 0x48:
				{
					GetBit(Register8.B, 1);
				}
				break;
			case 0x49:
				{
					GetBit(Register8.C, 1);
				}
				break;
			case 0x4a:
				{
					GetBit(Register8.D, 1);
				}
				break;
			case 0x4b:
				{
					GetBit(Register8.E, 1);
				}
				break;
			case 0x4c:
				{
					GetBit(Register8.H, 1);
				}
				break;
			case 0x4d:
				{
					GetBit(Register8.L, 1);
				}
				break;
			case 0x4e:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 1);
				}
				break;
			case 0x4f:
				{
					GetBit(Register8.A, 1);
				}
				break;
			case 0x50:
				{
					GetBit(Register8.B, 2);
				}
				break;
			case 0x51:
				{
					GetBit(Register8.C, 2);
				}
				break;
			case 0x52:
				{
					GetBit(Register8.D, 2);
				}
				break;
			case 0x53:
				{
					GetBit(Register8.E, 2);
				}
				break;
			case 0x54:
				{
					GetBit(Register8.H, 2);
				}
				break;
			case 0x55:
				{
					GetBit(Register8.L, 2);
				}
				break;
			case 0x56:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 2);
				}
				break;
			case 0x57:
				{
					GetBit(Register8.A, 2);
				}
				break;
			case 0x58:
				{
					GetBit(Register8.B, 3);
				}
				break;
			case 0x59:
				{
					GetBit(Register8.C, 3);
				}
				break;
			case 0x5a:
				{
					GetBit(Register8.D, 3);
				}
				break;
			case 0x5b:
				{
					GetBit(Register8.E, 3);
				}
				break;
			case 0x5c:
				{
					GetBit(Register8.H, 3);
				}
				break;
			case 0x5d:
				{
					GetBit(Register8.L, 3);
				}
				break;
			case 0x5e:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 3);
				}
				break;
			case 0x5f:
				{
					GetBit(Register8.A, 3);
				}
				break;
			case 0x60:
				{
					GetBit(Register8.B, 4);
				}
				break;
			case 0x61:
				{
					GetBit(Register8.C, 4);
				}
				break;
			case 0x62:
				{
					GetBit(Register8.D, 4);
				}
				break;
			case 0x63:
				{
					GetBit(Register8.E, 4);
				}
				break;
			case 0x64:
				{
					GetBit(Register8.H, 4);
				}
				break;
			case 0x65:
				{
					GetBit(Register8.L, 4);
				}
				break;
			case 0x66:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 4);
				}
				break;
			case 0x67:
				{
					GetBit(Register8.A, 4);
				}
				break;
			case 0x68:
				{
					GetBit(Register8.B, 5);
				}
				break;
			case 0x69:
				{
					GetBit(Register8.C, 5);
				}
				break;
			case 0x6a:
				{
					GetBit(Register8.D, 5);
				}
				break;
			case 0x6b:
				{
					GetBit(Register8.E, 5);
				}
				break;
			case 0x6c:
				{
					GetBit(Register8.H, 5);
				}
				break;
			case 0x6d:
				{
					GetBit(Register8.L, 5);
				}
				break;
			case 0x6e:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 5);
				}
				break;
			case 0x6f:
				{
					GetBit(Register8.A, 5);
				}
				break;
			case 0x70:
				{
					GetBit(Register8.B, 6);
				}
				break;
			case 0x71:
				{
					GetBit(Register8.C, 6);
				}
				break;
			case 0x72:
				{
					GetBit(Register8.D, 6);
				}
				break;
			case 0x73:
				{
					GetBit(Register8.E, 6);
				}
				break;
			case 0x74:
				{
					GetBit(Register8.H, 6);
				}
				break;
			case 0x75:
				{
					GetBit(Register8.L, 6);
				}
				break;
			case 0x76:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 6);
				}
				break;
			case 0x77:
				{
					GetBit(Register8.A, 6);
				}
				break;
			case 0x78:
				{
					GetBit(Register8.B, 7);
				}
				break;
			case 0x79:
				{
					GetBit(Register8.C, 7);
				}
				break;
			case 0x7a:
				{
					GetBit(Register8.D, 7);
				}
				break;
			case 0x7b:
				{
					GetBit(Register8.E, 7);
				}
				break;
			case 0x7c:
				{
					GetBit(Register8.H, 7);
				}
				break;
			case 0x7d:
				{
					GetBit(Register8.L, 7);
				}
				break;
			case 0x7e:
				{
					GetBit(new Address(RegisterHL, Register16.HL.ToString()), 7);
				}
				break;
			case 0x7f:
				{
					GetBit(Register8.A, 7);
				}
				break;

			case 0x80:
				{
					ResetBit(Register8.B, 0);
				}
				break;
			case 0x81:
				{
					ResetBit(Register8.C, 0);
				}
				break;
			case 0x82:
				{
					ResetBit(Register8.D, 0);
				}
				break;
			case 0x83:
				{
					ResetBit(Register8.E, 0);
				}
				break;
			case 0x84:
				{
					ResetBit(Register8.H, 0);
				}
				break;
			case 0x85:
				{
					ResetBit(Register8.L, 0);
				}
				break;
			case 0x86:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 0);
				}
				break;
			case 0x87:
				{
					ResetBit(Register8.A, 0);
				}
				break;
			case 0x88:
				{
					ResetBit(Register8.B, 1);
				}
				break;
			case 0x89:
				{
					ResetBit(Register8.C, 1);
				}
				break;
			case 0x8a:
				{
					ResetBit(Register8.D, 1);
				}
				break;
			case 0x8b:
				{
					ResetBit(Register8.E, 1);
				}
				break;
			case 0x8c:
				{
					ResetBit(Register8.H, 1);
				}
				break;
			case 0x8d:
				{
					ResetBit(Register8.L, 1);
				}
				break;
			case 0x8e:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 1);
				}
				break;
			case 0x8f:
				{
					ResetBit(Register8.A, 1);
				}
				break;
			case 0x90:
				{
					ResetBit(Register8.B, 2);
				}
				break;
			case 0x91:
				{
					ResetBit(Register8.C, 2);
				}
				break;
			case 0x92:
				{
					ResetBit(Register8.D, 2);
				}
				break;
			case 0x93:
				{
					ResetBit(Register8.E, 2);
				}
				break;
			case 0x94:
				{
					ResetBit(Register8.H, 2);
				}
				break;
			case 0x95:
				{
					ResetBit(Register8.L, 2);
				}
				break;
			case 0x96:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 2);
				}
				break;
			case 0x97:
				{
					ResetBit(Register8.A, 2);
				}
				break;
			case 0x98:
				{
					ResetBit(Register8.B, 3);
				}
				break;
			case 0x99:
				{
					ResetBit(Register8.C, 3);
				}
				break;
			case 0x9a:
				{
					ResetBit(Register8.D, 3);
				}
				break;
			case 0x9b:
				{
					ResetBit(Register8.E, 3);
				}
				break;
			case 0x9c:
				{
					ResetBit(Register8.H, 3);
				}
				break;
			case 0x9d:
				{
					ResetBit(Register8.L, 3);
				}
				break;
			case 0x9e:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 3);
				}
				break;
			case 0x9f:
				{
					ResetBit(Register8.A, 3);
				}
				break;
			case 0xa0:
				{
					ResetBit(Register8.B, 4);
				}
				break;
			case 0xa1:
				{
					ResetBit(Register8.C, 4);
				}
				break;
			case 0xa2:
				{
					ResetBit(Register8.D, 4);
				}
				break;
			case 0xa3:
				{
					ResetBit(Register8.E, 4);
				}
				break;
			case 0xa4:
				{
					ResetBit(Register8.H, 4);
				}
				break;
			case 0xa5:
				{
					ResetBit(Register8.L, 4);
				}
				break;
			case 0xa6:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 4);
				}
				break;
			case 0xa7:
				{
					ResetBit(Register8.A, 4);
				}
				break;
			case 0xa8:
				{
					ResetBit(Register8.B, 5);
				}
				break;
			case 0xa9:
				{
					ResetBit(Register8.C, 5);
				}
				break;
			case 0xaa:
				{
					ResetBit(Register8.D, 5);
				}
				break;
			case 0xab:
				{
					ResetBit(Register8.E, 5);
				}
				break;
			case 0xac:
				{
					ResetBit(Register8.H, 5);
				}
				break;
			case 0xad:
				{
					ResetBit(Register8.L, 5);
				}
				break;
			case 0xae:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 5);
				}
				break;
			case 0xaf:
				{
					ResetBit(Register8.A, 5);
				}
				break;
			case 0xb0:
				{
					ResetBit(Register8.B, 6);
				}
				break;
			case 0xb1:
				{
					ResetBit(Register8.C, 6);
				}
				break;
			case 0xb2:
				{
					ResetBit(Register8.D, 6);
				}
				break;
			case 0xb3:
				{
					ResetBit(Register8.E, 6);
				}
				break;
			case 0xb4:
				{
					ResetBit(Register8.H, 6);
				}
				break;
			case 0xb5:
				{
					ResetBit(Register8.L, 6);
				}
				break;
			case 0xb6:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 6);
				}
				break;
			case 0xb7:
				{
					ResetBit(Register8.A, 6);
				}
				break;
			case 0xb8:
				{
					ResetBit(Register8.B, 7);
				}
				break;
			case 0xb9:
				{
					ResetBit(Register8.C, 7);
				}
				break;
			case 0xba:
				{
					ResetBit(Register8.D, 7);
				}
				break;
			case 0xbb:
				{
					ResetBit(Register8.E, 7);
				}
				break;
			case 0xbc:
				{
					ResetBit(Register8.H, 7);
				}
				break;
			case 0xbd:
				{
					ResetBit(Register8.L, 7);
				}
				break;
			case 0xbe:
				{
					ResetBit(new Address(RegisterHL, Register16.HL.ToString()), 7);
				}
				break;
			case 0xbf:
				{
					ResetBit(Register8.A, 7);
				}
				break;

			case 0xc0:
				{
					SetBit(Register8.B, 0);
				}
				break;
			case 0xc1:
				{
					SetBit(Register8.C, 0);
				}
				break;
			case 0xc2:
				{
					SetBit(Register8.D, 0);
				}
				break;
			case 0xc3:
				{
					SetBit(Register8.E, 0);
				}
				break;
			case 0xc4:
				{
					SetBit(Register8.H, 0);
				}
				break;
			case 0xc5:
				{
					SetBit(Register8.L, 0);
				}
				break;
			case 0xc6:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 0);
				}
				break;
			case 0xc7:
				{
					SetBit(Register8.A, 0);
				}
				break;
			case 0xc8:
				{
					SetBit(Register8.B, 1);
				}
				break;
			case 0xc9:
				{
					SetBit(Register8.C, 1);
				}
				break;
			case 0xca:
				{
					SetBit(Register8.D, 1);
				}
				break;
			case 0xcb:
				{
					SetBit(Register8.E, 1);
				}
				break;
			case 0xcc:
				{
					SetBit(Register8.H, 1);
				}
				break;
			case 0xcd:
				{
					SetBit(Register8.L, 1);
				}
				break;
			case 0xce:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 1);
				}
				break;
			case 0xcf:
				{
					SetBit(Register8.A, 1);
				}
				break;
			case 0xd0:
				{
					SetBit(Register8.B, 2);
				}
				break;
			case 0xd1:
				{
					SetBit(Register8.C, 2);
				}
				break;
			case 0xd2:
				{
					SetBit(Register8.D, 2);
				}
				break;
			case 0xd3:
				{
					SetBit(Register8.E, 2);
				}
				break;
			case 0xd4:
				{
					SetBit(Register8.H, 2);
				}
				break;
			case 0xd5:
				{
					SetBit(Register8.L, 2);
				}
				break;
			case 0xd6:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 2);
				}
				break;
			case 0xd7:
				{
					SetBit(Register8.A, 2);
				}
				break;
			case 0xd8:
				{
					SetBit(Register8.B, 3);
				}
				break;
			case 0xd9:
				{
					SetBit(Register8.C, 3);
				}
				break;
			case 0xda:
				{
					SetBit(Register8.D, 3);
				}
				break;
			case 0xdb:
				{
					SetBit(Register8.E, 3);
				}
				break;
			case 0xdc:
				{
					SetBit(Register8.H, 3);
				}
				break;
			case 0xdd:
				{
					SetBit(Register8.L, 3);
				}
				break;
			case 0xde:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 3);
				}
				break;
			case 0xdf:
				{
					SetBit(Register8.A, 3);
				}
				break;
			case 0xe0:
				{
					SetBit(Register8.B, 4);
				}
				break;
			case 0xe1:
				{
					SetBit(Register8.C, 4);
				}
				break;
			case 0xe2:
				{
					SetBit(Register8.D, 4);
				}
				break;
			case 0xe3:
				{
					SetBit(Register8.E, 4);
				}
				break;
			case 0xe4:
				{
					SetBit(Register8.H, 4);
				}
				break;
			case 0xe5:
				{
					SetBit(Register8.L, 4);
				}
				break;
			case 0xe6:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 4);
				}
				break;
			case 0xe7:
				{
					SetBit(Register8.A, 4);
				}
				break;
			case 0xe8:
				{
					SetBit(Register8.B, 5);
				}
				break;
			case 0xe9:
				{
					SetBit(Register8.C, 5);
				}
				break;
			case 0xea:
				{
					SetBit(Register8.D, 5);
				}
				break;
			case 0xeb:
				{
					SetBit(Register8.E, 5);
				}
				break;
			case 0xec:
				{
					SetBit(Register8.H, 5);
				}
				break;
			case 0xed:
				{
					SetBit(Register8.L, 5);
				}
				break;
			case 0xee:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 5);
				}
				break;
			case 0xef:
				{
					SetBit(Register8.A, 5);
				}
				break;
			case 0xf0:
				{
					SetBit(Register8.B, 6);
				}
				break;
			case 0xf1:
				{
					SetBit(Register8.C, 6);
				}
				break;
			case 0xf2:
				{
					SetBit(Register8.D, 6);
				}
				break;
			case 0xf3:
				{
					SetBit(Register8.E, 6);
				}
				break;
			case 0xf4:
				{
					SetBit(Register8.H, 6);
				}
				break;
			case 0xf5:
				{
					SetBit(Register8.L, 6);
				}
				break;
			case 0xf6:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 6);
				}
				break;
			case 0xf7:
				{
					SetBit(Register8.A, 6);
				}
				break;
			case 0xf8:
				{
					SetBit(Register8.B, 7);
				}
				break;
			case 0xf9:
				{
					SetBit(Register8.C, 7);
				}
				break;
			case 0xfa:
				{
					SetBit(Register8.D, 7);
				}
				break;
			case 0xfb:
				{
					SetBit(Register8.E, 7);
				}
				break;
			case 0xfc:
				{
					SetBit(Register8.H, 7);
				}
				break;
			case 0xfd:
				{
					SetBit(Register8.L, 7);
				}
				break;
			case 0xfe:
				{
					SetBit(new Address(RegisterHL, Register16.HL.ToString()), 7);
				}
				break;
			case 0xff:
				{
					SetBit(Register8.A, 7);
				}
				break;

			default:
				throw new NotImplementedException($"unhandled prefix instruction {ToHex(instruction)}");
		}

	}

	private void ConditionalJumpInt8(bool condition, string conditionString, sbyte delta)
	{
#if DEBUG
		logger.LogTrace($"JR {conditionString}, {delta}");
#endif
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
#if DEBUG
		logger.LogTrace($"JP {conditionString}, {ToHex(address)}");
#endif
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
#if DEBUG
		logger.LogTrace($"JP {ToHex(address)}");
#endif
		RegisterPC = address;
		Clock += 16;
	}

	private void Jump(Register16 address)
	{
#if DEBUG
		logger.LogTrace($"JP {address}");
#endif
		RegisterPC = GetRegister(address);
		Clock += 4;
	}

	private void ConditionalReturn(bool condition, string conditionString)
	{
#if DEBUG
		logger.LogTrace($"RET {conditionString}");
#endif
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
#if DEBUG
		logger.LogTrace("RET");
#endif
		RegisterPC = PopUInt16();
		Clock += 16;
	}

	private void ReturnAndEnableInterrupts()
	{
#if DEBUG
		logger.LogTrace("RETI");
#endif
		RegisterPC = PopUInt16();
		ime = true;
		Clock += 16;
	}

	private void ConditionalCallUInt16(bool condition, string conditionString, UInt16 address)
	{
#if DEBUG
		logger.LogTrace($"CALL {conditionString}, {ToHex(address)}");
#endif
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
#if DEBUG
		logger.LogTrace($"CALL {ToHex(address)}");
#endif
		PushUInt16(RegisterPC);
		RegisterPC = address;
		Clock += 24;
	}

	private void RestartCall(byte address)
	{
#if DEBUG
		logger.LogTrace($"RST {ToHex(address)}");
#endif
		PushUInt16(RegisterPC);
		RegisterPC = address;
		Clock += 16;
	}

	private void SetTo(Register8 destination, byte source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
#endif
		SetRegister(destination, source);
		Clock += 8;
	}

	private void SetTo(Register16 destination, UInt16 source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
#endif
		SetRegister(destination, source);
		Clock += 12;
	}

	private void SetTo(Address destination, byte source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {ToHex(source)}");
#endif
		memory.WriteUInt8(destination.Value, source);
		Clock += 12;
	}

	private void SetTo(Address destination, Register8 source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {source}");
#endif
		memory.WriteUInt8(destination.Value, GetRegister(source));
		Clock += 8;
	}

	private void SetTo(Register8 destination, Address source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {source}");
#endif
		SetRegister(destination, memory.ReadUInt8(source.Value));
		Clock += 8;
	}

	private void SetTo(Register8 destination, Register8 source)
	{
#if DEBUG
		logger.LogTrace($"LD {destination}, {source}");
#endif
		SetRegister(destination, GetRegister(source));
		Clock += 4;
	}

	private void Increment(Register8 destinationAndSource)
	{
#if DEBUG
		logger.LogTrace($"INC {destinationAndSource}");
#endif
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
#if DEBUG
		logger.LogTrace($"INC {destinationAndSource}");
#endif
		var before = GetRegister(destinationAndSource);
		var after = (UInt16)(before + 1);
		SetRegister(destinationAndSource, after);
		Clock += 8;
	}

	private void Increment(Address destinationAndSource)
	{
#if DEBUG
		logger.LogTrace($"INC {destinationAndSource}");
#endif
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
#if DEBUG
		logger.LogTrace($"DEC {destinationAndSource}");
#endif
		var before = GetRegister(destinationAndSource);
		var after = (byte)(before - 1);
		SetRegister(destinationAndSource, after);
		Clock += 4;
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (after & 0b1111_0000) != (before & 0b1111_0000);
	}

	private void Decrement(Register16 destinationAndSource)
	{
#if DEBUG
		logger.LogTrace($"DEC {destinationAndSource}");
#endif
		var before = GetRegister(destinationAndSource);
		var after = (UInt16)(before - 1);
		SetRegister(destinationAndSource, after);
		Clock += 8;
	}

	private void Decrement(Address destinationAndSource)
	{
#if DEBUG
		logger.LogTrace($"DEC {destinationAndSource}");
#endif
		var before = memory.ReadUInt8(destinationAndSource.Value);
		var after = (byte)(before - 1);
		memory.WriteUInt8(destinationAndSource.Value, after);
		Clock += 12;
		ZeroFlag = after == 0;
		SubtractFlag = true;
		HalfCarryFlag = (after & 0b1111_0000) != (before & 0b1111_0000);
	}

	private void Add(Register8 destination, Register8 source)
	{
#if DEBUG
		logger.LogTrace($"ADD {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADD {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADD {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"ADD {destination}, {source}");
#endif
		var before = GetRegister(destination);
		var sourceValue = GetRegister(source);
		var after32 = (UInt32)before + (UInt32)sourceValue;
		var after16 = (UInt16)after32;
		SetRegister(destination, after16);
		SubtractFlag = false;
		// this kind of add involves carry bits determined from the high byte of the result
		HalfCarryFlag = (before & 0b0000_1111_1111_1111) + (sourceValue & 0b0000_1111_1111_1111) > 0b0000_1111_1111_1111;
		CarryFlag = after32 > 0b1111_1111_1111_1111;
		Clock += 8;
	}

	private void Add(Register16 destination, sbyte delta)
	{
#if DEBUG
		logger.LogTrace($"ADD {destination}, {delta}");
#endif
		var before = GetRegister(destination);
		var after = (UInt16)(before + delta);
		SetRegister(destination, after);
		ZeroFlag = false;
		SubtractFlag = false;
		// this kind of add involves the carry bits determined from the low byte of the result
		HalfCarryFlag = (before & 0b0000_0000_0000_1111) + (((byte)delta) & 0b0000_1111) > 0b0000_0000_0000_1111;
		CarryFlag = (before & 0b0000_0000_1111_1111) + (((byte)delta) & 0b1111_1111) > 0b0000_0000_1111_1111;
		Clock += 16;
	}

	private void Subtract(Register8 destination, Register8 source)
	{
#if DEBUG
		logger.LogTrace($"SUB {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"SBC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"SUB {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"SBC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"SUB {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"SBC {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"CP {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"CP {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"CP {destination}, {ToHex(source)}");
#endif
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
#if DEBUG
		logger.LogTrace($"AND {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"AND {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"AND {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"XOR {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"XOR {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"XOR {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"OR {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"OR {destination}, {source}");
#endif
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
#if DEBUG
		logger.LogTrace($"OR {destination}, {source}");
#endif
		var before = GetRegister(destination);
		var after = (byte)(before | source);
		SetRegister(destination, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
	}

	private void EnqueueInterruptsEnabled(bool value)
	{
#if DEBUG
		if (value)
		{
			logger.LogTrace("EI");
		}
		else
		{
			logger.LogTrace("DI");
		}
#endif
		// executes after the next instruction, so put the clock just after the end of this instruction, presumably in the middle of the next one
		// by the time the clock is at least that value then ext instruction must have completed
		interruptEnableDeltas.Enqueue(new(value, Clock + 5));
		Clock += 4;
	}

	private void RotateLeftDontIncludeCarry(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"RLC {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)((before << 1) | ((before & 0b1000_0000) >> 7));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 8;
	}

	private void RotateLeftDontIncludeCarry(Address address)
	{
#if DEBUG
		logger.LogTrace($"RLC {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)((before << 1) | ((before & 0b1000_0000) >> 7));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 16;
	}

	private void RotateLeftThroughCarry(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"RL {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)((before << 1) | (CarryFlag ? 0b0000_0001 : 0));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 8;
	}

	private void RotateLeftThroughCarry(Address address)
	{
#if DEBUG
		logger.LogTrace($"RL {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)((before << 1) | (CarryFlag ? 0b0000_0001 : 0));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 16;
	}

	private void RotateRightDontIncludeCarry(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"RRC {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)((before >> 1) | ((before & 0b0000_0001) << 7));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 8;
	}

	private void RotateRightDontIncludeCarry(Address address)
	{
#if DEBUG
		logger.LogTrace($"RRC {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)((before >> 1) | ((before & 0b0000_0001) << 7));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 16;
	}

	private void RotateRightThroughCarry(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"RR {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)((before >> 1) | (CarryFlag ? 0b1000_0000 : 0));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 8;
	}

	private void RotateRightThroughCarry(Address address)
	{
#if DEBUG
		logger.LogTrace($"RR {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)((before >> 1) | (CarryFlag ? 0b1000_0000 : 0));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 16;
	}

	private void ShiftLeftIntoCarryResetLsb(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"SLA {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)(before << 1);
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 8;
	}

	private void ShiftLeftIntoCarryResetLsb(Address address)
	{
#if DEBUG
		logger.LogTrace($"SLA {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)(before << 1);
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b1000_0000) != 0;
		Clock += 16;
	}

	private void ShiftRightIntoCarryResetMsb(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"SRL {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)(before >> 1);
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 8;
	}

	private void ShiftRightIntoCarryResetMsb(Address address)
	{
#if DEBUG
		logger.LogTrace($"SRL {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)(before >> 1);
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 16;
	}

	private void ShiftRightIntoCarryKeepMsb(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"SRA {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)((before >> 1) | (before & 0b1000_0000));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 8;
	}

	private void ShiftRightIntoCarryKeepMsb(Address address)
	{
#if DEBUG
		logger.LogTrace($"SRA {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)((before >> 1) | (before & 0b1000_0000));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (before & 0b0000_0001) != 0;
		Clock += 16;
	}

	private void Pop(Register16 destination)
	{
#if DEBUG
		logger.LogTrace($"POP {destination}");
#endif
		SetRegister(destination, PopUInt16());
		clock += 12;
	}

	private void Push(Register16 source)
	{
#if DEBUG
		logger.LogTrace($"PUSH {source}");
#endif
		PushUInt16(GetRegister(source));
		clock += 16;
	}

	private void SwapUpperAndLowerNibbles(Register8 register)
	{
#if DEBUG
		logger.LogTrace($"SWAP {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)(((before & 0b1111_0000) >> 4) | ((before & 0b0000_1111) << 4));
		SetRegister(register, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 8;
	}

	private void SwapUpperAndLowerNibbles(Address address)
	{
#if DEBUG
		logger.LogTrace($"SWAP {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)(((before & 0b1111_0000) >> 4) | ((before & 0b0000_1111) << 4));
		memory.WriteUInt8(address.Value, after);
		ZeroFlag = after == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		Clock += 16;
	}

	private void GetBit(Register8 register, int bit)
	{
#if DEBUG
		logger.LogTrace($"BIT {bit}, {register}");
#endif
		var value = GetRegister(register);
		ZeroFlag = (value & (1 << bit)) == 0;
		SubtractFlag = false;
		HalfCarryFlag = true;
		Clock += 8;
	}

	private void GetBit(Address address, int bit)
	{
#if DEBUG
		logger.LogTrace($"BIT {bit}, {address}");
#endif
		var value = memory.ReadUInt8(address.Value);
		ZeroFlag = (value & (1 << bit)) == 0;
		SubtractFlag = false;
		HalfCarryFlag = true;
		Clock += 12;
	}

	private void ResetBit(Register8 register, int bit)
	{
#if DEBUG
		logger.LogTrace($"RES {bit}, {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)(before & (~(1 << bit)));
		SetRegister(register, after);
		Clock += 8;
	}

	private void ResetBit(Address address, int bit)
	{
#if DEBUG
		logger.LogTrace($"RES {bit}, {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)(before & (~(1 << bit)));
		memory.WriteUInt8(address.Value, after);
		Clock += 16;
	}

	private void SetBit(Register8 register, int bit)
	{
#if DEBUG
		logger.LogTrace($"SET {bit}, {register}");
#endif
		var before = GetRegister(register);
		var after = (byte)(before | (1 << bit));
		SetRegister(register, after);
		Clock += 8;
	}

	private void SetBit(Address address, int bit)
	{
#if DEBUG
		logger.LogTrace($"SET {bit}, {address}");
#endif
		var before = memory.ReadUInt8(address.Value);
		var after = (byte)(before | (1 << bit));
		memory.WriteUInt8(address.Value, after);
		Clock += 16;
	}

	private void InvalidInstruction(byte instruction)
	{
#if DEBUG
		logger.LogWarning($"INVALID {ToHex(instruction)}");
#endif
		if (instruction == 0xfd)
		{
			// 			var skip = ReadNextPCUInt8();
			// #if DEBUG
			// 			logger.LogWarning($"TODO JEFF skipping {ToHex(skip)}");
			// #endif

#if DEBUG
			logger.LogWarning($"TODO JEFF special case unknown, treat as prefix?");
#endif
			ExecuteInstruction();
		}
		Clock += 4;
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