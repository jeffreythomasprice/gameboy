namespace Gameboy;

public class MemoryMBC1 : Memory
{
	private const UInt16 RAM_DISABLE_START = 0x0000;
	private const UInt16 RAM_DISABLE_END = 0x1fff;
	private const UInt16 ROM_BANK_SELECTOR_START = 0x2000;
	private const UInt16 ROM_BANK_SELECTOR_END = 0x3fff;
	private const UInt16 RAM_BANK_SELECTOR_START = 0x4000;
	private const UInt16 RAM_BANK_SELECTOR_END = 0x5fff;
	private const UInt16 MEMORY_MODEL_SELECT_START = 0x6000;
	private const UInt16 MEMORY_MODEL_SELECT_END = 0x7fff;

	private byte ramBankEnabled;
	private byte romBankLow;
	private byte romBankHigh;
	private byte ramBank;
	private byte memoryModelSelector;

	public MemoryMBC1(Cartridge cartridge) : base(cartridge) { }

	public override void Reset()
	{
		base.Reset();
		ramBankEnabled = 0x00;
		memoryModelSelector = 0x00;
	}

	protected override int ActiveROMBank
	{
		get
		{
			var result = (romBankHigh << 5) | romBankLow;
			return result == 0 ? 1 : result;
		}
	}

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
				romBankLow = (byte)(value & 0b0001_1111);
				break;
			case <= RAM_BANK_SELECTOR_END:
				// "16/8 mode"
				if ((memoryModelSelector & 1) == 0)
				{
					romBankHigh = (byte)(value & 0b0000_0011);
				}
				// "4/32 mode"
				else
				{
					ramBank = (byte)(value & 0b0000_0011);
				}
				break;
			case <= MEMORY_MODEL_SELECT_END:
				memoryModelSelector = value;
				break;
		}
	}
}