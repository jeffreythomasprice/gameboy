namespace Gameboy;

public abstract class Memory : IMemory, ISteppable
{
	public delegate void MemoryWriteDelegate(byte oldValue, ref byte newValue);

	public const UInt16 ROM_BANK_0_START = 0x0000;
	public const UInt16 ROM_BANK_0_END = SWITCHABLE_ROM_BANK_START - 1;
	public const UInt16 SWITCHABLE_ROM_BANK_START = 0x4000;
	public const UInt16 SWITCHABLE_ROM_BANK_END = VIDEO_RAM_START - 1;
	public const UInt16 VIDEO_RAM_START = 0x8000;
	public const UInt16 VIDEO_RAM_END = SWITCHABLE_RAM_BANK_START - 1;
	public const UInt16 SWITCHABLE_RAM_BANK_START = 0xa000;
	public const UInt16 SWITCHABLE_RAM_BANK_END = INTERNAL_RAM_1_START - 1;
	public const UInt16 INTERNAL_RAM_1_START = 0xc000;
	public const UInt16 INTERNAL_RAM_1_END = ECHO_INTERNAL_RAM_START - 1;
	public const UInt16 ECHO_INTERNAL_RAM_START = 0xe000;
	public const UInt16 ECHO_INTERNAL_RAM_END = SPRITE_ATTRIBUTES_START - 1;
	public const UInt16 SPRITE_ATTRIBUTES_START = 0xfe00;
	public const UInt16 SPRITE_ATTRIBUTES_END = UNUSED_1_START - 1;
	public const UInt16 UNUSED_1_START = 0xfea0;
	public const UInt16 UNUSED_1_END = IO_PORTS_START - 1;
	public const UInt16 IO_PORTS_START = 0xff00;
	public const UInt16 IO_PORTS_END = UNUSED_2_START - 1;
	public const UInt16 UNUSED_2_START = 0xff4c;
	public const UInt16 UNUSED_2_END = INTERNAL_RAM_2_START - 1;
	public const UInt16 INTERNAL_RAM_2_START = 0xff80;
	public const UInt16 INTERNAL_RAM_2_END = IO_IE - 1;
	public const UInt16 IO_IE = 0xffff;

	public const UInt16 IO_P1 = 0xff00;
	public const UInt16 IO_SB = 0xff01;
	public const UInt16 IO_SC = 0xff02;
	// 0xff03 unused
	public const UInt16 IO_DIV = 0xff04;
	public const UInt16 IO_TIMA = 0xff05;
	public const UInt16 IO_TMA = 0xff06;
	public const UInt16 IO_TAC = 0xff07;
	// 0xff08 unused
	// 0xff09 unused
	public const UInt16 IO_IF = 0xff0f;
	public const UInt16 IO_NR10 = 0xff10;
	public const UInt16 IO_NR11 = 0xff11;
	public const UInt16 IO_NR12 = 0xff12;
	public const UInt16 IO_NR13 = 0xff13;
	public const UInt16 IO_NR14 = 0xff14;
	// 0xff15 unused
	public const UInt16 IO_NR21 = 0xff16;
	public const UInt16 IO_NR22 = 0xff17;
	public const UInt16 IO_NR23 = 0xff18;
	public const UInt16 IO_NR24 = 0xff19;
	public const UInt16 IO_NR30 = 0xff1a;
	public const UInt16 IO_NR31 = 0xff1b;
	public const UInt16 IO_NR32 = 0xff1c;
	public const UInt16 IO_NR33 = 0xff1d;
	public const UInt16 IO_NR34 = 0xff1e;
	// 0xff1f unused
	public const UInt16 IO_NR41 = 0xff20;
	public const UInt16 IO_NR42 = 0xff21;
	public const UInt16 IO_NR43 = 0xff22;
	public const UInt16 IO_NR44 = 0xff23;
	public const UInt16 IO_NR50 = 0xff24;
	public const UInt16 IO_NR51 = 0xff25;
	public const UInt16 IO_NR52 = 0xff26;
	// 0xff27 through 0xff2f unused
	public const UInt16 IO_WAVE_PATTERN_RAM_START = 0xff30;
	public const UInt16 IO_WAVE_PATTERN_RAM_END = 0xff3f;
	public const UInt16 IO_LCDC = 0xff40;
	public const UInt16 IO_STAT = 0xff41;
	public const UInt16 IO_SCY = 0xff42;
	public const UInt16 IO_SCX = 0xff43;
	public const UInt16 IO_LY = 0xff44;
	public const UInt16 IO_LYC = 0xff45;
	public const UInt16 IO_DMA = 0xff46;
	public const UInt16 IO_BGP = 0xff47;
	public const UInt16 IO_OBP0 = 0xff48;
	public const UInt16 IO_OBP1 = 0xff49;
	public const UInt16 IO_WY = 0xff4a;
	public const UInt16 IO_WX = 0xff4b;

	public const byte IF_MASK_VBLANK = 0b0000_0001;
	public const byte IF_MASK_LCDC = 0b0000_0010;
	public const byte IF_MASK_TIMER = 0b0000_0100;
	public const byte IF_MASK_SERIAL = 0b0000_1000;
	public const byte IF_MASK_KEYPAD = 0b0001_0000;

	public event MemoryWriteDelegate? IORegisterDIVWrite;
	public event MemoryWriteDelegate? IORegisterLYWrite;

	private readonly Cartridge cartridge;

	private readonly byte[] videoRAM = new byte[VIDEO_RAM_END - VIDEO_RAM_START + 1];
	private readonly byte[] spriteAttributes = new byte[SPRITE_ATTRIBUTES_END - SPRITE_ATTRIBUTES_START + 1];
	private readonly byte[] internalRAM1 = new byte[INTERNAL_RAM_1_END - INTERNAL_RAM_1_START + 1];
	private readonly byte[] internalRAM2 = new byte[INTERNAL_RAM_2_END - INTERNAL_RAM_2_START + 1];
	private readonly byte[] ioPorts = new byte[IO_PORTS_END + 1];
	private readonly byte[,] switchableRAMBanks;
	private byte interruptsEnabled;
	private UInt64 clock;

	public Memory(Cartridge cartridge)
	{
		this.cartridge = cartridge;
		switchableRAMBanks = new byte[cartridge.RAMBanks.Count, cartridge.RAMBanks.Length];
		Reset();
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public byte ReadUInt8(ushort address) =>
		address switch
		{
			<= ROM_BANK_0_END => cartridge.GetROMBankBytes(0)[address],
			<= SWITCHABLE_ROM_BANK_END => cartridge.GetROMBankBytes(ActiveROMBank)[address - SWITCHABLE_ROM_BANK_START],
			<= VIDEO_RAM_END => videoRAM[address - VIDEO_RAM_START],
			<= SWITCHABLE_RAM_BANK_END => RAMBankEnabled ? switchableRAMBanks[ActiveRAMBank, address - SWITCHABLE_RAM_BANK_START] : (byte)0,
			<= INTERNAL_RAM_1_END => internalRAM1[address - INTERNAL_RAM_1_START],
			<= ECHO_INTERNAL_RAM_END => internalRAM1[address - ECHO_INTERNAL_RAM_START],
			<= SPRITE_ATTRIBUTES_END => spriteAttributes[address - SPRITE_ATTRIBUTES_START],
			<= UNUSED_1_END => 0,
			<= IO_PORTS_END => ioPorts[address - IO_PORTS_START],
			<= UNUSED_2_END => 0,
			<= INTERNAL_RAM_2_END => internalRAM2[address - INTERNAL_RAM_2_START],
			IO_IE => interruptsEnabled,
		};

	public void WriteUInt8(ushort address, byte value)
	{
		switch (address)
		{
			case <= ROM_BANK_0_END:
			case <= SWITCHABLE_ROM_BANK_END:
				ROMWrite(address, value);
				break;
			case <= VIDEO_RAM_END:
				videoRAM[address - VIDEO_RAM_START] = value;
				break;
			case <= SWITCHABLE_RAM_BANK_END:
				if (RAMBankEnabled)
				{
					switchableRAMBanks[ActiveRAMBank, address - SWITCHABLE_RAM_BANK_START] = value;
				}
				break;
			case <= INTERNAL_RAM_1_END:
				internalRAM1[address - INTERNAL_RAM_1_START] = value;
				break;
			case <= ECHO_INTERNAL_RAM_END:
				internalRAM1[address - ECHO_INTERNAL_RAM_START] = value;
				break;
			case <= SPRITE_ATTRIBUTES_END:
				spriteAttributes[address - SPRITE_ATTRIBUTES_START] = value;
				break;
			case <= UNUSED_1_END:
				break;
			case <= IO_PORTS_END:
				{
					var oldValue = ioPorts[address - IO_PORTS_START];
					// special cases for events that might modify this value
					switch (address)
					{
						case IO_DIV:
							IORegisterDIVWrite?.Invoke(oldValue, ref value);
							break;
						case IO_LY:
							IORegisterLYWrite?.Invoke(oldValue, ref value);
							break;
					}
					ioPorts[address - IO_PORTS_START] = value;
				}
				break;
			case <= UNUSED_2_END:
				break;
			case <= INTERNAL_RAM_2_END:
				internalRAM2[address - INTERNAL_RAM_2_START] = value;
				break;
			case IO_IE:
				interruptsEnabled = value;
				break;
		};
	}

	public virtual void Reset()
	{
		ioPorts[IO_P1 - IO_PORTS_START] = 0x00;
		ioPorts[IO_SB - IO_PORTS_START] = 0x00;
		ioPorts[IO_SC - IO_PORTS_START] = 0x00;
		ioPorts[IO_DIV - IO_PORTS_START] = 0x00;
		ioPorts[IO_TIMA - IO_PORTS_START] = 0x00;
		ioPorts[IO_TMA - IO_PORTS_START] = 0x00;
		ioPorts[IO_TAC - IO_PORTS_START] = 0x00;
		ioPorts[IO_IF - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR10 - IO_PORTS_START] = 0x80;
		ioPorts[IO_NR11 - IO_PORTS_START] = 0xbf;
		ioPorts[IO_NR12 - IO_PORTS_START] = 0xf3;
		ioPorts[IO_NR13 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR14 - IO_PORTS_START] = 0xbf;
		ioPorts[IO_NR21 - IO_PORTS_START] = 0x3f;
		ioPorts[IO_NR22 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR23 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR24 - IO_PORTS_START] = 0xbf;
		ioPorts[IO_NR30 - IO_PORTS_START] = 0x7f;
		ioPorts[IO_NR31 - IO_PORTS_START] = 0xff;
		ioPorts[IO_NR32 - IO_PORTS_START] = 0x9f;
		ioPorts[IO_NR33 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR34 - IO_PORTS_START] = 0xbf;
		ioPorts[IO_NR41 - IO_PORTS_START] = 0xff;
		ioPorts[IO_NR42 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR43 - IO_PORTS_START] = 0x00;
		ioPorts[IO_NR50 - IO_PORTS_START] = 0x77;
		ioPorts[IO_NR51 - IO_PORTS_START] = 0xf3;
		// f1 for gameboy, f0 for super gameboy
		ioPorts[IO_NR52 - IO_PORTS_START] = 0xf1;
		for (var i = IO_WAVE_PATTERN_RAM_START; i <= IO_WAVE_PATTERN_RAM_END; i++)
		{
			ioPorts[i - IO_PORTS_START] = 0x00;
		}
		ioPorts[IO_LCDC - IO_PORTS_START] = 0x91;
		ioPorts[IO_STAT - IO_PORTS_START] = 0x00;
		ioPorts[IO_SCY - IO_PORTS_START] = 0x00;
		ioPorts[IO_SCX - IO_PORTS_START] = 0x00;
		ioPorts[IO_LY - IO_PORTS_START] = 0x00;
		ioPorts[IO_LYC - IO_PORTS_START] = 0x00;
		ioPorts[IO_DMA - IO_PORTS_START] = 0x00;
		ioPorts[IO_BGP - IO_PORTS_START] = 0xfc;
		ioPorts[IO_OBP0 - IO_PORTS_START] = 0xff;
		ioPorts[IO_OBP1 - IO_PORTS_START] = 0xff;
		ioPorts[IO_WY - IO_PORTS_START] = 0x00;
		ioPorts[IO_WX - IO_PORTS_START] = 0x00;
		// 0xffff
		interruptsEnabled = 0x00;
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;
	}

	/// <summary>
	/// Which ROM bank should be used in the switchable area.
	/// </summary>
	protected abstract int ActiveROMBank { get; }

	/// <summary>
	/// Which RAM bank should be used in the switchable area.
	/// </summary>
	protected abstract int ActiveRAMBank { get; }

	/// <summary>
	/// If false, reads and writes to RAM banks are ignored.
	/// </summary>
	protected abstract bool RAMBankEnabled { get; }

	/// <summary>
	/// Called when a write is made to a ROM location.
	/// </summary>
	/// <param name="address">guaranteed to be in the range 0x0000 to 0x7fff, inclusive</param>
	/// <param name="value"></param>
	protected abstract void ROMWrite(UInt16 address, byte value);
}