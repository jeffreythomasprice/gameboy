using Microsoft.Extensions.Logging;

namespace Gameboy;

public class MemoryROM : Memory
{
	public MemoryROM(ILoggerFactory loggerFactory, Cartridge cartridge, SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad, InterruptRegisters interruptRegisters) : base(loggerFactory, cartridge, serialIO, timer, video, sound, keypad, interruptRegisters) { }

	public override void Reset()
	{
		base.Reset();

		ActiveLowROMBankChanged(ActiveLowROMBank);
		ActiveHighROMBankChanged(ActiveHighROMBank);
		ActiveRAMBankChanged(ActiveRAMBank);
		RAMBankEnabledChanged(RAMBankEnabled);
	}

	private int ActiveLowROMBank =>
		0;

	private int ActiveHighROMBank =>
		1;

	private int ActiveRAMBank =>
		0;

	private bool RAMBankEnabled =>
		false;

	protected override void ROMWrite(ushort address, byte value)
	{
		// nothing to do
	}
}