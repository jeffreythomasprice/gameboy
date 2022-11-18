namespace Gameboy;

public static class NumberUtils
{
	public const string BinaryPrefix = "#";
	public const string HexPrefix = "$";

	public static string ToBinary(byte x)
	{
		return BinaryPrefix + Convert.ToString(x, 2).PadLeft(8, '0');
	}

	public static string ToBinary(UInt16 x)
	{
		return BinaryPrefix + Convert.ToString(x, 2).PadLeft(16, '0');
	}

	public static string ToHex(byte x)
	{
		return HexPrefix + Convert.ToString(x, 16).PadLeft(2, '0');
	}

	public static string ToHex(UInt16 x)
	{
		return HexPrefix + Convert.ToString(x, 16).PadLeft(4, '0');
	}
}