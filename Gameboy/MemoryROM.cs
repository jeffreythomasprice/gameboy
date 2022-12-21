using Microsoft.Extensions.Logging;

namespace Gameboy;

public class MemoryROM : Memory
{
	public MemoryROM(ILoggerFactory loggerFactory, Cartridge cartridge, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad) : base(loggerFactory, cartridge, serialIO, timer, video, sound, keypad) { }

	protected override int ActiveLowROMBank =>
		0;

	protected override int ActiveHighROMBank =>
		1;

	protected override int ActiveRAMBank =>
		0;

	protected override bool RAMBankEnabled =>
		false;

	protected override void ROMWrite(ushort address, byte value)
	{
		// nothing to do
	}
}