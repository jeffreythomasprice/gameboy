namespace Gameboy;

public abstract class Memory : IMemory
{
	private const UInt16 ROM_BANK_0_START = 0x0000;
	private const UInt16 ROM_BANK_0_END = SWITCHABLE_ROM_BANK_START - 1;
	private const UInt16 SWITCHABLE_ROM_BANK_START = 0x4000;
	private const UInt16 SWITCHABLE_ROM_BANK_END = VIDEO_RAM_START - 1;
	private const UInt16 VIDEO_RAM_START = 0x8000;
	private const UInt16 VIDEO_RAM_END = SWITCHABLE_RAM_BANK_START - 1;
	private const UInt16 SWITCHABLE_RAM_BANK_START = 0xa000;
	private const UInt16 SWITCHABLE_RAM_BANK_END = INTERNAL_RAM_1_START - 1;
	private const UInt16 INTERNAL_RAM_1_START = 0xc000;
	private const UInt16 INTERNAL_RAM_1_END = ECHO_INTERNAL_RAM_START - 1;
	private const UInt16 ECHO_INTERNAL_RAM_START = 0xe000;
	private const UInt16 ECHO_INTERNAL_RAM_END = SPRITE_ATTRIBUTES_START - 1;
	private const UInt16 SPRITE_ATTRIBUTES_START = 0xfe00;
	private const UInt16 SPRITE_ATTRIBUTES_END = UNUSED_1_START - 1;
	private const UInt16 UNUSED_1_START = 0xfea0;
	private const UInt16 UNUSED_1_END = IO_PORTS_START - 1;
	private const UInt16 IO_PORTS_START = 0xff00;
	private const UInt16 IO_PORTS_END = UNUSED_2_START - 1;
	private const UInt16 UNUSED_2_START = 0xff4c;
	private const UInt16 UNUSED_2_END = INTERNAL_RAM_2_START - 1;
	private const UInt16 INTERNAL_RAM_2_START = 0xff80;
	private const UInt16 INTERNAL_RAM_2_END = INTERRUPT_ENABLE_REGISTER - 1;
	private const UInt16 INTERRUPT_ENABLE_REGISTER = 0xffff;

	private const int IO_INDEX_P1 = 0xff00 - IO_PORTS_START;
	private const int IO_INDEX_SB = 0xff01 - IO_PORTS_START;
	private const int IO_INDEX_SC = 0xff02 - IO_PORTS_START;
	// 0xff03 unused
	private const int IO_INDEX_DIV = 0xff04 - IO_PORTS_START;
	private const int IO_INDEX_TIMA = 0xff05 - IO_PORTS_START;
	private const int IO_INDEX_TMA = 0xff06 - IO_PORTS_START;
	private const int IO_INDEX_TAC = 0xff07 - IO_PORTS_START;
	// 0xff08 through 0xff0f unused
	private const int IO_INDEX_NR10 = 0xff10 - IO_PORTS_START;
	private const int IO_INDEX_NR11 = 0xff11 - IO_PORTS_START;
	private const int IO_INDEX_NR12 = 0xff12 - IO_PORTS_START;
	private const int IO_INDEX_NR13 = 0xff13 - IO_PORTS_START;
	private const int IO_INDEX_NR14 = 0xff14 - IO_PORTS_START;
	// 0xff15 unused
	private const int IO_INDEX_NR21 = 0xff16 - IO_PORTS_START;
	private const int IO_INDEX_NR22 = 0xff17 - IO_PORTS_START;
	private const int IO_INDEX_NR23 = 0xff18 - IO_PORTS_START;
	private const int IO_INDEX_NR24 = 0xff19 - IO_PORTS_START;
	private const int IO_INDEX_NR30 = 0xff1a - IO_PORTS_START;
	private const int IO_INDEX_NR31 = 0xff1b - IO_PORTS_START;
	private const int IO_INDEX_NR32 = 0xff1c - IO_PORTS_START;
	private const int IO_INDEX_NR33 = 0xff1d - IO_PORTS_START;
	private const int IO_INDEX_NR34 = 0xff1e - IO_PORTS_START;
	// 0xff1f unused
	private const int IO_INDEX_NR41 = 0xff20 - IO_PORTS_START;
	private const int IO_INDEX_NR42 = 0xff21 - IO_PORTS_START;
	private const int IO_INDEX_NR43 = 0xff22 - IO_PORTS_START;
	private const int IO_INDEX_NR44 = 0xff23 - IO_PORTS_START;
	private const int IO_INDEX_NR50 = 0xff24 - IO_PORTS_START;
	private const int IO_INDEX_NR51 = 0xff25 - IO_PORTS_START;
	private const int IO_INDEX_NR52 = 0xff26 - IO_PORTS_START;
	// 0xff27 through 0xff2f unused
	private const int IO_INDEX_WAVE_PATTERN_RAM_START = 0xff30 - IO_PORTS_START;
	private const int IO_INDEX_WAVE_PATTERN_RAM_END = 0xff3f - IO_PORTS_START;
	private const int IO_INDEX_LCDC = 0xff40 - IO_PORTS_START;
	private const int IO_INDEX_STAT = 0xff41 - IO_PORTS_START;
	private const int IO_INDEX_SCY = 0xff42 - IO_PORTS_START;
	private const int IO_INDEX_SCX = 0xff43 - IO_PORTS_START;
	private const int IO_INDEX_LY = 0xff44 - IO_PORTS_START;
	private const int IO_INDEX_LYC = 0xff45 - IO_PORTS_START;
	private const int IO_INDEX_DMA = 0xff46 - IO_PORTS_START;
	private const int IO_INDEX_BGP = 0xff47 - IO_PORTS_START;
	private const int IO_INDEX_OBP0 = 0xff48 - IO_PORTS_START;
	private const int IO_INDEX_OBP1 = 0xff49 - IO_PORTS_START;
	private const int IO_INDEX_WY = 0xff4a - IO_PORTS_START;
	private const int IO_INDEX_WX = 0xff4b - IO_PORTS_START;

	private readonly Cartridge cartridge;

	private readonly byte[] videoRAM = new byte[VIDEO_RAM_END - VIDEO_RAM_START + 1];
	private readonly byte[] spriteAttributes = new byte[SPRITE_ATTRIBUTES_END - SPRITE_ATTRIBUTES_START + 1];
	private readonly byte[] internalRAM1 = new byte[INTERNAL_RAM_1_END - INTERNAL_RAM_1_START + 1];
	private readonly byte[] internalRAM2 = new byte[INTERNAL_RAM_2_END - INTERNAL_RAM_2_START + 1];
	private readonly byte[] ioPorts = new byte[IO_PORTS_END - IO_PORTS_START + 1];
	private readonly byte[,] switchableRAMBanks;
	private byte interruptsEnabled;

	public Memory(Cartridge cartridge)
	{
		this.cartridge = cartridge;
		switchableRAMBanks = new byte[cartridge.RAMBanks.Count, cartridge.RAMBanks.Length];
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
			INTERRUPT_ENABLE_REGISTER => interruptsEnabled,
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
				ioPorts[address - IO_PORTS_START] = value;
				break;
			case <= UNUSED_2_END:
				break;
			case <= INTERNAL_RAM_2_END:
				internalRAM2[address - INTERNAL_RAM_2_START] = value;
				break;
			case <= INTERRUPT_ENABLE_REGISTER:
				interruptsEnabled = value;
				break;
		};
	}

	public virtual void Reset()
	{
		ioPorts[IO_INDEX_P1] = 0x00;
		ioPorts[IO_INDEX_SB] = 0x00;
		ioPorts[IO_INDEX_SC] = 0x00;
		ioPorts[IO_INDEX_DIV] = 0x00;
		ioPorts[IO_INDEX_TIMA] = 0x00;
		ioPorts[IO_INDEX_TMA] = 0x00;
		ioPorts[IO_INDEX_TAC] = 0x00;
		ioPorts[IO_INDEX_NR10] = 0x80;
		ioPorts[IO_INDEX_NR11] = 0xbf;
		ioPorts[IO_INDEX_NR12] = 0xf3;
		ioPorts[IO_INDEX_NR13] = 0x00;
		ioPorts[IO_INDEX_NR14] = 0xbf;
		ioPorts[IO_INDEX_NR21] = 0x3f;
		ioPorts[IO_INDEX_NR22] = 0x00;
		ioPorts[IO_INDEX_NR23] = 0x00;
		ioPorts[IO_INDEX_NR24] = 0xbf;
		ioPorts[IO_INDEX_NR30] = 0x7f;
		ioPorts[IO_INDEX_NR31] = 0xff;
		ioPorts[IO_INDEX_NR32] = 0x9f;
		ioPorts[IO_INDEX_NR33] = 0x00;
		ioPorts[IO_INDEX_NR34] = 0xbf;
		ioPorts[IO_INDEX_NR41] = 0xff;
		ioPorts[IO_INDEX_NR42] = 0x00;
		ioPorts[IO_INDEX_NR43] = 0x00;
		ioPorts[IO_INDEX_NR50] = 0x77;
		ioPorts[IO_INDEX_NR51] = 0xf3;
		// f1 for gameboy, f0 for super gameboy
		ioPorts[IO_INDEX_NR52] = 0xf1;
		for (var i = IO_INDEX_WAVE_PATTERN_RAM_START; i <= IO_INDEX_WAVE_PATTERN_RAM_END; i++)
		{
			ioPorts[i] = 0x00;
		}
		ioPorts[IO_INDEX_LCDC] = 0x91;
		ioPorts[IO_INDEX_STAT] = 0x00;
		ioPorts[IO_INDEX_SCY] = 0x00;
		ioPorts[IO_INDEX_SCX] = 0x00;
		ioPorts[IO_INDEX_LY] = 0x00;
		ioPorts[IO_INDEX_LYC] = 0x00;
		ioPorts[IO_INDEX_DMA] = 0x00;
		ioPorts[IO_INDEX_BGP] = 0xfc;
		ioPorts[IO_INDEX_OBP0] = 0xff;
		ioPorts[IO_INDEX_OBP1] = 0xff;
		ioPorts[IO_INDEX_WY] = 0x00;
		ioPorts[IO_INDEX_WX] = 0x00;
		// 0xffff
		interruptsEnabled = 0x00;
	}

	/*
	TODO IO ports

	see file:///home/jeff/workspaces/personal/gameboy/references/GBCPUman.pdf
	page 35
	*/

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