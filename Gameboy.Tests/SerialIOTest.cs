namespace Gameboy.Tests;

public class SerialIOTest
{
	[Fact]
	public void ObservingOutgoingBytes()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var serialIO = new SerialIO(loggerFactory);
		var outgoingByteStream = new MemoryStream();
		serialIO.DataAvailable += (value) =>
		{
			outgoingByteStream.WriteByte(value);
		};

		// bit 0 = 1 = internal clock
		// bit 7 = 1 = start transfer
		serialIO.RegisterSC = 0b1000_0001;
		// data byte
		serialIO.RegisterSB = 0b1010_1100;

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
		serialIO.RegisterSC = 0b1000_0001;
		serialIO.RegisterSB = 0b0001_1011;
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
		var serialIO = new SerialIO(loggerFactory);
		var outgoingByteStream = new MemoryStream();
		serialIO.DataAvailable += (value) =>
		{
			outgoingByteStream.WriteByte(value);
		};

		// bit 0 = 0 = external clock
		// bit 7 = 1 = start transfer
		serialIO.RegisterSC = 0b1000_0000;
		// data byte
		serialIO.RegisterSB = 0b1010_1100;

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
		var serialIO = new SerialIO(loggerFactory);
		var memory = MemoryUtils.CreateMemoryROM(loggerFactory, serialIO, new Timer(loggerFactory), new byte[0]);
		var cpu = new CPU(loggerFactory, memory);
		var interruptTriggered = false;
		serialIO.DataAvailable += (value) =>
		{
			interruptTriggered = true;
		};

		// bit 0 = 1 = internal clock
		// bit 7 = 1 = start transfer
		memory.WriteUInt8(Memory.IO_SC, 0b1000_0001);
		// data byte
		memory.WriteUInt8(Memory.IO_SB, 0b1010_1100);
		// enable interrupts
		memory.WriteUInt8(Memory.IO_IE, 0b0000_1000);

		// 8 ticks for each bit of the output, on the 8th tick it should emit the byte
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.False(interruptTriggered);
		cpu.Step();
		Assert.False(interruptTriggered);
		// should have executed 4 of 8 ticks worth of serial IO, and one NOP
		Assert.Equal((UInt64)4, serialIO.Clock);
		Assert.Equal((UInt64)4, cpu.Clock);
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.False(interruptTriggered);
		serialIO.Step();
		Assert.True(interruptTriggered);
		// flag has been set
		Assert.Equal(Memory.IF_MASK_SERIAL, memory.ReadUInt8(Memory.IO_IF));
		cpu.Step();
		// flag has been reset
		Assert.Equal(0b0000_0000, memory.ReadUInt8(Memory.IO_IF));
		// the full byte should have been output, and we've executed 2 NOP
		Assert.Equal((UInt64)8, serialIO.Clock);
		Assert.Equal((UInt64)8, cpu.Clock);
		// the interrupt handler + 1 NOP
		Assert.Equal(0x0059, cpu.RegisterPC);
	}
}