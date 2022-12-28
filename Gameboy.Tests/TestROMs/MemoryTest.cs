namespace Gameboy.Tests;

public class MemoryTest
{
	[Fact]
	public void EnableAndDisableRegions()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var logger = loggerFactory.CreateLogger(GetType().FullName!);
		using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
		var cartridge = new Cartridge(stream);
		var serialIO = new SerialIO(loggerFactory);
		var timer = new Timer(loggerFactory);
		var video = new RGBVideo(loggerFactory);
		var sound = new Sound(loggerFactory);
		var keypad = new Keypad(loggerFactory);
		var interruptRegisters = new InterruptRegisters(serialIO, timer, video, sound, keypad);
		var memory = cartridge.CreateMemory(loggerFactory, serialIO, timer, video, sound, keypad, interruptRegisters);

		// test data for when reads are disabled
		var expectedWhenDisabled = CreateUniformArray(160, 0xff);

		Assert.True(video.VideoDataWriteEnabled);
		Assert.True(video.SpriteAttributesDataWriteEnabled);

		var videoData1 = CreateIncrementingArray(Memory.VIDEO_RAM_END - Memory.VIDEO_RAM_START, 0x11);
		var spriteAttributeData1 = CreateIncrementingArray(Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START, 0x22);
		memory.WriteArray(Memory.VIDEO_RAM_START, videoData1);
		AssertEqual(memory, Memory.VIDEO_RAM_START, videoData1);
		memory.WriteArray(Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData1);
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData1);

		video.VideoDataWriteEnabled = false;

		var videoData2 = CreateIncrementingArray(Memory.VIDEO_RAM_END - Memory.VIDEO_RAM_START, 0x33);
		var spriteAttributeData2 = CreateIncrementingArray(Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START, 0x44);
		memory.WriteArray(Memory.VIDEO_RAM_START, videoData2);
		AssertEqual(memory, Memory.VIDEO_RAM_START, expectedWhenDisabled);
		memory.WriteArray(Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData2);
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData2);

		video.VideoDataWriteEnabled = true;
		video.SpriteAttributesDataWriteEnabled = false;

		var videoData3 = CreateIncrementingArray(Memory.VIDEO_RAM_END - Memory.VIDEO_RAM_START, 0x55);
		var spriteAttributeData3 = CreateIncrementingArray(Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START, 0x66);
		memory.WriteArray(Memory.VIDEO_RAM_START, videoData3);
		AssertEqual(memory, Memory.VIDEO_RAM_START, videoData3);
		memory.WriteArray(Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData3);
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, expectedWhenDisabled);

		video.Reset();
		Assert.True(video.VideoDataWriteEnabled);
		Assert.True(video.SpriteAttributesDataWriteEnabled);
	}

	[Fact]
	public void DMA()
	{
		using var loggerFactory = LoggerUtils.CreateLoggerFactory();
		var logger = loggerFactory.CreateLogger(GetType().FullName!);
		using var stream = new FileStream("gb-test-roms/cpu_instrs/cpu_instrs.gb", FileMode.Open);
		var cartridge = new Cartridge(stream);
		var serialIO = new SerialIO(loggerFactory);
		var timer = new Timer(loggerFactory);
		var video = new RGBVideo(loggerFactory);
		var sound = new Sound(loggerFactory);
		var keypad = new Keypad(loggerFactory);
		var interruptRegisters = new InterruptRegisters(serialIO, timer, video, sound, keypad);
		var memory = cartridge.CreateMemory(loggerFactory, serialIO, timer, video, sound, keypad, interruptRegisters);

		// intentionally longer than the DMA length, so we can prove it cuts off there
		var data1 = CreateIncrementingArray(200, 0x11);
		// start of internal RAM
		memory.WriteArray(0xc000, data1);
		memory.WriteUInt8(Memory.IO_DMA, 0xc0);
		Assert.Equal(0, memory.ReadUInt8(Memory.IO_DMA));
		for (var i = 0; i < 200; i++)
		{
			memory.Step();
		}
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, data1.Take(160).ToArray());

		// same thing, but also disable sprite attribute memory to prove DMA works while that's off
		video.SpriteAttributesDataWriteEnabled = false;
		var data2 = CreateIncrementingArray(200, 0x22);
		// another location in internal RAM
		memory.WriteArray(0xd100, data2);
		memory.WriteUInt8(Memory.IO_DMA, 0xd1);
		Assert.Equal(0, memory.ReadUInt8(Memory.IO_DMA));
		for (var i = 0; i < 200; i++)
		{
			memory.Step();
		}
		video.SpriteAttributesDataWriteEnabled = true;
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, data2.Take(160).ToArray());

		// but now do a weird address
		var data3 = CreateIncrementingArray(200, 0x33);
		// address in internal RAM that doesn't divide evenly by 0x100, so we get cut off at the beginning
		memory.WriteArray(0xc9f0, data3);
		memory.WriteUInt8(Memory.IO_DMA, 0xca);
		Assert.Equal(0, memory.ReadUInt8(Memory.IO_DMA));
		for (var i = 0; i < 200; i++)
		{
			memory.Step();
		}
		AssertEqual(memory, Memory.SPRITE_ATTRIBUTES_START, data3.Skip(16).Take(160).ToArray());
	}

	private byte[] CreateUniformArray(int length, byte value)
	{
		var result = new byte[length];
		for (var i = 0; i < length; i++)
		{
			result[i] = value;
		}
		return result;
	}

	private byte[] CreateIncrementingArray(int length, byte startingValue)
	{
		var result = new byte[length];
		byte value = startingValue;
		for (var i = 0; i < length; i++)
		{
			result[i] = value;
			value++;
		}
		return result;
	}

	private void AssertEqual(IMemory memory, UInt16 address, byte[] data)
	{
		for (var i = 0; i < data.Length; i++, address++)
		{
			Assert.Equal(data[i], memory.ReadUInt8(address));
		}
	}
}