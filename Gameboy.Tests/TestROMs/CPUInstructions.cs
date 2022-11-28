namespace Gameboy.Tests.TestROMs;

public class CPUInstructions
{
	[Fact]
	public void Combined()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/cpu_instrs.gb",
			1000000000,
			"""
			cpu_instrs

			01:ok  02:ok  03:ok  04:ok  05:ok  06:ok  07:ok  08:ok  09:ok  10:ok  11:ok
			"""
		);
	}

	[Fact]
	public void _01_Special()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/01-special.gb",
			10000000,
			"""
			01-special


			Passed

			"""
		);
	}

	[Fact]
	public void _02_Interrupts()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/02-interrupts.gb",
			10000000,
			"""
			02-interrupts


			Passed

			"""
		);
	}

	[Fact]
	public void _03_Op_Sp_Hl()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/03-op sp,hl.gb",
			100000000,
			"""
			03-op sp,hl


			Passed

			"""
		);
	}

	[Fact]
	public void _04_Op_R_Imm()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/04-op r,imm.gb",
			100000000,
			"""
			04-op r,imm


			Passed

			"""
		);
	}

	[Fact]
	public void _05_Op_Rp()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/05-op rp.gb",
			100000000,
			"""
			05-op rp


			Passed

			"""
		);
	}

	[Fact]
	public void _06_Ld_R_R()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/06-ld r,r.gb",
			10000000,
			"""
			06-ld r,r


			Passed

			"""
		);
	}

	[Fact]
	public void _07_Jr_Jp_Call_Ret_Rst()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb",
			10000000,
			"""
			07-jr,jp,call,ret,rst


			Passed

			"""
		);
	}

	[Fact]
	public void _08_Misc_Instrs()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/08-misc instrs.gb",
			10000000,
			"""
			08-misc instrs


			Passed

			"""
		);
	}

	[Fact]
	public void _09_Op_R_R()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/09-op r,r.gb",
			100000000,
			"""
			09-op r,r


			Passed

			"""
		);
	}

	[Fact]
	public void _10_Bit_Ops()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/10-bit ops.gb",
			100000000,
			"""
			10-bit ops


			Passed

			"""
		);
	}

	[Fact]
	public void _11_Op_A_Hl()
	{
		TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/11-op a,(hl).gb",
			100000000,
			"""
			11-op a,(hl)


			Passed

			"""
		);
	}
}