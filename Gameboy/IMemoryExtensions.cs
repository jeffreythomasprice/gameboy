namespace Gameboy;

public static class IMemoryExtensions
{
	public static UInt16 ReadUInt16(this IMemory memory, UInt16 address)
	{
		var low = memory.ReadUInt8(address);
		var high = memory.ReadUInt8((UInt16)(address + 1));
		return (UInt16)((high << 8) | low);
	}

	public static void WriteUInt16(this IMemory memory, UInt16 address, UInt16 value)
	{
		var low = (byte)(value & 0xff);
		var high = (byte)((value & 0xff00) >> 8);
		memory.WriteUInt8(address, low);
		memory.WriteUInt8((UInt16)(address + 1), high);
	}

	public static byte[] ReadArray(this IMemory memory, UInt16 address, int length)
	{
		var result = new byte[length];
		memory.ReadArray(result, 0, address, length);
		return result;
	}

	public static void WriteArray(this IMemory memory, UInt16 address, byte[] data)
	{
		for (var i = 0; i < data.Length; i++)
		{
			memory.WriteUInt8((UInt16)(address + i), data[i]);
		}
	}
}
