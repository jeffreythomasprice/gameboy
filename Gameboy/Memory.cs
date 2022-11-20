namespace Gameboy;

public class Memory : IMemory
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

	private readonly Cartridge cartridge;

	public Memory(Cartridge cartridge)
	{
		this.cartridge = cartridge;
	}

	public byte ReadUInt8(ushort address) =>
		address switch
		{
			<= ROM_BANK_0_END => throw new NotImplementedException("TODO ROM bank 0"),
			<= SWITCHABLE_ROM_BANK_END => throw new NotImplementedException("TODO switchable ROM bank"),
			<= VIDEO_RAM_END => throw new NotImplementedException("TODO video RAM"),
			<= SWITCHABLE_RAM_BANK_END => throw new NotImplementedException("TODO switchable RAM bank"),
			<= INTERNAL_RAM_1_END => throw new NotImplementedException("TODO internal RAM 1"),
			<= ECHO_INTERNAL_RAM_END => throw new NotImplementedException("TODO echo internal RAM"),
			<= SPRITE_ATTRIBUTES_END => throw new NotImplementedException("TODO sprite attributes"),
			<= UNUSED_1_END => throw new NotImplementedException("TODO unused 1"),
			<= IO_PORTS_END => throw new NotImplementedException("TODO IO ports"),
			<= UNUSED_2_END => throw new NotImplementedException("TODO unused 2"),
			<= INTERNAL_RAM_2_END => throw new NotImplementedException("TODO internal RAM 2"),
			INTERRUPT_ENABLE_REGISTER => throw new NotImplementedException("TODO interrupt enable"),
		};

	public void WriteUInt8(ushort address, byte value)
	{
		switch (address)
		{
			case <= ROM_BANK_0_END:
				throw new NotImplementedException("TODO ROM bank 0");
			case <= SWITCHABLE_ROM_BANK_END:
				throw new NotImplementedException("TODO switchable ROM bank");
			case <= VIDEO_RAM_END:
				throw new NotImplementedException("TODO video RAM");
			case <= SWITCHABLE_RAM_BANK_END:
				throw new NotImplementedException("TODO switchable RAM bank");
			case <= INTERNAL_RAM_1_END:
				throw new NotImplementedException("TODO internal RAM 1");
			case <= ECHO_INTERNAL_RAM_END:
				throw new NotImplementedException("TODO echo internal RAM");
			case <= SPRITE_ATTRIBUTES_END:
				throw new NotImplementedException("TODO sprite attributes");
			case <= UNUSED_1_END:
				throw new NotImplementedException("TODO unused 1");
			case <= IO_PORTS_END:
				throw new NotImplementedException("TODO IO ports");
			case <= UNUSED_2_END:
				throw new NotImplementedException("TODO unused 2");
			case <= INTERNAL_RAM_2_END:
				throw new NotImplementedException("TODO internal RAM 2");
			case <= INTERRUPT_ENABLE_REGISTER:
				throw new NotImplementedException("TODO interrupt enable");
		};
	}
}