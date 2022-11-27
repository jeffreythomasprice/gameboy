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

	public MemoryMBC3(ILoggerFactory loggerFactory, Cartridge cartridge) : base(loggerFactory, cartridge) { }

	public override void Reset()
	{
		base.Reset();
		ramBankEnabled = 0x00;
		romBank = 0x00;
		ramBank = 0x00;
	}

	protected override int ActiveROMBank => romBank == 0 ? 1 : romBank;

	protected override int ActiveRAMBank => ramBank;

	protected override bool RAMBankEnabled => (ramBankEnabled & 0b0000_1111) == 0b0000_1010;

	protected override void ROMWrite(ushort address, byte value)
	{
		switch (address)
		{
			case <= RAM_DISABLE_END:
				ramBankEnabled = value;
				break;
			case <= ROM_BANK_SELECTOR_END:
				romBank = (byte)(value & 0b0111_1111);
				break;
			case <= RAM_BANK_SELECTOR_END:
				ramBank = (byte)(value & 0b0000_0011);
				break;
		}
	}
}