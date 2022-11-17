namespace Gameboy.Tests;

public class CPUTest
{
	[Fact]
	public void Registers()
	{
		var cpu = new CPU(new SimpleMemory());

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
}