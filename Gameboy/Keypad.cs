using Microsoft.Extensions.Logging;

using static Gameboy.NumberUtils;

namespace Gameboy;

public class Keypad : ISteppable
{
	public delegate void KeypadRegisterDeltaDelegate(byte oldValue, byte newValue);

	public event KeypadRegisterDeltaDelegate? KeypadRegisterDelta;

	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;
	private Dictionary<Key, bool> state = new();

	public Keypad(ILoggerFactory loggerFactory, IMemory memory)
	{
		logger = loggerFactory.CreateLogger<Keypad>();
		this.memory = memory;
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		clock = 0;
		ClearKeys();
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;

		/*
		P10 through P13 are the low nibble of the keypad register, they represent current state
		P14 and P15 are bits 4 and 5 of the keypad register, they select which set of 4 keys go in P10 through P13
		P10 through P13 are reset when that key is pressed, and set when the key is not pressed
		
		docs say that P14 being reset and P15 being set selects the arrow keys, and the other way around selects the other keys
		
		example rom waiting for any key sets both P14 and P15
		I interpret this as the following:
		both reset or both set = all keys present in output, all keys trigger interrupts
		P14 reset and P15 set = only arrow keys
		P14 set and P15 reset = only the other keys
		*/
		var oldValue = memory.ReadUInt8(Memory.IO_P1);
		var p14 = (oldValue & 0b0001_0000) != 0;
		var p15 = (oldValue & 0b0010_0000) != 0;
		// TODO precalculate the key masks so we don't do dictionary lookup every step
		byte newValue = (byte)(0b0000_1111 | (oldValue & 0b1111_0000));
		if (!p14 || p15)
		{
			if (IsPressed(Key.Right))
			{
				newValue &= 0b1111_1110;
			}
			if (IsPressed(Key.Left))
			{
				newValue &= 0b1111_1101;
			}
			if (IsPressed(Key.Up))
			{
				newValue &= 0b1111_1011;
			}
			if (IsPressed(Key.Down))
			{
				newValue &= 0b1111_0111;
			}
		}
		if (p14 || !p15)
		{
			if (IsPressed(Key.A))
			{
				newValue &= 0b1111_1110;
			}
			if (IsPressed(Key.B))
			{
				newValue &= 0b1111_1101;
			}
			if (IsPressed(Key.Select))
			{
				newValue &= 0b1111_1011;
			}
			if (IsPressed(Key.Start))
			{
				newValue &= 0b1111_0111;
			}
		}
		memory.WriteUInt8(Memory.IO_P1, newValue);

		/*
		interrupt occurs whenever any of P10 through P13 go from high to low
		so we want the values that were set before and now are low
		so and the old value with the complement of the new value
		*/
		var delta = (oldValue & 0b0000_1111) & (~(newValue & 0b0000_1111));
		if (delta != 0)
		{
			logger.LogTrace($"keypad register delta {ToBinary(oldValue)} -> {ToBinary(newValue)}");
			KeypadRegisterDelta?.Invoke(oldValue, newValue);
			// set interrupt flag
			memory.WriteUInt8(Memory.IO_IF, (byte)(memory.ReadUInt8(Memory.IO_IF) | Memory.IF_MASK_KEYPAD));
		}
	}

	public void ClearKeys()
	{
		state.Clear();
	}

	public bool IsPressed(Key key) =>
		state.GetValueOrDefault(key, false);

	public void SetPressed(Key key, bool value)
	{
		state[key] = value;
	}
}