namespace Gameboy.Tests;

public class CPUTest
{
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

	[Fact]
	public void Instruction_00()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		memory.Write(CPU.InitialPC, new byte[] { 0x00, });
		var actual = new CPU(loggerFactory, memory);
		var expected = new CPUBuilder(loggerFactory, memory)
			.Copy(actual)
			.AddClock(4)
			.AddPC(1)
			.CPU;
		actual.ExecuteInstruction();
		AssertEqual(expected, actual);
	}

	[Theory]
	[MemberData(nameof(InstructionData_01_11_21_31))]
	public void Instructions_01_11_21_31(byte[] instructions, Func<CPUBuilder, CPUBuilder> expectedBuilder)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		memory.Write(CPU.InitialPC, instructions);
		var actual = new CPU(loggerFactory, memory);
		var expected = expectedBuilder(
			new CPUBuilder(loggerFactory, memory)
				.Copy(actual)
				.AddClock(12)
				.AddPC(3)
			)
			.CPU;
		actual.ExecuteInstruction();
		AssertEqual(expected, actual);
	}

	public static IEnumerable<object[]> InstructionData_01_11_21_31
	{
		get
		{
			yield return new object[] {
				new byte[] { 0x01, 0x12, 0x34 },
				(CPUBuilder expected) => expected.RegisterBC(0x3412),
			};
			yield return new object[] {
				new byte[] { 0x11, 0x12, 0x34 },
				(CPUBuilder expected) => expected.RegisterDE(0x3412),
			};
			yield return new object[] {
				new byte[] { 0x21, 0x12, 0x34 },
				(CPUBuilder expected) => expected.RegisterHL(0x3412),
			};
			yield return new object[] {
				new byte[] { 0x31, 0x12, 0x34 },
				(CPUBuilder expected) => expected.RegisterSP(0x3412),
			};
		}
	}

	[Theory]
	[MemberData(nameof(InstructionData_02_12_22_32))]
	public void Instructions_02_12_22_32(
		(UInt16, byte[])[] actualMemory,
		Func<CPUBuilder, CPUBuilder> actualBuilder,
		(UInt16, byte[])[] expectedMemory,
		Func<CPUBuilder, CPUBuilder> expectedBuilder)
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		foreach (var (address, data) in actualMemory)
		{
			memory.Write(address, data);
		}
		var actual = actualBuilder(new CPUBuilder(loggerFactory, memory)).CPU;
		var expected = expectedBuilder(
			new CPUBuilder(loggerFactory, memory)
				.Copy(actual)
				.AddClock(8)
				.AddPC(1)
			)
			.CPU;
		actual.ExecuteInstruction();
		AssertEqual(expected, actual);
		foreach (var (address, data) in expectedMemory)
		{
			AssertMemoryEqual(address, data, memory);
		}
	}

	public static IEnumerable<object[]> InstructionData_02_12_22_32
	{
		get
		{
			yield return new object[] {
				new[] { (CPU.InitialPC, new byte[] { 0x02, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterBC(0x1234),
				new[] { ((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object[] {
				new[] { (CPU.InitialPC, new byte[] { 0x12, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterDE(0x1234),
				new[] { ((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected,
			};
			yield return new object[] {
				new[] { (CPU.InitialPC, new byte[] { 0x22, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterHL(0x1234),
				new[] { ((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterHL(0x1235),
			};
			yield return new object[] {
				new[] { (CPU.InitialPC, new byte[] { 0x32, }), },
				(CPUBuilder actual) => actual
					.RegisterA(0x42)
					.RegisterHL(0x1234),
				new[] { ((UInt16)0x1234, new byte[] { 0x42, }), },
				(CPUBuilder expected) => expected
					.RegisterA(0x42)
					.RegisterHL(0x1233),
			};
		}
	}

	private void AssertEqual(CPU expected, CPU actual)
	{
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
		AssertEqual("zero flag", expected.ZeroFlag, actual.ZeroFlag);
		AssertEqual("subtract flag", expected.SubtractFlag, actual.SubtractFlag);
		AssertEqual("half carry flag", expected.HalfCarryFlag, actual.HalfCarryFlag);
		AssertEqual("carry flag", expected.CarryFlag, actual.CarryFlag);
		AssertEqual("clock", expected.Clock, actual.Clock);
	}

	private void AssertEqual<T>(string name, T expected, T actual) where T : IEquatable<T>
	{
		Assert.True(expected.Equals(actual), $"{name} should be {expected} was {actual}");
	}

	private void AssertMemoryEqual(UInt16 address, byte[] expected, IMemory actual)
	{
		Assert.Equal(expected, actual.Read(address, expected.Length));
	}
}