namespace Gameboy.Tests;

public class CPUTest
{
	public record MemoryData(UInt16 Address, byte[] Data)
	{
		public void WriteTo(IMemory destination)
		{
			destination.Write(Address, Data);
		}

		public void AssertEquals(IMemory actual)
		{
			Assert.Equal(Data, actual.Read(Address, Data.Length));
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
		Assert.Equal(6, cpu.RegisterF);
		Assert.Equal(7, cpu.RegisterH);
		Assert.Equal(8, cpu.RegisterL);
		Assert.Equal(0x0106, cpu.RegisterAF);
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
		Assert.Equal(0x22, cpu.RegisterF);
		Assert.Equal(0x77, cpu.RegisterH);
		Assert.Equal(0x88, cpu.RegisterL);
		Assert.Equal(0x1122, cpu.RegisterAF);
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(true),
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
					.HalfCarryFlag(false),
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
					.ZeroFlag(true)
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
					.ZeroFlag(true)
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
					.ZeroFlag(true)
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
					.ZeroFlag(true)
					.SubtractFlag(false)
					.HalfCarryFlag(false)
					.CarryFlag(false),
			};
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
		actual.ExecuteInstruction();
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
	}

	private void AssertEqual<T>(string name, T expected, T actual) where T : IEquatable<T>
	{
		Assert.True(expected.Equals(actual), $"{name} should be {expected} was {actual}");
	}
}