namespace Gameboy.Tests;

public class SimpleMemory : IMemory
{
	private UInt64 clock;

	private byte[] data = new byte[0];

	public ulong Clock => clock;

	public byte ReadUInt8(UInt16 address)
	{
		if (address < data.Length)
		{
			return data[address];
		}
		return 0;
	}

	public void Reset()
	{
		clock = 0;
	}

	public void Step()
	{
		clock++;
	}

	public void WriteUInt8(UInt16 address, byte value)
	{
		if (address >= data.Length)
		{
			var copy = new byte[(int)address + 1];
			Array.Copy(data, copy, data.Length);
			data = copy;
		}
		data[address] = value;
	}
}