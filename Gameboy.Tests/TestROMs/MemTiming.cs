namespace Gameboy.Tests.TestROMs;

public class MemTiming
{
	[Fact]
	public void Combined()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing/mem_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			mem_timing

			01:ok  02:ok  03:ok  

			Passed all tests

			""",
			"96b4c31af72e72328aa5116b1a96ba137b9bb7a1"
		);
	}

	[Fact]
	public void _01_ReadTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing/individual/01-read_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			01-read_timing


			Passed
			
			""",
			"84c85b363412d95be66b419a122a2d985519df06"
		);
	}

	[Fact]
	public void _02_WriteTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing/individual/02-write_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			02-write_timing


			Passed
			
			""",
			"2bb3eb6f02a2ae9c37decf905cec40793fac5d38"
		);
	}

	[Fact]
	public void _03_ModifyTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing/individual/03-modify_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			03-modify_timing


			Passed
			
			""",
			"f4a2d96f52230cdbaee6da55747a03502dc714eb"
		);
	}
}
