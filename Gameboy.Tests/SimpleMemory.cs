namespace Gameboy.Tests;

public class SimpleMemory : IMemory
{
	private byte[] data = new byte[0];

	public byte Read(UInt16 address)
	{
		if (address < data.Length)
		{
			return data[address];
		}
		return 0;
	}

	public void Write(UInt16 address, byte value)
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