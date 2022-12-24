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
			instr_timing


			Passed
			
			""",
			"TODO hash goes here"
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
			instr_timing


			Passed
			
			""",
			"TODO hash goes here"
		);
	}
}
