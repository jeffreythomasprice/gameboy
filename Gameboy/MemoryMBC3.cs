using Microsoft.Extensions.Logging;

namespace Gameboy;

public class MemoryMBC3 : Memory
{
	private const UInt16 RAM_DISABLE_START = 0x0000;
	private const UInt16 RAM_DISABLE_END = 0x1fff;
	private const UInt16 ROM_BANK_SELECTOR_START = 0x2000;
	private const UInt16 ROM_BANK_SELECTOR_END = 0x3fff;
	private const UInt16 RAM_BANK_SELECTOR_START = 0x4000;
	private const UInt16 RAM_BANK_SELECTOR_END = 0x5fff;

	private byte ramBankEnabled;
	private byte romBank;
	private byte ramBank;

	public MemoryMBC3(ILoggerFactory loggerFactory, Cartridge cartridge, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad, InterruptRegisters interruptRegisters) : base(loggerFactory, cartridge, serialIO, timer, video, sound, keypad, interruptRegisters) { }

	public override void Reset()
	{
		base.Reset();

		ramBankEnabled = 0x00;
		romBank = 0x00;
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
			case <= ROM_BANK_SELECTOR_END:
				romBank = (byte)(value & 0b0111_1111);
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
		romBank == 0 ? 1 : romBank;

	private int ActiveRAMBank =>
		ramBank;

	private bool RAMBankEnabled =>
		(ramBankEnabled & 0b0000_1111) == 0b0000_1010;
}