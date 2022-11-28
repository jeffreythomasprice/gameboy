namespace Gameboy;

public static class TimeUtils
{
	/// <summary>
	/// Convert a number of clock ticks to real time units.
	/// </summary>
	/// <param name="clock">a time in clock ticks</param>
	/// <returns>the time in real time</returns>
	public static TimeSpan ToTimeSpan(UInt64 clock) =>
		TimeSpan.FromSeconds((double)clock / (double)CPU.ClockTicksPerSecond);

	/// <summary>
	/// Convert a time in real time units to clock ticks
	/// </summary>
	/// <param name="time"></param>
	/// <returns></returns>
	public static UInt64 ToClockTicks(TimeSpan time) =>
		(UInt64)(time.TotalSeconds * CPU.ClockTicksPerSecond);
}