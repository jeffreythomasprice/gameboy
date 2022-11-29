using Microsoft.Extensions.Logging;

namespace Gameboy;

public class MemoryMBC1 : Memory
{
	private const UInt16 RAM_DISABLE_START = 0x0000;
	private const UInt16 RAM_DISABLE_END = 0x1fff;
	private const UInt16 LOW_BITS_SELECTOR_START = 0x2000;
	private const UInt16 LOW_BITS_SELECTOR_END = 0x3fff;
	private const UInt16 HIGH_BITS_SELECTOR_START = 0x4000;
	private const UInt16 HIGH_BITS_SELECTOR_END = 0x5fff;
	private const UInt16 MEMORY_MODEL_SELECT_START = 0x6000;
	private const UInt16 MEMORY_MODEL_SELECT_END = 0x7fff;

	private bool ramBankEnabled;
	private byte lowBits;
	private byte highBits;
	// 0 = low ROM and RAM locked to 0, high ROM is banked on combination of low and high bits
	// 1 = low ROM and RAM use high bits, high ROM uses low bits
	private bool memoryMode;

	public MemoryMBC1(ILoggerFactory loggerFactory, Cartridge cartridge) : base(loggerFactory, cartridge) { }

	public override void Reset()
	{
		base.Reset();
		ramBankEnabled = false;
		lowBits = 0x01;
		highBits = 0x00;
		memoryMode = false;
	}

	protected override int ActiveLowROMBank =>
		memoryMode ? highBits : 0;

	protected override int ActiveHighROMBank =>
		memoryMode ? lowBits : lowBits | (highBits << 5);

	protected override int ActiveRAMBank =>
		memoryMode ? highBits : 0;

	protected override bool RAMBankEnabled =>
		ramBankEnabled;

	protected override void ROMWrite(ushort address, byte value)
	{
		switch (address)
		{
			case <= RAM_DISABLE_END:
				ramBankEnabled = (value & 0b0000_1111) == 0b0000_1010;
				break;
			case <= LOW_BITS_SELECTOR_END:
				lowBits = (byte)(value & 0b0001_1111);
				if (lowBits == 0)
				{
					lowBits = 1;
				}
				break;
			case <= HIGH_BITS_SELECTOR_END:
				highBits = (byte)(value & 0b0000_0011);
				break;
			case <= MEMORY_MODEL_SELECT_END:
				memoryMode = (value & 0b0000_0001) != 0;
				break;
		}
	}
}