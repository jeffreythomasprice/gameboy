namespace Gameboy.Tests.TestROMs;

public class InterruptTime
{
	[Fact]
	public async Task Test()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/interrupt_time/interrupt_time.gb",
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(10),
			"",
			"TODO hash goes here"
		);
	}
}