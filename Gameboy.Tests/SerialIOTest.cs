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
}