namespace Gameboy.Tests;

public class TimerTest
{
	[Theory]
	[MemberData(nameof(IncrementClockData))]
	public void IncrementClock(byte tac, byte tma, int numberOfStepsPerIncrement, int numberOfIncrementsToOverflow)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var timer = new Timer(loggerFactory, memory);
		var overflowed = false;
		timer.Overflow += () =>
		{
			overflowed = true;
		};

		memory.WriteUInt8(Memory.IO_TAC, tac);
		memory.WriteUInt8(Memory.IO_TMA, tma);
		// assume starting TIMA was reset to TMA like an overflow just occurred
		memory.WriteUInt8(Memory.IO_TIMA, tma);

		byte before;

		// loop a few times to prove the counters are stable
		for (var i = 0; i < 10; i++)
		{
			// e.g. TMA = 64 meaning we only have 192 increments until overflow, so we should be able to increment 191 times
			for (var overflowCounter = 0; overflowCounter < numberOfIncrementsToOverflow - 1; overflowCounter++)
			{
				// e.g. if TAC = 00 that means 1024 clock cycles, so 1023 cycles should go by before incrementing
				for (var incrementCounter = 0; incrementCounter < numberOfStepsPerIncrement - 1; incrementCounter++)
				{
					before = memory.ReadUInt8(Memory.IO_TIMA);
					timer.Step();
					Assert.Equal(before, memory.ReadUInt8(Memory.IO_TIMA));
					Assert.False(overflowed);
				}
				// one more step should see this increment
				before = memory.ReadUInt8(Memory.IO_TIMA);
				timer.Step();
				Assert.Equal((byte)(before + 1), memory.ReadUInt8(Memory.IO_TIMA));
			}

			// one more increment, so another round through the increment counter
			for (var incrementCounter = 0; incrementCounter < numberOfStepsPerIncrement - 1; incrementCounter++)
			{
				before = memory.ReadUInt8(Memory.IO_TIMA);
				timer.Step();
				Assert.Equal(before, memory.ReadUInt8(Memory.IO_TIMA));
				Assert.False(overflowed);
			}
			// and then one more step should see us overflow
			timer.Step();
			Assert.Equal(tma, memory.ReadUInt8(Memory.IO_TIMA));
			Assert.True(overflowed);
			overflowed = false;
		}
	}

	public static IEnumerable<object?[]> IncrementClockData
	{
		get
		{
			yield return new object?[] {
				// TAC, enabled, increment every 1024 ticks
				0b0000_0100,
				// TMA
				0,
				1024,
				256,
			};
			yield return new object?[] {
				// TAC, enabled, increment every 16 ticks
				0b0000_0101,
				// TMA
				0,
				16,
				256,
			};
			yield return new object?[] {
				// TAC, enabled, increment every 64 ticks
				0b0000_0110,
				// TMA
				0,
				64,
				256,
			};
			yield return new object?[] {
				// TAC, enabled, increment every 256 ticks
				0b0000_0111,
				// TMA
				0,
				256,
				256,
			};
			yield return new object?[] {
				// TAC, enabled, increment every 1024 ticks
				0b0000_0100,
				// TMA
				64,
				1024,
				256-64,
			};
			yield return new object?[] {
				// TAC, enabled, increment every 64 ticks
				0b0000_0110,
				// TMA
				77,
				64,
				256-77,
			};
		}
	}

	[Theory]
	[MemberData(nameof(TimerDisabledData))]
	public void TimerDisabled(byte tac, int iterations)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var timer = new Timer(loggerFactory, memory);
		var overflowed = false;
		timer.Overflow += () =>
		{
			overflowed = true;
		};

		memory.WriteUInt8(Memory.IO_TAC, tac);
		memory.WriteUInt8(Memory.IO_TMA, 0);
		memory.WriteUInt8(Memory.IO_TIMA, 0);

		for (var i = 0; i < iterations; i++)
		{
			timer.Step();
			Assert.Equal(0, memory.ReadUInt8(Memory.IO_TIMA));
			Assert.False(overflowed);
		}
	}

	public static IEnumerable<object?[]> TimerDisabledData
	{
		get
		{
			yield return new object?[] {
				// TAC, disabled, increment every 1024 ticks
				0b0000_0000,
				2*256*1024,
			};
			yield return new object?[] {
				// TAC, disabled, increment every 16 ticks
				0b0000_0001,
				2*256*1024,
			};
			yield return new object?[] {
				// TAC, disabled, increment every 64 ticks
				0b0000_0010,
				2*256*1024,
			};
			yield return new object?[] {
				// TAC, disabled, increment every 256 ticks
				0b0000_0011,
				2*256*1024,
			};
		}
	}

	[Fact]
	public void ResetDiv()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var timer = new Timer(loggerFactory, memory);
		var overflowed = false;
		timer.Overflow += () =>
		{
			overflowed = true;
		};

		// TAC, enabled, increment every 1024 ticks
		memory.WriteUInt8(Memory.IO_TAC, 0b0000_0100);
		memory.WriteUInt8(Memory.IO_TMA, 0);
		memory.WriteUInt8(Memory.IO_TIMA, 0);

		for (var i = 0; i < 512; i++)
		{
			timer.Step();
			Assert.False(overflowed);
		}

		// writing anything to div should reset it
		memory.WriteUInt8(Memory.IO_DIV, 0x77);

		timer.Step();
		Assert.Equal(0, memory.ReadUInt8(Memory.IO_DIV));

		// we just stepped once, so there should be TOTAL_STEPS - 2 remaining before it overflow
		for (var i = 0; i < 1024 * 256 - 2; i++)
		{
			timer.Step();
			Assert.False(overflowed);
		}

		// and then one more overflows
		timer.Step();
		Assert.True(overflowed);
	}

	[Fact]
	public void DisableAndReenable()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var timer = new Timer(loggerFactory, memory);
		var overflowed = false;
		timer.Overflow += () =>
		{
			overflowed = true;
		};

		// TAC, enabled, increment every 1024 ticks
		memory.WriteUInt8(Memory.IO_TAC, 0b0000_0100);
		memory.WriteUInt8(Memory.IO_TMA, 0);
		memory.WriteUInt8(Memory.IO_TIMA, 0);

		for (var i = 0; i < 1024 * 64; i++)
		{
			timer.Step();
			Assert.False(overflowed);
		}

		// TAC, disabled, increment every 1024 ticks
		memory.WriteUInt8(Memory.IO_TAC, 0b0000_0000);

		for (var i = 0; i < 1024 * 256 * 2; i++)
		{
			timer.Step();
			Assert.False(overflowed);
		}

		// TAC, enabled, increment every 1024 ticks
		memory.WriteUInt8(Memory.IO_TAC, 0b0000_0100);

		for (var i = 0; i < 1024 * 192 - 1; i++)
		{
			timer.Step();
			Assert.False(overflowed);
		}

		// and then one more overflows
		timer.Step();
		Assert.True(overflowed);
	}
}