namespace Gameboy;

public static class ISteppableExtensions
{
	/// <summary>
	/// Repeatedly steps until the clock is at least the target.
	/// </summary>
	public static void StepTo(this ISteppable s, UInt64 clock)
	{
		while (s.Clock < clock)
		{
			s.Step();
		}
	}
}