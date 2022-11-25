namespace Gameboy.Tests;

public class SerialIOTest
{
	[Fact]
	public void ObservingOutgoingBytes()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var serialIO = new SerialIO(loggerFactory, memory);
		var outgoingByteStream = new MemoryStream();
		serialIO.DataAvailable += (value) =>
		{
			outgoingByteStream.WriteByte(value);
		};

		// bit 0 = 1 = internal clock
		// bit 7 = 1 = start transfer
		memory.WriteUInt8(Memory.IO_SC, 0b1000_0001);
		// data byte
		memory.WriteUInt8(Memory.IO_SB, 0b1010_1100);

		// 8 ticks for each bit of the output, on the 8th tick it should emit the byte
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		Assert.Equal(new byte[] { 0b1010_1100 }, outgoingByteStream.ToArray());

		// 2nd byte
		memory.WriteUInt8(Memory.IO_SC, 0b1000_0001);
		memory.WriteUInt8(Memory.IO_SB, 0b0001_1011);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(1, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(2, outgoingByteStream.Length);
		Assert.Equal(new byte[] { 0b1010_1100, 0b0001_1011 }, outgoingByteStream.ToArray());
	}

	[Fact]
	public void ExternalClockNotImplemented()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var serialIO = new SerialIO(loggerFactory, memory);
		var outgoingByteStream = new MemoryStream();
		serialIO.DataAvailable += (value) =>
		{
			outgoingByteStream.WriteByte(value);
		};

		// bit 0 = 0 = external clock
		// bit 7 = 1 = start transfer
		memory.WriteUInt8(Memory.IO_SC, 0b1000_0000);
		// data byte
		memory.WriteUInt8(Memory.IO_SB, 0b1010_1100);

		// 8 ticks for each bit of the output, on the 8th tick it should emit the byte
		// since we don't support external clock, it should just always stay at zero bytes
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
		serialIO.Step();
		Assert.Equal(0, outgoingByteStream.Length);
	}

	[Fact]
	public void Interrupt()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var memory = new SimpleMemory();
		var serialIO = new SerialIO(loggerFactory, memory);
		var cpu = new CPU(loggerFactory, memory);
		serialIO.DataAvailable += (value) =>
		{
			cpu.SerialIOCompleteInterrupt();
		};

		// bit 0 = 1 = internal clock
		// bit 7 = 1 = start transfer
		memory.WriteUInt8(Memory.IO_SC, 0b1000_0001);
		// data byte
		memory.WriteUInt8(Memory.IO_SB, 0b1010_1100);
		// enable interrupts
		memory.WriteUInt8(Memory.INTERRUPT_ENABLE_REGISTER, 0b0000_1000);

		// 8 ticks for each bit of the output, on the 8th tick it should emit the byte
		serialIO.Step();
		serialIO.Step();
		serialIO.Step();
		serialIO.Step();
		cpu.Step();
		// should have executed 4 of 8 ticks worth of serial IO, and one NOP
		Assert.Equal((UInt64)4, serialIO.Clock);
		Assert.Equal((UInt64)4, cpu.Clock);
		serialIO.Step();
		serialIO.Step();
		serialIO.Step();
		serialIO.Step();
		cpu.Step();
		// the full byte should have been output, and we've executed 2 NOP
		Assert.Equal((UInt64)8, serialIO.Clock);
		Assert.Equal((UInt64)8, cpu.Clock);
		// the interrupt handler + 1 NOP
		Assert.Equal(0x0059, cpu.RegisterPC);
	}
}