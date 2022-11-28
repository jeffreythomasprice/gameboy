namespace Gameboy.Tests.TestROMs;

public class InstructionTiming
{
	[Fact]
	public void Test()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/instr_timing/instr_timing.gb",
			3000000,
			"""
			instr_timing


			Passed
			
			"""
		);
	}
}