namespace Gameboy.Tests.TestROMs;

public class CPUInstructions
{
	[Fact]
	public async Task Combined()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/cpu_instrs.gb",
			TimeSpan.FromSeconds(54),
			TimeSpan.FromSeconds(60),
			"""
		cpu_instrs

		01:ok  02:ok  03:ok  04:ok  05:ok  06:ok  07:ok  08:ok  09:ok  10:ok  11:ok  

		Passed all tests

		""",
			"40c12bfd4b43f52dd3064efc7baf8919fb52cc79"
		);
	}

	[Fact]
	public async Task _01_Special()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/01-special.gb",
			TimeSpan.FromSeconds(3),
			TimeSpan.FromSeconds(5),
			"""
			01-special


			Passed

			""",
			"8b057dcd972f7c6e35e79a2b222a9093f6b79ce3"
		);
	}

	[Fact]
	public async Task _02_Interrupts()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/02-interrupts.gb",
			TimeSpan.FromSeconds(0.5),
			TimeSpan.FromSeconds(1),
			"""
			02-interrupts


			Passed

			""",
			"fb4e299f9de0c1715294921a9b8c7a27bf26cfa9"
		);
	}

	[Fact]
	public async Task _03_Op_Sp_Hl()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/03-op sp,hl.gb",
			TimeSpan.FromSeconds(3),
			TimeSpan.FromSeconds(5),
			"""
			03-op sp,hl


			Passed

			""",
			"11a6c910d32b716d7c206803e3dba322ce27bb19"
		);
	}

	[Fact]
	public async Task _04_Op_R_Imm()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/04-op r,imm.gb",
			TimeSpan.FromSeconds(3),
			TimeSpan.FromSeconds(5),
			"""
			04-op r,imm


			Passed

			""",
			"b34bf3f5a1c760f1d6d68630013a89780b9e62c0"
		);
	}

	[Fact]
	public async Task _05_Op_Rp()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/05-op rp.gb",
			TimeSpan.FromSeconds(4),
			TimeSpan.FromSeconds(6),
			"""
			05-op rp


			Passed

			""",
			"5433d27ba25466e0cd28737952583bb22abb452b"
		);
	}

	[Fact]
	public async Task _06_Ld_R_R()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/06-ld r,r.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			06-ld r,r


			Passed

			""",
			"37f5c837cf92d9c54484c29a9e76e0421aefe028"
		);
	}

	[Fact]
	public async Task _07_Jr_Jp_Call_Ret_Rst()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			07-jr,jp,call,ret,rst


			Passed

			""",
			"f9fbdad51868369bf14c0130b809364cc7c0661f"
		);
	}

	[Fact]
	public async Task _08_Misc_Instrs()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/08-misc instrs.gb",
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			"""
			08-misc instrs


			Passed

			""",
			"a52a8526d022555a78fd01ad0cdbb3663a8eaaa3"
		);
	}

	[Fact]
	public async Task _09_Op_R_R()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/09-op r,r.gb",
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(12),
			"""
		09-op r,r


		Passed

		""",
			"d9597cd7b81dcaab8a51d0e5aceb8f41b82a9f60"
		);
	}

	[Fact]
	public async Task _10_Bit_Ops()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/10-bit ops.gb",
			TimeSpan.FromSeconds(14),
			TimeSpan.FromSeconds(16),
			"""
			10-bit ops


			Passed

			""",
			"57d1407fd63c1393d23e19ab4b1f0f92bfd05a78"
		);
	}

	[Fact]
	public async Task _11_Op_A_Hl()
	{
		await TestROMUtils.PerformTest(
			"gb-test-roms/cpu_instrs/individual/11-op a,(hl).gb",
			TimeSpan.FromSeconds(18),
			TimeSpan.FromSeconds(20),
			"""
			11-op a,(hl)


			Passed

			""",
			"544f97e7b323372e72c0313bfb4985f03d0967c5"
		);
	}
}