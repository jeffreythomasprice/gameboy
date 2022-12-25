using Microsoft.Extensions.Logging;

namespace Gameboy;

public class MemoryMBC5 : Memory
{
	private const UInt16 RAM_DISABLE_START = 0x0000;
	private const UInt16 RAM_DISABLE_END = 0x1fff;
	private const UInt16 ROM_BANK_SELECTOR_1_START = 0x2000;
	private const UInt16 ROM_BANK_SELECTOR_1_END = 0x2fff;
	private const UInt16 ROM_BANK_SELECTOR_2_START = 0x3000;
	private const UInt16 ROM_BANK_SELECTOR_2_END = 0x3fff;
	private const UInt16 RAM_BANK_SELECTOR_START = 0x4000;
	private const UInt16 RAM_BANK_SELECTOR_END = 0x5fff;

	private byte ramBankEnabled;
	private byte romBankLow;
	private byte romBankHigh;
	private byte ramBank;

	public MemoryMBC5(ILoggerFactory loggerFactory, Cartridge cartridge, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad, InterruptRegisters interruptRegisters) : base(loggerFactory, cartridge, serialIO, timer, video, sound, keypad, interruptRegisters) { }

	public override void Reset()
	{
		base.Reset();

		ramBankEnabled = 0x00;
		romBankLow = 0x00;
		romBankHigh = 0x00;
		ramBank = 0x00;

		ActiveLowROMBankChanged(ActiveLowROMBank);
		ActiveHighROMBankChanged(ActiveHighROMBank);
		ActiveRAMBankChanged(ActiveRAMBank);
		RAMBankEnabledChanged(RAMBankEnabled);
	}

	protected override void ROMWrite(ushort address, byte value)
	{
		switch (address)
		{
			case <= RAM_DISABLE_END:
				ramBankEnabled = value;
				RAMBankEnabledChanged(RAMBankEnabled);
				break;
			case <= ROM_BANK_SELECTOR_1_END:
				romBankLow = value;
				ActiveHighROMBankChanged(ActiveHighROMBank);
				break;
			case <= ROM_BANK_SELECTOR_2_END:
				romBankHigh = (byte)(value & 0b0000_0001);
				ActiveHighROMBankChanged(ActiveHighROMBank);
				break;
			case <= RAM_BANK_SELECTOR_END:
				ramBank = (byte)(value & 0b0000_0011);
				ActiveRAMBankChanged(ActiveRAMBank);
				break;
		}
	}

	private int ActiveLowROMBank =>
		0;

	private int ActiveHighROMBank =>
		(romBankHigh << 8) | romBankLow;

	private int ActiveRAMBank =>
		ramBank;

	private bool RAMBankEnabled =>
		(ramBankEnabled & 0b0000_1111) == 0b0000_1010;
}