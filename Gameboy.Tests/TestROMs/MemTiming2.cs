namespace Gameboy.Tests.TestROMs;

public class MemTiming2
{
	[Fact]
	public void Combined()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/mem_timing.gb",
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
			"gb-test-roms/mem_timing-2/rom_singles/01-read_timing.gb",
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
	public void _02_WriteTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/rom_singles/02-write_timing.gb",
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
	public void _03_ModifyTiming()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/mem_timing-2/rom_singles/03-modify_timing.gb",
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
