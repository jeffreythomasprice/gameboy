namespace Gameboy;

public interface IMemory : ISteppable
{
	byte ReadUInt8(UInt16 address);
	void WriteUInt8(UInt16 address, byte value);
}
