using Microsoft.Extensions.Logging;

namespace Gameboy;

public abstract class Memory : IDisposable, IMemory, ISteppable
{
	public const UInt16 ROM_BANK_1_START = 0x0000;
	public const UInt16 ROM_BANK_1_END = ROM_BANK_2_START - 1;
	public const UInt16 ROM_BANK_2_START = 0x4000;
	public const UInt16 ROM_BANK_2_END = VIDEO_RAM_START - 1;
	public const UInt16 VIDEO_RAM_START = 0x8000;
	public const UInt16 VIDEO_RAM_END = RAM_BANK_START - 1;
	public const UInt16 RAM_BANK_START = 0xa000;
	public const UInt16 RAM_BANK_END = INTERNAL_RAM_1_START - 1;
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

	private bool isDisposed = false;

	private readonly ILogger logger;
	private readonly Cartridge cartridge;
	private readonly SerialIO serialIO;
	private readonly Timer timer;
	private readonly Video video;
	private readonly Sound sound;
	private readonly Keypad keypad;

	private int activeLowROMBank;
	private int activeHighROMBank;
	private int activeRAMBank;
	private bool ramBankEnabled;

	private readonly ReadOnlyMemory<byte>[] romBanks;
	private readonly byte[] internalRAM1 = new byte[INTERNAL_RAM_1_END - INTERNAL_RAM_1_START + 1];
	private readonly byte[] internalRAM2 = new byte[INTERNAL_RAM_2_END - INTERNAL_RAM_2_START + 1];
	// IO_IF
	private byte interruptFlags;
	private readonly byte[,] ramBanks;
	// IO_IE
	private byte interruptsEnabled;

	private UInt64 clock;

	private UInt16 dmaSourceAddress;
	private UInt16 dmaDestinationIndex;
	private int dmaCopiesRemaining;

	public Memory(ILoggerFactory loggerFactory, Cartridge cartridge, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad)
	{
		logger = loggerFactory.CreateLogger<Memory>();
		this.cartridge = cartridge;
		this.serialIO = serialIO;
		this.timer = timer;
		this.video = video;
		this.sound = sound;
		this.keypad = keypad;

		activeLowROMBank = 0;
		activeHighROMBank = 0;
		activeRAMBank = 0;
		ramBankEnabled = false;

		romBanks = new ReadOnlyMemory<byte>[cartridge.ROMBanks.Count];
		for (var i = 0; i < romBanks.Length; i++)
		{
			romBanks[i] = cartridge.GetROMBankBytes(i);
		}
		ramBanks = new byte[cartridge.RAMBanks.Count, cartridge.RAMBanks.Length];

		Reset();

		serialIO.DataAvailable += SerialIODataAvailable;
		timer.Overflow += TimerOverflow;
		video.VBlankInterrupt += VideoVBlankInterrupt;
		video.LCDCInterrupt += VideoLCDCInterrupt;
		keypad.KeypadRegisterDelta += KeypadRegisterDelta;
	}

	~Memory()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(true);
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public byte ReadUInt8(ushort address)
	{
		return address switch
		{
			<= ROM_BANK_1_END => romBanks[activeLowROMBank].Span[address],
			<= ROM_BANK_2_END => romBanks[activeHighROMBank].Span[address - ROM_BANK_2_START],

			<= VIDEO_RAM_END => video.ReadVideoUInt8((UInt16)(address - VIDEO_RAM_START)),

			<= RAM_BANK_END => ramBankEnabled && ramBanks.Length >= 1 ? ramBanks[activeRAMBank, address - RAM_BANK_START] : (byte)0xff,

			<= INTERNAL_RAM_1_END => internalRAM1[address - INTERNAL_RAM_1_START],

			<= ECHO_INTERNAL_RAM_END => internalRAM1[address - ECHO_INTERNAL_RAM_START],

			<= SPRITE_ATTRIBUTES_END => video.ReadSpriteAttributesUInt8((UInt16)(address - SPRITE_ATTRIBUTES_START)),

			<= UNUSED_1_END => 0,

			// keypad
			IO_P1 => keypad.RegisterP1,

			// serial IO
			IO_SB => serialIO.RegisterSB,
			IO_SC => serialIO.RegisterSC,

			// timer
			IO_DIV => timer.RegisterDIV,
			IO_TIMA => timer.RegisterTIMA,
			IO_TMA => timer.RegisterTMA,
			IO_TAC => timer.RegisterTAC,

			IO_IF => interruptFlags,

			// sound
			IO_NR10 => sound.RegisterNR10,
			IO_NR11 => sound.RegisterNR11,
			IO_NR12 => sound.RegisterNR12,
			IO_NR13 => sound.RegisterNR13,
			IO_NR14 => sound.RegisterNR14,
			IO_NR21 => sound.RegisterNR21,
			IO_NR22 => sound.RegisterNR22,
			IO_NR23 => sound.RegisterNR23,
			IO_NR24 => sound.RegisterNR24,
			IO_NR30 => sound.RegisterNR30,
			IO_NR31 => sound.RegisterNR31,
			IO_NR32 => sound.RegisterNR32,
			IO_NR33 => sound.RegisterNR33,
			IO_NR34 => sound.RegisterNR34,
			IO_NR41 => sound.RegisterNR41,
			IO_NR42 => sound.RegisterNR42,
			IO_NR43 => sound.RegisterNR43,
			IO_NR44 => sound.RegisterNR44,
			IO_NR50 => sound.RegisterNR50,
			IO_NR51 => sound.RegisterNR51,
			IO_NR52 => sound.RegisterNR52,
			IO_WAVE_PATTERN_RAM_START + 0 => sound.RegisterWavePattern0,
			IO_WAVE_PATTERN_RAM_START + 1 => sound.RegisterWavePattern1,
			IO_WAVE_PATTERN_RAM_START + 2 => sound.RegisterWavePattern2,
			IO_WAVE_PATTERN_RAM_START + 3 => sound.RegisterWavePattern3,
			IO_WAVE_PATTERN_RAM_START + 4 => sound.RegisterWavePattern4,
			IO_WAVE_PATTERN_RAM_START + 5 => sound.RegisterWavePattern5,
			IO_WAVE_PATTERN_RAM_START + 6 => sound.RegisterWavePattern6,
			IO_WAVE_PATTERN_RAM_START + 7 => sound.RegisterWavePattern7,
			IO_WAVE_PATTERN_RAM_START + 8 => sound.RegisterWavePattern8,
			IO_WAVE_PATTERN_RAM_START + 9 => sound.RegisterWavePattern9,
			IO_WAVE_PATTERN_RAM_START + 10 => sound.RegisterWavePattern10,
			IO_WAVE_PATTERN_RAM_START + 11 => sound.RegisterWavePattern11,
			IO_WAVE_PATTERN_RAM_START + 12 => sound.RegisterWavePattern12,
			IO_WAVE_PATTERN_RAM_START + 13 => sound.RegisterWavePattern13,
			IO_WAVE_PATTERN_RAM_START + 14 => sound.RegisterWavePattern14,
			IO_WAVE_PATTERN_RAM_START + 15 => sound.RegisterWavePattern15,

			// video
			IO_LCDC => video.RegisterLCDC,
			IO_STAT => video.RegisterSTAT,
			IO_SCY => video.RegisterSCY,
			IO_SCX => video.RegisterSCX,
			IO_LY => video.RegisterLY,
			IO_LYC => video.RegisterLYC,
			IO_DMA => 0,
			IO_BGP => video.RegisterBGP,
			IO_OBP0 => video.RegisterOBP0,
			IO_OBP1 => video.RegisterOBP1,
			IO_WY => video.RegisterWY,
			IO_WX => video.RegisterWX,

			<= UNUSED_2_END => 0,

			<= INTERNAL_RAM_2_END => internalRAM2[address - INTERNAL_RAM_2_START],

			IO_IE => interruptsEnabled,
		};
	}

	public void WriteUInt8(ushort address, byte value)
	{
		switch (address)
		{
			case <= ROM_BANK_1_END:
			case <= ROM_BANK_2_END:
				ROMWrite(address, value);
				break;

			case <= VIDEO_RAM_END:
				video.WriteVideoUInt8((UInt16)(address - VIDEO_RAM_START), value);
				break;

			case <= RAM_BANK_END:
				if (ramBankEnabled && ramBanks.Length >= 1)
				{
					ramBanks[activeRAMBank, address - RAM_BANK_START] = value;
				}
				break;

			case <= INTERNAL_RAM_1_END:
				internalRAM1[address - INTERNAL_RAM_1_START] = value;
				break;

			case <= ECHO_INTERNAL_RAM_END:
				internalRAM1[address - ECHO_INTERNAL_RAM_START] = value;
				break;

			case <= SPRITE_ATTRIBUTES_END:
				video.WriteSpriteAttributesUInt8((UInt16)(address - SPRITE_ATTRIBUTES_START), value);
				break;

			case <= UNUSED_1_END:
				break;

			// keypad
			case IO_P1:
				keypad.RegisterP1 = value;
				break;

			// serial IO
			case IO_SB:
				serialIO.RegisterSB = value;
				break;
			case IO_SC:
				serialIO.RegisterSC = value;
				break;

			// timer
			case IO_DIV:
				timer.RegisterDIV = value;
				break;
			case IO_TIMA:
				timer.RegisterTIMA = value;
				break;
			case IO_TMA:
				timer.RegisterTMA = value;
				break;
			case IO_TAC:
				timer.RegisterTAC = value;
				break;

			case IO_IF:
				interruptFlags = value;
				break;

			// sound
			case IO_NR10:
				sound.RegisterNR10 = value;
				break;
			case IO_NR11:
				sound.RegisterNR11 = value;
				break;
			case IO_NR12:
				sound.RegisterNR12 = value;
				break;
			case IO_NR13:
				sound.RegisterNR13 = value;
				break;
			case IO_NR14:
				sound.RegisterNR14 = value;
				break;
			case IO_NR21:
				sound.RegisterNR21 = value;
				break;
			case IO_NR22:
				sound.RegisterNR22 = value;
				break;
			case IO_NR23:
				sound.RegisterNR23 = value;
				break;
			case IO_NR24:
				sound.RegisterNR24 = value;
				break;
			case IO_NR30:
				sound.RegisterNR30 = value;
				break;
			case IO_NR31:
				sound.RegisterNR31 = value;
				break;
			case IO_NR32:
				sound.RegisterNR32 = value;
				break;
			case IO_NR33:
				sound.RegisterNR33 = value;
				break;
			case IO_NR34:
				sound.RegisterNR34 = value;
				break;
			case IO_NR41:
				sound.RegisterNR41 = value;
				break;
			case IO_NR42:
				sound.RegisterNR42 = value;
				break;
			case IO_NR43:
				sound.RegisterNR43 = value;
				break;
			case IO_NR44:
				sound.RegisterNR44 = value;
				break;
			case IO_NR50:
				sound.RegisterNR50 = value;
				break;
			case IO_NR51:
				sound.RegisterNR51 = value;
				break;
			case IO_NR52:
				sound.RegisterNR52 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 0:
				sound.RegisterWavePattern0 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 1:
				sound.RegisterWavePattern1 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 2:
				sound.RegisterWavePattern2 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 3:
				sound.RegisterWavePattern3 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 4:
				sound.RegisterWavePattern4 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 5:
				sound.RegisterWavePattern5 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 6:
				sound.RegisterWavePattern6 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 7:
				sound.RegisterWavePattern7 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 8:
				sound.RegisterWavePattern8 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 9:
				sound.RegisterWavePattern9 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 10:
				sound.RegisterWavePattern10 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 11:
				sound.RegisterWavePattern11 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 12:
				sound.RegisterWavePattern12 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 13:
				sound.RegisterWavePattern13 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 14:
				sound.RegisterWavePattern14 = value;
				break;
			case IO_WAVE_PATTERN_RAM_START + 15:
				sound.RegisterWavePattern15 = value;
				break;

			// video
			case IO_LCDC:
				video.RegisterLCDC = value;
				break;
			case IO_STAT:
				video.RegisterSTAT = value;
				break;
			case IO_SCY:
				video.RegisterSCY = value;
				break;
			case IO_SCX:
				video.RegisterSCX = value;
				break;
			case IO_LY:
				video.RegisterLY = value;
				break;
			case IO_LYC:
				video.RegisterLYC = value;
				break;
			case IO_DMA:
				{
					// the value written here is used to pick the high byte of the address to start reading from
					// only actually start a new DMA transfer if one is not in progress
					if (dmaCopiesRemaining == 0)
					{
						dmaSourceAddress = (UInt16)(value << 8);
						dmaDestinationIndex = 0;
						// docs seem to indicate that it copies only the 28 used bits from the 32-bit sprite values
						// 28 bits * 40 sprites = 140 bytes, where the whole sprite attrib area is 160 bytes
						// other docs and code samples make no mention of having to pack or unpack data though, so I'm assuming that's nonsense
						dmaCopiesRemaining = 160;
					}
				}
				break;
			case IO_BGP:
				video.RegisterBGP = value;
				break;
			case IO_OBP0:
				video.RegisterOBP0 = value;
				break;
			case IO_OBP1:
				video.RegisterOBP1 = value;
				break;
			case IO_WY:
				video.RegisterWY = value;
				break;
			case IO_WX:
				video.RegisterWX = value;
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

	public void ReadArray(byte[] destination, int destinationIndex, UInt16 address, int length)
	{
		if (length == 0)
		{
			return;
		}
#if DEBUG
		logger.LogWarning($"array copy from {NumberUtils.ToHex(address)}, len={length}");
#endif
		for (var i = 0; i < length; i++)
		{
			destination[destinationIndex + i] = ReadUInt8((UInt16)(address + i));
		}
	}

	public virtual void Reset()
	{
		// keypad
		// IO_P1

		// serial IO
		// IO_SB
		// IO_SC

		// timer
		// IO_DIV
		// IO_TIMA
		// IO_TMA
		// IO_TAC

		// IO_IF
		interruptFlags = 0x00;

		// sound
		// IO_NR10
		// IO_NR11
		// IO_NR12
		// IO_NR13
		// IO_NR14
		// IO_NR21
		// IO_NR22
		// IO_NR23
		// IO_NR24
		// IO_NR30
		// IO_NR31
		// IO_NR32
		// IO_NR33
		// IO_NR34
		// IO_NR41
		// IO_NR42
		// IO_NR43
		// IO_NR44
		// IO_NR50
		// IO_NR51
		// IO_NR52
		// IO_WAVE_PATTERN_RAM

		// video
		// IO_LCDC
		// IO_STAT
		// IO_SCY
		// IO_SCX
		// IO_LY
		// IO_LYC
		// IO_DMA is managed here, not in the video system, but doesn't have a specific byte of memory
		// IO_BGP
		// IO_OBP0
		// IO_OBP1
		// IO_WY
		// IO_WX

		// IO_IE
		interruptsEnabled = 0x00;

		clock = 0;

		// stuff for IO_DMA
		dmaSourceAddress = 0x0000;
		dmaDestinationIndex = 0;
		dmaCopiesRemaining = 0;
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;

		if (dmaCopiesRemaining > 0)
		{
			video.WriteSpriteAttributesUInt8IgnoreWriteControl(dmaDestinationIndex, ReadUInt8(dmaSourceAddress));
			dmaSourceAddress++;
			dmaDestinationIndex++;
			dmaCopiesRemaining--;
			if (dmaCopiesRemaining == 0)
			{
				logger.LogTrace("DMA complete");
			}
		}
	}

	/// <summary>
	/// Which ROM bank is active from 0x0000 to 0x3fff
	/// </summary>
	protected void ActiveLowROMBankChanged(int newValue)
	{
		activeLowROMBank = newValue % cartridge.ROMBanks.Count;
	}

	/// <summary>
	/// Which ROM bank is active from 0x4000 to 0x7fff
	/// </summary>
	protected void ActiveHighROMBankChanged(int newValue)
	{
		activeHighROMBank = newValue % cartridge.ROMBanks.Count;
	}

	/// <summary>
	/// Which RAM bank is active
	/// </summary>
	protected void ActiveRAMBankChanged(int newValue)
	{
		if (ramBanks.Length == 0)
		{
			activeRAMBank = 0;
		}
		else
		{
			activeRAMBank = newValue % ramBanks.Length;
		}
	}

	/// <summary>
	/// If false, reads and writes to RAM banks are ignored.
	/// </summary>
	protected void RAMBankEnabledChanged(bool newValue)
	{
		ramBankEnabled = newValue;
	}

	/// <summary>
	/// Called when a write is made to a ROM location.
	/// </summary>
	/// <param name="address">guaranteed to be in the range 0x0000 to 0x7fff, inclusive</param>
	/// <param name="value"></param>
	protected abstract void ROMWrite(UInt16 address, byte value);

	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			isDisposed = true;
			serialIO.DataAvailable -= SerialIODataAvailable;
			timer.Overflow -= TimerOverflow;
			video.VBlankInterrupt -= VideoVBlankInterrupt;
			video.LCDCInterrupt -= VideoLCDCInterrupt;
			keypad.KeypadRegisterDelta -= KeypadRegisterDelta;
		}
	}

	private void SerialIODataAvailable(byte value)
	{
		interruptFlags |= IF_MASK_SERIAL;
	}

	private void TimerOverflow()
	{
		interruptFlags |= IF_MASK_TIMER;
	}

	private void VideoVBlankInterrupt()
	{
		interruptFlags |= IF_MASK_VBLANK;
	}

	private void VideoLCDCInterrupt()
	{
		interruptFlags |= IF_MASK_LCDC;
	}

	private void KeypadRegisterDelta(byte oldValue, byte newValue)
	{
		interruptFlags |= IF_MASK_KEYPAD;
	}
}