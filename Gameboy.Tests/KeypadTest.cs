namespace Gameboy.Tests;

public class KeypadTest
{
	[Fact]
	public void StateTracking()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var keypad = new Keypad(loggerFactory);

		Assert.False(keypad.IsPressed(Key.Left));
		Assert.False(keypad.IsPressed(Key.Right));
		Assert.False(keypad.IsPressed(Key.Up));
		Assert.False(keypad.IsPressed(Key.Down));
		Assert.False(keypad.IsPressed(Key.A));
		Assert.False(keypad.IsPressed(Key.B));
		Assert.False(keypad.IsPressed(Key.Start));
		Assert.False(keypad.IsPressed(Key.Select));

		foreach (var key in Enum.GetValues<Key>())
		{
			keypad.ClearKeys();
			Assert.False(keypad.IsPressed(Key.Left));
			Assert.False(keypad.IsPressed(Key.Right));
			Assert.False(keypad.IsPressed(Key.Up));
			Assert.False(keypad.IsPressed(Key.Down));
			Assert.False(keypad.IsPressed(Key.A));
			Assert.False(keypad.IsPressed(Key.B));
			Assert.False(keypad.IsPressed(Key.Start));
			Assert.False(keypad.IsPressed(Key.Select));

			keypad.SetPressed(key, true);
			foreach (var otherKey in Enum.GetValues<Key>())
			{
				if (key == otherKey)
				{
					Assert.True(keypad.IsPressed(otherKey));
				}
				else
				{
					Assert.False(keypad.IsPressed(otherKey));
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(MemoryRegisterData))]
	public void MemoryRegister(Key[] keysPressed, byte input, byte expected)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var keypad = new Keypad(loggerFactory);

		keypad.Reset();
		foreach (var key in keysPressed)
		{
			keypad.SetPressed(key, true);
		}
		keypad.RegisterP1 = input;
		keypad.Step();
		Assert.Equal(expected, keypad.RegisterP1);
	}

	public static IEnumerable<object?[]> MemoryRegisterData
	{
		get
		{
			yield return new object?[] {
				new Key[] { Key.Right, },
				// P14 reset, P15 set
				0b0010_0000,
				0b0010_1110,
			};
			yield return new object?[] {
				new Key[] {Key.Left, },
				// P14 reset, P15 set
				0b0010_0000,
				0b0010_1101,
			};
			yield return new object?[] {
				new Key[] {Key.Up, },
				// P14 reset, P15 set
				0b0010_0000,
				0b0010_1011,
			};
			yield return new object?[] {
				new Key[] {Key.Down, },
				// P14 reset, P15 set
				0b0010_0000,
				0b0010_0111,
			};
			yield return new object?[] {
				new Key[] {Key.A, },
				// P14 set, P15 reset
				0b0001_0000,
				0b0001_1110,
			};
			yield return new object?[] {
				new Key[] {Key.B, },
				// P14 set, P15 reset
				0b0001_0000,
				0b0001_1101,
			};
			yield return new object?[] {
				new Key[] {Key.Select, },
				// P14 set, P15 reset
				0b0001_0000,
				0b0001_1011,
			};
			yield return new object?[] {
				new Key[] {Key.Start, },
				// P14 set, P15 reset
				0b0001_0000,
				0b0001_0111,
			};
			yield return new object?[] {
				new Key[] { Key.Right, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1110,
			};
			yield return new object?[] {
				new Key[] { Key.Left, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1101,
			};
			yield return new object?[] {
				new Key[] { Key.Up, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1011,
			};
			yield return new object?[] {
				new Key[] { Key.Down, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_0111,
			};
			yield return new object?[] {
				new Key[] {Key.A, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1110,
			};
			yield return new object?[] {
				new Key[] {Key.B, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1101,
			};
			yield return new object?[] {
				new Key[] {Key.Select, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_1011,
			};
			yield return new object?[] {
				new Key[] {Key.Start, },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_0111,
			};
			yield return new object?[] {
				new Key[] { Key.Right, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1110,
			};
			yield return new object?[] {
				new Key[] {Key.Left, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1101,
			};
			yield return new object?[] {
				new Key[] {Key.Up, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1011,
			};
			yield return new object?[] {
				new Key[] {Key.Down, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_0111,
			};
			yield return new object?[] {
				new Key[] {Key.A, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1110,
			};
			yield return new object?[] {
				new Key[] {Key.B, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1101,
			};
			yield return new object?[] {
				new Key[] {Key.Select, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_1011,
			};
			yield return new object?[] {
				new Key[] {Key.Start, },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_0111,
			};
			yield return new object?[] {
				new Key[] { Key.Right, Key.Start },
				// P14 reset, P15 set
				0b0010_0000,
				0b0010_1110,
			};
			yield return new object?[] {
				new Key[] { Key.Right, Key.Start },
				// P14 reset, P15 reset
				0b0000_0000,
				0b0000_0110,
			};
			yield return new object?[] {
				new Key[] { Key.Right, Key.Start },
				// P14 set, P15 set
				0b0011_0000,
				0b0011_0110,
			};
		}
	}

	[Theory]
	[MemberData(nameof(InterruptData))]
	public void Interrupt(Key[] keysPressed, byte input, bool expectedInterruptToFire)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var keypad = new Keypad(loggerFactory);
		var (memory, interruptRegisters) = MemoryUtils.CreateMemoryROM(loggerFactory, new SerialIO(loggerFactory), new Timer(loggerFactory), new Video(loggerFactory), new Sound(loggerFactory), keypad, new byte[0]);
		var cpu = new CPU(loggerFactory, memory, interruptRegisters, () =>
		{
			// intentionally left blank
		});

		// enable interrupts
		memory.WriteUInt8(Memory.IO_IE, InterruptRegisters.IF_MASK_KEYPAD);

		var interruptFired = false;
		keypad.KeypadRegisterDelta += (oldValue, newValue) =>
		{
			interruptFired = true;
		};

		keypad.Reset();
		foreach (var key in keysPressed)
		{
			keypad.SetPressed(key, true);
		}
		memory.WriteUInt8(Memory.IO_P1, input);
		keypad.Step();
		Assert.Equal(expectedInterruptToFire, interruptFired);
		if (expectedInterruptToFire)
		{
			// flag has been set
			Assert.Equal(InterruptRegisters.IF_MASK_KEYPAD, memory.ReadUInt8(Memory.IO_IF));
		}
		else
		{
			// flag has been reset
			Assert.Equal(0b0000_0000, memory.ReadUInt8(Memory.IO_IF));
		}
		cpu.Step();
		// flag has been reset
		Assert.Equal(0b0000_0000, memory.ReadUInt8(Memory.IO_IF));
	}

	public static IEnumerable<object?[]> InterruptData
	{
		get
		{
			yield return new object?[] {
				new Key[] { Key.Right, },
				// P14 reset, P15 set
				// all buttons not pressed to start
				0b0010_1111,
				true,
			};
			yield return new object?[] {
				new Key[] { Key.Right, },
				// P14 set, P15 reset
				// all buttons not pressed to start
				0b0001_1111,
				false,
			};
			yield return new object?[] {
				new Key[] { Key.Left, Key.Select },
				// P14 reset, P15 reset
				// all buttons not pressed to start
				0b0000_1111,
				true,
			};
			yield return new object?[] {
				new Key[] { Key.Start, },
				// P14 set, P15 set
				// this button was pressed to start
				0b0011_0111,
				false,
			};
		}
	}
}