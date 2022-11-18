namespace Gameboy;

public static class IMemoryExtensions
{
	public static byte[] Read(this IMemory memory, UInt16 address, int length)
	{
		var result = new byte[length];
		for (var i = 0; i < length; i++)
		{
			result[i] = memory.Read((UInt16)(address + i));
		}
		return result;
	}

	public static void Write(this IMemory memory, UInt16 address, byte[] data)
	{
		for (var i = 0; i < data.Length; i++)
		{
			memory.Write((UInt16)(address + i), data[i]);
		}
	}
}
