namespace Gameboy;

public interface ISteppable
{
	/// <summary>
	/// Current time according to this component. Different components might advance at different rates.
	/// </summary>
	UInt64 Clock { get; }

	/// <summary>
	/// Reset this component to it's starting state.
	/// </summary>
	void Reset();

	/// <summary>
	/// Advance this component in whatever smallest increment makes sense.
	/// </summary>
	void Step();
}