namespace Gameboy.Tests.TestROMs;

public class InstructionTiming
{
	[Fact]
	public void Test()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/instr_timing/instr_timing.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			instr_timing


			Passed
			
			""",
			"357352886effe323e739598c6ee1f514d6420e87"
		);
	}
}