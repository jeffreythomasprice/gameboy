using System.Text;

namespace Gameboy;

public class Cartridge
{
	public enum Type
	{
		ROMOnly = 0x00,
		ROM_MBC1 = 0x01,
		ROM_MBC1_RAM = 0x02,
		ROM_MBC1_RAM_BATTERY = 0x03,
		ROM_MBC2 = 0x05,
		ROM_MBC2_BATTERY = 0x06,
		ROM_RAM = 0x08,
		ROM_RAM_BATTERY = 0x09,
		ROM_MMM01 = 0x0b,
		ROM_MMM01_SRAM = 0x0c,
		ROM_MMM01_SRAM_BATTERY = 0x0d,
		ROM_MBC3_TIMER_BATTERY = 0x0f,
		ROM_MBC3_TIMER_RAM_BATTERY = 0x10,
		ROM_MBC3 = 0x11,
		ROM_MBC3_RAM = 0x12,
		ROM_MBC3_RAM_BATTERY = 0x13,
		ROM_MBC5 = 0x19,
		ROM_MBC5_RAM = 0x1a,
		ROM_MBC5_RAM_BATTERY = 0x1b,
		ROM_MBC5_RUMBLE = 0x1c,
		ROM_MBC5_RUMBLE_SRAM = 0x1d,
		ROM_MBC5_RUMBLE_SRAM_BATTERY = 0x1d,
		POCKET_CAMERA = 0x1f,
		BANDAI_TAMA5 = 0xfd,
		HUDSON_HUC_3 = 0xfe,
		HUDSON_HUC_1 = 0xff,
	}

	public record Bank(int Count, int Length);

	private readonly byte[] data;

	public Cartridge(Stream input)
	{
		using var memoryStream = new MemoryStream();
		input.CopyTo(memoryStream);
		data = memoryStream.ToArray();
	}

	public int Length => data.Length;

	public ReadOnlySpan<byte> GetSpan(int start, int length) => data.AsSpan(start, length);

	public ReadOnlySpan<byte> GetSpan(Range range) => data.AsSpan(range);

	public string Title => Encoding.ASCII.GetString(GetSpan(new Range(0x0134, 0x0142 + 1)));

	public bool IsColorGameboy => data[0x0143] == 0x80;

	public bool IsSuperGameboy => data[0x0146] == 0x03;

	public Type CartridgeType => Enum.GetValues<Type>().First(x => ((byte)x) == data[0x0147]);

	public Bank ROMBanks
	{
		get
		{
			const int _16KB = 1024 * 16;
			var value = data[0x0148];
			return value switch
			{
				0 => new(2, _16KB),
				1 => new(4, _16KB),
				2 => new(8, _16KB),
				3 => new(16, _16KB),
				4 => new(32, _16KB),
				5 => new(64, _16KB),
				6 => new(128, _16KB),
				0x52 => new(72, _16KB),
				0x53 => new(80, _16KB),
				0x54 => new(96, _16KB),
				_ => throw new ArgumentOutOfRangeException($"unhandled ROM bank count: {value}"),
			};
		}
	}

	public Bank RAMBanks
	{
		get
		{
			const int _2KB = 1024 * 2;
			const int _8KB = 1024 * 8;
			var value = data[0x0149];
			return value switch
			{
				0 => new(0, 0),
				1 => new(1, _2KB),
				2 => new(1, _8KB),
				3 => new(4, _8KB),
				4 => new(16, _8KB),
				_ => throw new ArgumentOutOfRangeException($"unhandled RAM bank count: {value}"),
			};
		}
	}
}
