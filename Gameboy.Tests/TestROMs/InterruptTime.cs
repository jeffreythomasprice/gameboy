namespace Gameboy.Tests.TestROMs;

public class InterruptTime
{
	[Fact]
	public void Test()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/interrupt_time/interrupt_time.gb",
			1000000,
			"""
			TODO JEFF
			"""
		);
	}
}