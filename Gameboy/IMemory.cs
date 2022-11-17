namespace Gameboy;

public interface IMemory
{
	byte Read(UInt16 address);
	void Write(UInt16 address, byte value);
}
