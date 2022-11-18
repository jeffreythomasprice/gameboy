namespace Gameboy;

public interface IMemory
{
	byte ReadUInt8(UInt16 address);
	void WriteUInt8(UInt16 address, byte value);
}
