namespace Gameboy.Tests.TestROMs;

public class MemTiming2
{
	[Fact]
	public void Combined()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/mem_timing.gb",
			TimeSpan.FromSeconds(3),
			TimeSpan.FromSeconds(5),
			"",
			"c8a0e8ed3ace9f342495e6a693424ee9a05c9bc2"
		);
	}

	[Fact]
	public void _01_ReadTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/rom_singles/01-read_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"",
			"84c85b363412d95be66b419a122a2d985519df06"
		);
	}

	[Fact]
	public void _02_WriteTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/rom_singles/02-write_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"",
			"2bb3eb6f02a2ae9c37decf905cec40793fac5d38"
		);
	}

	[Fact]
	public void _03_ModifyTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/rom_singles/03-modify_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"",
			"f4a2d96f52230cdbaee6da55747a03502dc714eb"
		);
	}
}
