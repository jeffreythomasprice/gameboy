namespace Gameboy.Tests;

public class CPUTest
{
	public record MemoryData(UInt16 Address, byte[] Data)
	{
		public void WriteTo(IMemory destination)
		{
			destination.WriteArray(Address, Data);
		}

		public void AssertEquals(IMemory actual)
		{
			Assert.Equal(Data, actual.ReadArray(Address, Data.Length));
		}
	}

	[Fact]
	public void Registers()
	{
		var cpu = new CPU(LoggerUtils.CreateLoggerFactory(), new SimpleMemory());

		cpu.RegisterA = 1;
		cpu.RegisterB = 2;
		cpu.RegisterC = 3;
		cpu.RegisterD = 4;
		cpu.RegisterE = 5;
		cpu.RegisterF = 6;
		cpu.RegisterH = 7;
		cpu.RegisterL = 8;
		cpu.RegisterSP = 0x1234;
		cpu.RegisterPC = 0x5678;

		Assert.Equal(1, cpu.RegisterA);
		Assert.Equal(2, cpu.RegisterB);
		Assert.Equal(3, cpu.RegisterC);
		Assert.Equal(4, cpu.RegisterD);
		Assert.Equal(5, cpu.RegisterE);
		// note that value of F is 0x00 and not 0x06 because some bits of F aren't usable
		Assert.Equal(0, cpu.RegisterF);
		Assert.Equal(7, cpu.RegisterH);
		Assert.Equal(8, cpu.RegisterL);
		// note that value of F is 0x00 and not 0x06 because some bits of F aren't usable
		Assert.Equal(0x0100, cpu.RegisterAF);
		Assert.Equal(0x0203, cpu.RegisterBC);
		Assert.Equal(0x0405, cpu.RegisterDE);
		Assert.Equal(0x0708, cpu.RegisterHL);
		Assert.Equal(0x1234, cpu.RegisterSP);
		Assert.Equal(0x5678, cpu.RegisterPC);

		cpu.RegisterAF = 0x1122;
		cpu.RegisterBC = 0x3344;
		cpu.RegisterDE = 0x5566;
		cpu.RegisterHL = 0x7788;
		Assert.Equal(0x11, cpu.RegisterA);
		Assert.Equal(0x33, cpu.RegisterB);
		Assert.Equal(0x44, cpu.RegisterC);
		Assert.Equal(0x55, cpu.RegisterD);
		Assert.Equal(0x66, cpu.RegisterE);
		// note that value of F is 0x20 and not 0x22 because some bits of F aren't usable
		Assert.Equal(0x20, cpu.RegisterF);
		Assert.Equal(0x77, cpu.RegisterH);
		Assert.Equal(0x88, cpu.RegisterL);
		// note that value of F is 0x20 and not 0x22 because some bits of F aren't usable
		Assert.Equal(0x1120, cpu.RegisterAF);
		Assert.Equal(0x3344, cpu.RegisterBC);
		Assert.Equal(0x5566, cpu.RegisterDE);
		Assert.Equal(0x7788, cpu.RegisterHL);

		cpu.RegisterF = 0b0000_0000;
		Assert.Equal(0b0000_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.False(cpu.CarryFlag);

		cpu.RegisterF = 0b1000_0000;
		Assert.Equal(0b1000_0000, cpu.RegisterF);
		Assert.True(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.False(cpu.CarryFlag);

		cpu.RegisterF = 0b0100_0000;
		Assert.Equal(0b0100_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.True(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.False(cpu.CarryFlag);

		cpu.RegisterF = 0b0010_0000;
		Assert.Equal(0b0010_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.True(cpu.HalfCarryFlag);
		Assert.False(cpu.CarryFlag);

		cpu.RegisterF = 0b0001_0000;
		Assert.Equal(0b0001_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.True(cpu.CarryFlag);

		cpu.ZeroFlag = true;
		cpu.SubtractFlag = true;
		cpu.HalfCarryFlag = true;
		cpu.CarryFlag = true;
		Assert.Equal(0b1111_0000, cpu.RegisterF);
		Assert.True(cpu.ZeroFlag);
		Assert.True(cpu.SubtractFlag);
		Assert.True(cpu.HalfCarryFlag);
		Assert.True(cpu.CarryFlag);

		cpu.ZeroFlag = false;
		Assert.Equal(0b0111_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.True(cpu.SubtractFlag);
		Assert.True(cpu.HalfCarryFlag);
		Assert.True(cpu.CarryFlag);

		cpu.SubtractFlag = false;
		Assert.Equal(0b0011_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.True(cpu.HalfCarryFlag);
		Assert.True(cpu.CarryFlag);

		cpu.HalfCarryFlag = false;
		Assert.Equal(0b0001_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.True(cpu.CarryFlag);

		cpu.CarryFlag = false;
		Assert.Equal(0b0000_0000, cpu.RegisterF);
		Assert.False(cpu.ZeroFlag);
		Assert.False(cpu.SubtractFlag);
		Assert.False(cpu.HalfCarryFlag);
		Assert.False(cpu.CarryFlag);
	}

	[Theory]
	[MemberData(nameof(InstructionData_00))]
	public void Instruction_00(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_00
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x00 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected,
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_01_11_21_31))]
	public void Instructions_01_11_21_31(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(3)
		);
	}

	public static IEnumerable<object?[]> InstructionData_01_11_21_31
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x01, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterBC(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x11, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterDE(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x21, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x31, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x3412),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_02_12_22_32))]
	public void Instructions_02_12_22_32(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object[]> InstructionData_02_12_22_32
	{
		get
		{
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x02, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterBC(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x12, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterDE(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x22, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterHL(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterHL(0x1235),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x32, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterHL(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42)
					.RegisterHL(0x1233),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_0a_1a_2a_3a))]
	public void Instructions_0a_1a_2a_3a(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object[]> InstructionData_0a_1a_2a_3a
	{
		get
		{
			yield return new object[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x0a, }),
					new(0x1234, new byte[] { 0x42, }),
				},
				(CPUBuilder actual) => actual
					.RegisterBC(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x1a, }),
					new(0x1234, new byte[] { 0x42, }),
				},
				(CPUBuilder actual) => actual
					.RegisterDE(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x2a, }),
					new(0x1234, new byte[] { 0x42, }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42)
					.RegisterHL(0x1235),
			};
			yield return new object[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x3a, }),
					new(0x1234, new byte[] { 0x42, }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] { new((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42)
					.RegisterHL(0x1233),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_03_13_23_33))]
	public void Instructions_03_13_23_33(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object[]> InstructionData_03_13_23_33
	{
		get
		{
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x03, }), },
				(CPUBuilder actual) => actual
					.RegisterBC(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterBC(0x1235),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x13, }), },
				(CPUBuilder actual) => actual
					.RegisterDE(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterDE(0x1235),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x23, }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0x1235),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x33, }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1235),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_0b_1b_2b_3b))]
	public void Instructions_0b_1b_2b_3b(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object[]> InstructionData_0b_1b_2b_3b
	{
		get
		{
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0b, }), },
				(CPUBuilder actual) => actual
					.RegisterBC(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterBC(0x1233),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1b, }), },
				(CPUBuilder actual) => actual
					.RegisterDE(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterDE(0x1233),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2b, }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0x1233),
			};
			yield return new object[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3b, }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1233),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_04_0c_14_1c_24_2c_3c))]
	public void Instructions_04_0c_14_1c_24_2c_3c(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_04_0c_14_1c_24_2c_3c
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x04, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x04, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x04, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0c, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0c, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0c, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x14, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x14, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x14, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1c, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1c, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1c, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x24, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x24, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x24, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2c, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2c, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2c, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3c, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3c, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3c, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_34))]
	public void Instructions_34(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_34
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x34, }),
					new(0x1234, new byte[] {0b0000_0001}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b0000_0010}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x34, }),
					new(0x1234, new byte[] {0b1111_1111}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b0000_0000}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x34, }),
					new(0x1234, new byte[] {0b0000_1111}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b0001_0000}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_05_0d_15_1d_25_2d_3d))]
	public void Instructions_05_0d_15_1d_25_2d_3d(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_05_0d_15_1d_25_2d_3d
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x05, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x05, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x05, }), },
				(CPUBuilder actual) => actual
					.RegisterB(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0d, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0d, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0d, }), },
				(CPUBuilder actual) => actual
					.RegisterC(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x15, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x15, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x15, }), },
				(CPUBuilder actual) => actual
					.RegisterD(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1d, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1d, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1d, }), },
				(CPUBuilder actual) => actual
					.RegisterE(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x25, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x25, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x25, }), },
				(CPUBuilder actual) => actual
					.RegisterH(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2d, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2d, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2d, }), },
				(CPUBuilder actual) => actual
					.RegisterL(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3d, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3d, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3d, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0011_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0010_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_35))]
	public void Instructions_35(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_35
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x35, }),
					new(0x1234, new byte[] {0b0000_0001}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b0000_0000}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x35, }),
					new(0x1234, new byte[] {0b1111_1111}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b1111_1110}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x35, }),
					new(0x1234, new byte[] {0b0011_0000}),
				 },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] {0b0010_1111}),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_06_0e_16_1e_26_2e_3e))]
	public void Instructions_06_0e_16_1e_26_2e_3e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_06_0e_16_1e_26_2e_3e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x06, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0e, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x16, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1e, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x26, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2e, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3e, 0x42, }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_36))]
	public void Instructions_36(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_36
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x36, 0x42, }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 } ), },
				(CPUBuilder expected) => expected,
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_07))]
	public void Instructions_07(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_07
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x07, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x07, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x07, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x07, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_0f))]
	public void Instructions_0f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_0f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1011)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x0f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_17))]
	public void Instructions_17(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_17
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x17, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x17, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x17, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_0100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x17, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_1f))]
	public void Instructions_1f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_1f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1011)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0110_0101)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x1f, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_08))]
	public void Instruction_08(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(20)
				.AddPC(3)
		);
	}

	public static IEnumerable<object?[]> InstructionData_08
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x08, 0x34, 0x12 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1122),
				new MemoryData[] { new(0x1234, new byte[] { 0x22, 0x11 }), },
				(CPUBuilder expected) => expected,
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_09_19_29_39))]
	public void Instruction_09_19_29_39(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_09_19_29_39
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x09 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_0001_0000_0010)
					.RegisterBC(0b0000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x09 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_1001_0000_0010)
					.RegisterBC(0b0000_1011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0001_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x09 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b1000_0001_0000_0010)
					.RegisterBC(0b1000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x19 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_0001_0000_0010)
					.RegisterDE(0b0000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x19 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_1001_0000_0010)
					.RegisterDE(0b0000_1011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0001_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x19 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b1000_0001_0000_0010)
					.RegisterDE(0b1000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x29 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_0001_0000_0010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0010_0000_0100)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x29 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_1001_0000_0010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0001_0010_0000_0100)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x29 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b1000_0001_0000_0010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0010_0000_0100)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x39 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_0001_0000_0010)
					.RegisterSP(0b0000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x39 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b0000_1001_0000_0010)
					.RegisterSP(0b0000_1011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0001_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x39 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0b1000_0001_0000_0010)
					.RegisterSP(0b1000_0011_0000_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0b0000_0100_0000_0110)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_10))]
	public void Instruction_10(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_10
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x10, 0x00 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.IsStopped(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_18_20_28_30_38))]
	public void Instruction_18_20_28_30_38(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
		);
	}

	public static IEnumerable<object?[]> InstructionData_18_20_28_30_38
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x18, 5 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 + 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x18, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 - 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x20, 5 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 + 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x20, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 - 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x20, 5 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x20, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x28, 5 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 + 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x28, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 - 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x28, 5 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x28, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x30, 5 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 + 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x30, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 - 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x30, 5 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x30, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x38, 5 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 + 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x38, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2 - 5)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x38, 5 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x38, unchecked((byte)(-5)) }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(2)
					.AddClock(8),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_27))]
	public void Instruction_27(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_27
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_0000)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0010_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0110_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_1100)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0110)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x27 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1001_1111)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_2f))]
	public void Instruction_2f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_2f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x2f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1010_1100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0101_0011)
					.SubtractFlag(true)
					.HalfCarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_37_3f))]
	public void Instruction_37_3f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_37_3f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x37 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x37 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3f }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x3f }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_40_41_42_43_44_45_47))]
	public void Instruction_40_41_42_43_44_45_47(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_40_41_42_43_44_45_47
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x40 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x41 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x42 }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x43 }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x44 }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x45 }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x47 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_46))]
	public void Instruction_46(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_46
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x46 }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterB(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_48_49_4a_4b_4c_4d_4f))]
	public void Instruction_48_49_4a_4b_4c_4d_4f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_48_49_4a_4b_4c_4d_4f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x48 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x49 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x4a }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x4b }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x4c }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x4d }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x4f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_4e))]
	public void Instruction_4e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_4e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x4e }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterC(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_50_51_52_53_54_55_57))]
	public void Instruction_50_51_52_53_54_55_57(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_50_51_52_53_54_55_57
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x50 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x51 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x52 }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x53 }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x54 }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x55 }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x57 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_56))]
	public void Instruction_56(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_56
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x56 }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterD(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_58_59_5a_5b_5c_5d_5f))]
	public void Instruction_58_59_5a_5b_5c_5d_5f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_58_59_5a_5b_5c_5d_5f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x58 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x59 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x5a }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x5b }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x5c }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x5d }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x5f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_5e))]
	public void Instruction_5e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_5e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x5e }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterE(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_60_61_62_63_64_65_67))]
	public void Instruction_60_61_62_63_64_65_67(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_60_61_62_63_64_65_67
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x60 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x61 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x62 }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x63 }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x64 }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x65 }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x67 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_66))]
	public void Instruction_66(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_66
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x66 }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterH(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_68_69_6a_6b_6c_6d_6f))]
	public void Instruction_68_69_6a_6b_6c_6d_6f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_68_69_6a_6b_6c_6d_6f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x68 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x69 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x6a }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x6b }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x6c }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x6d }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x6f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_6e))]
	public void Instruction_6e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_6e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x6e }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterL(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_70_71_72_73_74_75_77))]
	public void Instruction_70_71_72_73_74_75_77(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_70_71_72_73_74_75_77
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x70 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.RegisterB(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x71 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.RegisterC(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x72 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.RegisterD(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x73 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.RegisterE(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x74 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] { new(0x1234, new byte[] { 0x12 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x75 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] { new(0x1234, new byte[] { 0x34 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x77 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.RegisterA(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_78_79_7a_7b_7c_7d_7f))]
	public void Instruction_78_79_7a_7b_7c_7d_7f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_78_79_7a_7b_7c_7d_7f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x78 }), },
				(CPUBuilder actual) => actual
					.RegisterB(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x79 }), },
				(CPUBuilder actual) => actual
					.RegisterC(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x7a }), },
				(CPUBuilder actual) => actual
					.RegisterD(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x7b }), },
				(CPUBuilder actual) => actual
					.RegisterE(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x7c }), },
				(CPUBuilder actual) => actual
					.RegisterH(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x7d }), },
				(CPUBuilder actual) => actual
					.RegisterL(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x7f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_7e))]
	public void Instruction_7e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_7e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x7e }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_76))]
	public void Instruction_76(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_76
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x76 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.IsHalted(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_80_81_82_83_84_85_87))]
	public void Instruction_80_81_82_83_84_85_87(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_80_81_82_83_84_85_87
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x80 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x80 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterB(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x80 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterB(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x81 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x81 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterC(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x81 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterC(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x82 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x82 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterD(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x82 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterD(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x83 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x83 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterE(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x83 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterE(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x84 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x84 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterH(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x84 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterH(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x85 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x85 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterL(0b0000_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x85 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterL(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x87 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x87 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x87 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_86))]
	public void Instruction_86(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_86
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x86 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x86 }),
					new(0x1234, new byte[] { 0b0000_1111 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x86 }),
					new(0x1234, new byte[] { 0b0001_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_88_89_8a_8b_8c_8d_8f))]
	public void Instruction_88_89_8a_8b_8c_8d_8f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_88_89_8a_8b_8c_8d_8f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterB(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterB(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterB(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x88 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterB(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterC(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterC(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterC(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x89 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterC(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterD(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterD(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterD(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterD(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterE(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterE(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterE(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterE(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterH(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterH(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterH(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterH(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterL(0b0000_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterL(0b0000_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterL(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterL(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_1000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_1000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x8f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_8e))]
	public void Instruction_8e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_8e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0000_1111 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0000_1111 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0001_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x8e }),
					new(0x1234, new byte[] { 0b0001_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_90_91_92_93_94_95_97))]
	public void Instruction_90_91_92_93_94_95_97(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_90_91_92_93_94_95_97
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x90 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x90 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x90 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterB(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x91 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x91 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x91 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterC(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x92 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x92 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x92 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterD(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x93 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x93 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x93 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterE(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x94 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x94 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x94 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterH(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x95 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x95 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x95 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterL(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x97 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x97 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_96))]
	public void Instruction_96(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_96
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x96 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x96 }),
					new(0x1234, new byte[] { 0b0000_0001 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x96 }),
					new(0x1234, new byte[] { 0b0010_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_98_99_9a_9b_9c_9d_9f))]
	public void Instruction_98_99_9a_9b_9c_9d_9f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_98_99_9a_9b_9c_9d_9f
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterB(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x98 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterB(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterC(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x99 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterC(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterD(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9a }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterD(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterE(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9b }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterE(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterH(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9c }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterH(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterL(0b0010_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9d }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterL(0b0010_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0x9f }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_9e))]
	public void Instruction_9e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_9e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x9e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x9e }),
					new(0x1234, new byte[] { 0b0000_0001 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0x9e }),
					new(0x1234, new byte[] { 0b0010_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_a0_a1_a2_a3_a4_a5_a7))]
	public void Instruction_a0_a1_a2_a3_a4_a5_a7(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_a0_a1_a2_a3_a4_a5_a7
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa0 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterB(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa0 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterB(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa1 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterC(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa1 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterC(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa2 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterD(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa2 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterD(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa3 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterE(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa3 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterE(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa4 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterH(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa4 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterH(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa5 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterL(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa5 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterL(0b0010_0100),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa7 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1100_1010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa7 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_a6))]
	public void Instruction_a6(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_a6
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xa6 }),
					new(0x1234, new byte[] { 0b0110_0110 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xa6 }),
					new(0x1234, new byte[] { 0b0010_0100 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_a8_a9_aa_ab_ac_ad_af))]
	public void Instruction_a8_a9_aa_ab_ac_ad_af(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_a8_a9_aa_ab_ac_ad_af
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa8 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterB(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa8 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa9 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterC(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xa9 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xaa }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterD(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xaa }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xab }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterE(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xab }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xac }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterH(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xac }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xad }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterL(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xad }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xaf }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xaf }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_ae))]
	public void Instruction_ae(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_ae
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xae }),
					new(0x1234, new byte[] { 0b0110_0110 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xae }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_b0_b1_b2_b3_b4_b5_b7))]
	public void Instruction_b0_b1_b2_b3_b4_b5_b7(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_b0_b1_b2_b3_b4_b5_b7
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb0 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterB(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb0 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb1 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterC(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb1 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb2 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterD(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb2 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb3 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterE(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb3 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb4 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterH(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb4 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb5 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterL(0b0110_0110),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb5 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb7 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1100_1010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb7 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_b6))]
	public void Instruction_b6(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_b6
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xb6 }),
					new(0x1234, new byte[] { 0b0110_0110 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xb6 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_b8_b9_ba_bb_bc_bd_bf))]
	public void Instruction_b8_b9_ba_bb_bc_bd_bf(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_b8_b9_ba_bb_bc_bd_bf
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb8 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb8 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterB(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb8 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterB(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb9 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb9 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterC(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xb9 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterC(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xba }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xba }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterD(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xba }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterD(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbb }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbb }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterE(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbb }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterE(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbc }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbc }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterH(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbc }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterH(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbd }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbd }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterL(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbd }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterL(0b0010_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbf }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xbf }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_1111),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_be))]
	public void Instruction_be(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_be
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xbe }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xbe }),
					new(0x1234, new byte[] { 0b0000_0001 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xbe }),
					new(0x1234, new byte[] { 0b0010_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c0_c8_d0_d8))]
	public void Instruction_c0_c8_d0_d8(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c0_c8_d0_d8
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc0 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.AddClock(20),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc0 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(1)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc8 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.AddClock(20),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc8 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(1)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd0 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.AddClock(20),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd0 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(1)
					.AddClock(8),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd8 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.AddClock(20),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd8 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(1)
					.AddClock(8),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c1_d1_e1_f1))]
	public void Instruction_c1_d1_e1_f1(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c1_d1_e1_f1
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc1 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterBC(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd1 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterDE(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xe1 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterHL(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xf1 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterAF(0x3412),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c2_ca_d2_da))]
	public void Instruction_c2_ca_d2_da(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c2_ca_d2_da
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc2, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x3412)
					.AddClock(16),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc2, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xca, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x3412)
					.AddClock(16),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xca, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd2, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x3412)
					.AddClock(16),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd2, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xda, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x3412)
					.AddClock(16),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xda, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.AddPC(3)
					.AddClock(12),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c3))]
	public void Instruction_c3(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c3
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc3, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x3412),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c4_cc_d4_dc))]
	public void Instruction_c4_cc_d4_dc(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c4_cc_d4_dc
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc4, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(false),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[]{ 0x03, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe)
					.RegisterPC(0x3412)
					.AddClock(24),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc4, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1000)
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcc, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(true),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[]{ 0x03, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe)
					.RegisterPC(0x3412)
					.AddClock(24),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcc, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.ZeroFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1000)
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd4, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(false),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[]{ 0x03, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe)
					.RegisterPC(0x3412)
					.AddClock(24),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd4, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1000)
					.AddPC(3)
					.AddClock(12),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xdc, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(true),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[]{ 0x03, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe)
					.RegisterPC(0x3412)
					.AddClock(24),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xdc, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1000)
					.AddPC(3)
					.AddClock(12),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c5_d5_e5_f5))]
	public void Instruction_c5_d5_e5_f5(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c5_d5_e5_f5
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc5 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.RegisterBC(0x1234),
				new MemoryData[] { new(0x0ffe, new byte[] { 0x34, 0x12 }), },
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd5 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.RegisterDE(0x1234),
				new MemoryData[] { new(0x0ffe, new byte[] { 0x34, 0x12 }), },
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe5 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.RegisterHL(0x1234),
				new MemoryData[] { new(0x0ffe, new byte[] { 0x34, 0x12 }), },
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf5 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.RegisterAF(0x1234),
					// note the low byte is 0x30 and not 0x34, because some bits of F aren't usable
				new MemoryData[] { new(0x0ffe, new byte[] { 0x30, 0x12 }), },
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c6_ce_d6_de_e6_ee_f6_fe))]
	public void Instruction_c6_ce_d6_de_e6_ee_f6_fe(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c6_ce_d6_de_e6_ee_f6_fe
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc6, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc6, 0b0000_1111 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc6, 0b0001_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0000_1111 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0000)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0000_1111 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0001)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0001_0001)
					.ZeroFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0001_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0010)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xce, 0b0001_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1111_0010)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0011)
					.ZeroFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd6, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd6, 0b0000_0001 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd6, 0b0010_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0000_0001 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0000_0001 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_1110)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0010_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.CarryFlag(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1111_0000)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xde, 0b0010_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000)
					.CarryFlag(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1111)
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe6, 0b0110_0110 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0100_0010)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe6, 0b0010_0100 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xee, 0b0110_0110 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1010_1100)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xee, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf6, 0b0110_0110 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b1100_1010),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b1110_1110)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf6, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0b0000_0000)
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xfe, 0b0000_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xfe, 0b0000_0001 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0000_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xfe, 0b0010_0000 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0b0001_0000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(true)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c7_cf_d7_df_e7_ef_f7_ff))]
	public void Instruction_c7_cf_d7_df_e7_ef_f7_ff(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c7_cf_d7_df_e7_ef_f7_ff
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xc7 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0000)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcf }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0008)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xd7 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0010)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xdf }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0018)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe7 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0020)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xef }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0028)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf7 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0030)
					.RegisterSP(0x0ffe),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xff }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[] { 0x01, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterPC(0x0038)
					.RegisterSP(0x0ffe),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_c9_d9))]
	public void Instruction_c9_d9(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
		);
	}

	public static IEnumerable<object?[]> InstructionData_c9_d9
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xc9 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd9 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.InterruptsEnabled(true),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.InterruptsEnabled(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xd9 }),
					new(0x1000, new byte[] { 0x12, 0x34 }),
				},
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000)
					.InterruptsEnabled(false),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1002)
					.RegisterPC(0x3412)
					.InterruptsEnabled(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_cd))]
	public void Instruction_cd(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(24)
		);
	}

	public static IEnumerable<object?[]> InstructionData_cd
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcd, 0x12, 0x34 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[] {
					// CPU initial PC + size of this intruction
					new(0x0ffe, new byte[]{ 0x03, 0x01 }),
				},
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffe)
					.RegisterPC(0x3412),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_e0_f0))]
	public void Instruction_e0_f0(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_e0_f0
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe0, 0x12 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[] { new(0xff12, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xf0, 0x12 }),
					new(0xff12, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_e2_f2))]
	public void Instruction_e2_f2(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_e2_f2
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe2 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterC(0x12),
				new MemoryData[] { new(0xff12, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xf2 }),
					new(0xff12, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual
					.RegisterC(0x12),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_e8))]
	public void Instruction_e8(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_e8
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe8, 5 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1005)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe8, unchecked((byte)-5) }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x1000),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x0ffb)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe8, 1 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0xffff),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x0000)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_e9))]
	public void Instruction_e9(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(4)
		);
	}

	public static IEnumerable<object?[]> InstructionData_e9
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xe9 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterPC(0x1234),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_ea_fa))]
	public void Instruction_ea_fa(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(3)
		);
	}

	public static IEnumerable<object?[]> InstructionData_ea_fa
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xea, 0x34, 0x12 }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42),
				new MemoryData[] { new(0x1234, new byte[] { 0x42 }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xfa, 0x34, 0x12 }),
					new(0x1234, new byte[] { 0x42 }),
				},
				(CPUBuilder actual) => actual,
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterA(0x42),
			};
		}
	}

	[Fact]
	public void Instruction_f3_fb()
	{
		// special case for testing several steps, testing the value after each step
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		memory.WriteArray(CPU.InitialPC, new byte[] {
			// DI
			0xf3,
			// NOP
			0x00,
			// EI
			0xfb,
			// EI
			0xfb,
			// DI
			0xf3,
			// EI
			0xfb,
			// NOP,
			0x00,
			// NOP,
			0x00,
		});
		var actual = new CPUBuilder(loggerFactory, memory).CPU;
		var expected = new CPUBuilder(loggerFactory, memory).Copy(actual);
		AssertEqual(expected.CPU, actual);

		// should be about to execute DI
		actual.Step();
		expected.AddPC(1).AddClock(4);
		AssertEqual(expected.CPU, actual);

		// should be about to execute NOP
		actual.Step();
		expected.AddPC(1).AddClock(4);
		expected.InterruptsEnabled(false);
		AssertEqual(expected.CPU, actual);

		// should be about to execute EI
		actual.Step();
		expected.AddPC(1).AddClock(4);
		AssertEqual(expected.CPU, actual);

		// should be about to execute EI
		actual.Step();
		expected.AddPC(1).AddClock(4);
		expected.InterruptsEnabled(true);
		AssertEqual(expected.CPU, actual);

		// should be about to execute DI
		actual.Step();
		expected.AddPC(1).AddClock(4);
		expected.InterruptsEnabled(true);
		AssertEqual(expected.CPU, actual);

		// should be about to execute EI
		actual.Step();
		expected.AddPC(1).AddClock(4);
		expected.InterruptsEnabled(false);
		AssertEqual(expected.CPU, actual);

		// should be about to execute NOP
		actual.Step();
		expected.AddPC(1).AddClock(4);
		expected.InterruptsEnabled(true);
		AssertEqual(expected.CPU, actual);

		// should be about to execute NOP
		actual.Step();
		expected.AddPC(1).AddClock(4);
		AssertEqual(expected.CPU, actual);
	}

	[Theory]
	[MemberData(nameof(InstructionData_f8))]
	public void Instruction_f8(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(12)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_f8
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf8, 0x00 }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x0102),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0x0102)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf8, 0xff }), },
				(CPUBuilder actual) => actual
					.RegisterSP(0x01ff),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterHL(0x01fe)
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(true)
					.CarryFlag(true),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_f9))]
	public void Instruction_f9(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(1)
		);
	}

	public static IEnumerable<object?[]> InstructionData_f9
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xf9 }), },
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[0],
				(CPUBuilder expected) => expected
					.RegisterSP(0x1234),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_00_01_02_03_04_05_07))]
	public void Instruction_prefix_00_01_02_03_04_05_07(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_00_01_02_03_04_05_07
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x00, (builder, x) => builder.RegisterB(x)),
				(0x01, (builder, x) => builder.RegisterC(x)),
				(0x02, (builder, x) => builder.RegisterD(x)),
				(0x03, (builder, x) => builder.RegisterE(x)),
				(0x04, (builder, x) => builder.RegisterH(x)),
				(0x05, (builder, x) => builder.RegisterL(x)),
				(0x07, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_06))]
	public void Instruction_prefix_06(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_06
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x06 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x06 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x06 }),
					new(0x1234, new byte[] { 0b0100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x06 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_08_09_0a_0b_0c_0d_0f))]
	public void Instruction_prefix_08_09_0a_0b_0c_0d_0f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_08_09_0a_0b_0c_0d_0f
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x08, (builder, x) => builder.RegisterB(x)),
				(0x09, (builder, x) => builder.RegisterC(x)),
				(0x0a, (builder, x) => builder.RegisterD(x)),
				(0x0b, (builder, x) => builder.RegisterE(x)),
				(0x0c, (builder, x) => builder.RegisterH(x)),
				(0x0d, (builder, x) => builder.RegisterL(x)),
				(0x0f, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1011)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_0e))]
	public void Instruction_prefix_0e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_0e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x0e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x0e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x0e }),
					new(0x1234, new byte[] { 0b1100_1011 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x0e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_10_11_12_13_14_15_17))]
	public void Instruction_prefix_10_11_12_13_14_15_17(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_10_11_12_13_14_15_17
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x10, (builder, x) => builder.RegisterB(x)),
				(0x11, (builder, x) => builder.RegisterC(x)),
				(0x12, (builder, x) => builder.RegisterD(x)),
				(0x13, (builder, x) => builder.RegisterE(x)),
				(0x14, (builder, x) => builder.RegisterH(x)),
				(0x15, (builder, x) => builder.RegisterL(x)),
				(0x17, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_16))]
	public void Instruction_prefix_16(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_16
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x16 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x16 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x16 }),
					new(0x1234, new byte[] { 0b0100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x16 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_18_19_1a_1b_1c_1d_1f))]
	public void Instruction_prefix_18_19_1a_1b_1c_1d_1f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_18_19_1a_1b_1c_1d_1f
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x18, (builder, x) => builder.RegisterB(x)),
				(0x19, (builder, x) => builder.RegisterC(x)),
				(0x1a, (builder, x) => builder.RegisterD(x)),
				(0x1b, (builder, x) => builder.RegisterE(x)),
				(0x1c, (builder, x) => builder.RegisterH(x)),
				(0x1d, (builder, x) => builder.RegisterL(x)),
				(0x1f, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1011)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_1e))]
	public void Instruction_prefix_1e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_1e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x1e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x1e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x1e }),
					new(0x1234, new byte[] { 0b1100_1011 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x1e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_20_21_22_23_24_25_27))]
	public void Instruction_prefix_20_21_22_23_24_25_27(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_20_21_22_23_24_25_27
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x20, (builder, x) => builder.RegisterB(x)),
				(0x21, (builder, x) => builder.RegisterC(x)),
				(0x22, (builder, x) => builder.RegisterD(x)),
				(0x23, (builder, x) => builder.RegisterE(x)),
				(0x24, (builder, x) => builder.RegisterH(x)),
				(0x25, (builder, x) => builder.RegisterL(x)),
				(0x27, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1001_0100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_26))]
	public void Instruction_prefix_26(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_26
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x26 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x26 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x26 }),
					new(0x1234, new byte[] { 0b0100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1001_0100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x26 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_28_29_2a_2b_2c_2d_2f))]
	public void Instruction_prefix_28_29_2a_2b_2c_2d_2f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_28_29_2a_2b_2c_2d_2f
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x28, (builder, x) => builder.RegisterB(x)),
				(0x29, (builder, x) => builder.RegisterC(x)),
				(0x2a, (builder, x) => builder.RegisterD(x)),
				(0x2b, (builder, x) => builder.RegisterE(x)),
				(0x2c, (builder, x) => builder.RegisterH(x)),
				(0x2d, (builder, x) => builder.RegisterL(x)),
				(0x2f, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1011)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_2e))]
	public void Instruction_prefix_2e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_2e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x2e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x2e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x2e }),
					new(0x1234, new byte[] { 0b1100_1011 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x2e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_30_31_32_33_34_35_37))]
	public void Instruction_prefix_30_31_32_33_34_35_37(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_30_31_32_33_34_35_37
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x30, (builder, x) => builder.RegisterB(x)),
				(0x31, (builder, x) => builder.RegisterC(x)),
				(0x32, (builder, x) => builder.RegisterD(x)),
				(0x33, (builder, x) => builder.RegisterE(x)),
				(0x34, (builder, x) => builder.RegisterH(x)),
				(0x35, (builder, x) => builder.RegisterL(x)),
				(0x37, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b1010_1100)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_36))]
	public void Instruction_prefix_36(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_36
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x36 }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b1010_1100 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x36 }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_38_39_3a_3b_3c_3d_3f))]
	public void Instruction_prefix_38_39_3a_3b_3c_3d_3f(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_38_39_3a_3b_3c_3d_3f
	{
		get
		{
			foreach (var (instruction, setter) in new (byte, Func<CPUBuilder, byte, CPUBuilder>)[] {
				(0x38, (builder, x) => builder.RegisterB(x)),
				(0x39, (builder, x) => builder.RegisterC(x)),
				(0x3a, (builder, x) => builder.RegisterD(x)),
				(0x3b, (builder, x) => builder.RegisterE(x)),
				(0x3c, (builder, x) => builder.RegisterH(x)),
				(0x3d, (builder, x) => builder.RegisterL(x)),
				(0x3f, (builder, x) => builder.RegisterA(x)),
			})
			{
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1010)
						.CarryFlag(true),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b1100_1011)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0110_0101)
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
					(CPUBuilder actual) => setter(actual, 0b0000_0000)
						.CarryFlag(false),
					new MemoryData[0],
					(CPUBuilder expected) => setter(expected, 0b0000_0000)
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(false)
						.CarryFlag(false),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_3e))]
	public void Instruction_prefix_3e(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_3e
	{
		get
		{
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x3e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x3e }),
					new(0x1234, new byte[] { 0b1100_1010 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(true),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x3e }),
					new(0x1234, new byte[] { 0b1100_1011 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0110_0101 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(false)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(true),
			};
			yield return new object?[] {
				new MemoryData[] {
					new(CPU.InitialPC, new byte[] { 0xcb, 0x3e }),
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder actual) => actual
					.RegisterHL(0x1234)
					.CarryFlag(false),
				new MemoryData[] {
					new(0x1234, new byte[] { 0b0000_0000 }),
				},
				(CPUBuilder expected) => expected
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_40_through_7f_registers))]
	public void Instruction_prefix_40_through_7f_registers(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_40_through_7f_registers
	{
		get
		{
			foreach (var (bit, oneInJustThatBit, instructionBase) in new (int, byte, int)[] {
				(0, 0b0000_0001, 0x40),
				(1, 0b0000_0010, 0x48),
				(2, 0b0000_0100, 0x50),
				(3, 0b0000_1000, 0x58),
				(4, 0b0001_0000, 0x60),
				(5, 0b0010_0000, 0x68),
				(6, 0b0100_0000, 0x70),
				(7, 0b1000_0000, 0x78),
			})
			{
				foreach (var (instructionOffset, setter) in new (int, Func<CPUBuilder, byte, CPUBuilder>)[] {
					(0x00, (builder, x) => builder.RegisterB(x)),
					(0x01, (builder, x) => builder.RegisterC(x)),
					(0x02, (builder, x) => builder.RegisterD(x)),
					(0x03, (builder, x) => builder.RegisterE(x)),
					(0x04, (builder, x) => builder.RegisterH(x)),
					(0x05, (builder, x) => builder.RegisterL(x)),
					(0x07, (builder, x) => builder.RegisterA(x)),
				})
				{
					var instruction = (byte)(instructionBase + instructionOffset);
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, 0b0000_0000),
						new MemoryData[0],
						(CPUBuilder expected) => expected
							.ZeroFlag(true)
							.SubtractFlag(false)
							.HalfCarryFlag(true),
					};
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, oneInJustThatBit),
						new MemoryData[0],
						(CPUBuilder expected) => expected
							.ZeroFlag(false)
							.SubtractFlag(false)
							.HalfCarryFlag(true),
					};
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_40_through_7f_memory))]
	public void Instruction_prefix_40_through_7f_memory(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_40_through_7f_memory
	{
		get
		{
			var instructionOffset = 0x06;
			foreach (var (bit, oneInJustThatBit, instructionBase) in new (int, byte, int)[] {
				(0, 0b0000_0001, 0x40),
				(1, 0b0000_0010, 0x48),
				(2, 0b0000_0100, 0x50),
				(3, 0b0000_1000, 0x58),
				(4, 0b0001_0000, 0x60),
				(5, 0b0010_0000, 0x68),
				(6, 0b0100_0000, 0x70),
				(7, 0b1000_0000, 0x78),
			})
			{
				var instruction = (byte)(instructionBase + instructionOffset);
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { 0b0000_0000 }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[0],
					(CPUBuilder expected) => expected
						.ZeroFlag(true)
						.SubtractFlag(false)
						.HalfCarryFlag(true),
				};
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { oneInJustThatBit }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[0],
					(CPUBuilder expected) => expected
						.ZeroFlag(false)
						.SubtractFlag(false)
						.HalfCarryFlag(true),
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_80_through_bf_registers))]
	public void Instruction_prefix_80_through_bf_registers(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_80_through_bf_registers
	{
		get
		{
			foreach (var (bit, oneInAllBitsButThat, instructionBase) in new (int, byte, int)[] {
				(0, 0b1111_1110, 0x80),
				(1, 0b1111_1101, 0x88),
				(2, 0b1111_1011, 0x90),
				(3, 0b1111_0111, 0x98),
				(4, 0b1110_1111, 0xa0),
				(5, 0b1101_1111, 0xa8),
				(6, 0b1011_1111, 0xb0),
				(7, 0b0111_1111, 0xb8),
			})
			{
				foreach (var (instructionOffset, setter) in new (int, Func<CPUBuilder, byte, CPUBuilder>)[] {
					(0x00, (builder, x) => builder.RegisterB(x)),
					(0x01, (builder, x) => builder.RegisterC(x)),
					(0x02, (builder, x) => builder.RegisterD(x)),
					(0x03, (builder, x) => builder.RegisterE(x)),
					(0x04, (builder, x) => builder.RegisterH(x)),
					(0x05, (builder, x) => builder.RegisterL(x)),
					(0x07, (builder, x) => builder.RegisterA(x)),
				})
				{
					var instruction = (byte)(instructionBase + instructionOffset);
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, 0b0000_0000),
						new MemoryData[0],
						(CPUBuilder expected) => setter(expected, 0b0000_0000),
					};
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, 0b1111_1111),
						new MemoryData[0],
						(CPUBuilder expected) => setter(expected, oneInAllBitsButThat),
					};
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_80_through_bf_memory))]
	public void Instruction_prefix_80_through_bf_memory(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_80_through_bf_memory
	{
		get
		{
			var instructionOffset = 0x06;
			foreach (var (bit, oneInAllBitsButThat, instructionBase) in new (int, byte, int)[] {
				(0, 0b1111_1110, 0x80),
				(1, 0b1111_1101, 0x88),
				(2, 0b1111_1011, 0x90),
				(3, 0b1111_0111, 0x98),
				(4, 0b1110_1111, 0xa0),
				(5, 0b1101_1111, 0xa8),
				(6, 0b1011_1111, 0xb0),
				(7, 0b0111_1111, 0xb8),
			})
			{
				var instruction = (byte)(instructionBase + instructionOffset);
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { 0b0000_0000 }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[] {
						new(0x1234, new byte[] { 0b0000_0000 }),
					},
					(CPUBuilder expected) => expected,
				};
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { 0b1111_1111 }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[] {
						new(0x1234, new byte[] { oneInAllBitsButThat }),
					},
					(CPUBuilder expected) => expected,
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_c0_through_ff_registers))]
	public void Instruction_prefix_c0_through_ff_registers(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(8)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_c0_through_ff_registers
	{
		get
		{
			foreach (var (bit, oneInJustThatBit, instructionBase) in new (int, byte, int)[] {
				(0, 0b0000_0001, 0xc0),
				(1, 0b0000_0010, 0xc8),
				(2, 0b0000_0100, 0xd0),
				(3, 0b0000_1000, 0xd8),
				(4, 0b0001_0000, 0xe0),
				(5, 0b0010_0000, 0xe8),
				(6, 0b0100_0000, 0xf0),
				(7, 0b1000_0000, 0xf8),
			})
			{
				foreach (var (instructionOffset, setter) in new (int, Func<CPUBuilder, byte, CPUBuilder>)[] {
					(0x00, (builder, x) => builder.RegisterB(x)),
					(0x01, (builder, x) => builder.RegisterC(x)),
					(0x02, (builder, x) => builder.RegisterD(x)),
					(0x03, (builder, x) => builder.RegisterE(x)),
					(0x04, (builder, x) => builder.RegisterH(x)),
					(0x05, (builder, x) => builder.RegisterL(x)),
					(0x07, (builder, x) => builder.RegisterA(x)),
				})
				{
					var instruction = (byte)(instructionBase + instructionOffset);
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, 0b0000_0000),
						new MemoryData[0],
						(CPUBuilder expected) => setter(expected, oneInJustThatBit),
					};
					yield return new object?[] {
						new MemoryData[] { new(CPU.InitialPC, new byte[] { 0xcb, instruction }), },
						(CPUBuilder actual) => setter(actual, 0b1111_1111),
						new MemoryData[0],
						(CPUBuilder expected) => setter(expected, 0b1111_1111),
					};
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_prefix_c0_through_ff_memory))]
	public void Instruction_prefix_c0_through_ff_memory(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		PerformTest(
			actualMemory,
			actualBuilder,
			expectedMemory,
			expected => expectedBuilder(expected)
				.AddClock(16)
				.AddPC(2)
		);
	}

	public static IEnumerable<object?[]> InstructionData_prefix_c0_through_ff_memory
	{
		get
		{
			var instructionOffset = 0x06;
			foreach (var (bit, oneInJustThatBit, instructionBase) in new (int, byte, int)[] {
				(0, 0b0000_0001, 0xc0),
				(1, 0b0000_0010, 0xc8),
				(2, 0b0000_0100, 0xd0),
				(3, 0b0000_1000, 0xd8),
				(4, 0b0001_0000, 0xe0),
				(5, 0b0010_0000, 0xe8),
				(6, 0b0100_0000, 0xf0),
				(7, 0b1000_0000, 0xf8),
			})
			{
				var instruction = (byte)(instructionBase + instructionOffset);
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { 0b0000_0000 }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[] {
						new(0x1234, new byte[] { oneInJustThatBit }),
					},
					(CPUBuilder expected) => expected,
				};
				yield return new object?[] {
					new MemoryData[] {
						new(CPU.InitialPC, new byte[] { 0xcb, instruction }),
						new(0x1234, new byte[] { 0b1111_1111 }),
					},
					(CPUBuilder actual) => actual
						.RegisterHL(0x1234),
					new MemoryData[] {
						new(0x1234, new byte[] { 0b1111_1111 }),
					},
					(CPUBuilder expected) => expected,
				};
			}
		}
	}

	private void PerformTest(
		MemoryData[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		MemoryData[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder
	)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		foreach (var actualMemoryValue in actualMemory)
		{
			actualMemoryValue.WriteTo(memory);
		}
		var actual = actualBuilder(new CPUBuilder(loggerFactory, memory)).CPU;
		var expected = expectedBuilder(new CPUBuilder(loggerFactory, memory).Copy(actual)).CPU;
		actual.Step();
		AssertEqual(expected, actual);
		foreach (var expectedMemoryValue in expectedMemory)
		{
			expectedMemoryValue.AssertEquals(memory);
		}
	}

	private void AssertEqual(CPU expected, CPU actual)
	{
		// put flags first so the error message shows that instead of just register F being wrong
		AssertEqual("zero flag", expected.ZeroFlag, actual.ZeroFlag);
		AssertEqual("subtract flag", expected.SubtractFlag, actual.SubtractFlag);
		AssertEqual("half carry flag", expected.HalfCarryFlag, actual.HalfCarryFlag);
		AssertEqual("carry flag", expected.CarryFlag, actual.CarryFlag);
		AssertEqual("register A", expected.RegisterA, actual.RegisterA);
		AssertEqual("register B", expected.RegisterB, actual.RegisterB);
		AssertEqual("register C", expected.RegisterC, actual.RegisterC);
		AssertEqual("register D", expected.RegisterD, actual.RegisterD);
		AssertEqual("register E", expected.RegisterE, actual.RegisterE);
		AssertEqual("register F", expected.RegisterF, actual.RegisterF);
		AssertEqual("register H", expected.RegisterH, actual.RegisterH);
		AssertEqual("register L", expected.RegisterL, actual.RegisterL);
		AssertEqual("register SP", expected.RegisterSP, actual.RegisterSP);
		AssertEqual("register PC", expected.RegisterPC, actual.RegisterPC);
		AssertEqual("register AF", expected.RegisterAF, actual.RegisterAF);
		AssertEqual("register BC", expected.RegisterBC, actual.RegisterBC);
		AssertEqual("register DE", expected.RegisterDE, actual.RegisterDE);
		AssertEqual("register HL", expected.RegisterHL, actual.RegisterHL);
		AssertEqual("clock", expected.Clock, actual.Clock);
		AssertEqual("is halted", expected.IsHalted, actual.IsHalted);
		AssertEqual("is stopped", expected.IsStopped, actual.IsStopped);
		AssertEqual("interrupts enabled", expected.InterruptsEnabled, actual.InterruptsEnabled);
	}

	private void AssertEqual<T>(string name, T expected, T actual) where T : IEquatable<T>
	{
		Assert.True(expected.Equals(actual), $"{name} should be {expected} was {actual}");
	}
}