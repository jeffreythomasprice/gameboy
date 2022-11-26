using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Video : ISteppable
{
	public delegate void SetMemoryRegionEnabledDelegate(bool enabled);

	/// <param name="y">in range 0 to ScreenHeight-1, inclusive</param>
	/// <param name="data">a color, each element is a 2-bit value, so the high 6 bits are unused</param>
	public delegate void ScanlineAvailableDelegate(int y, byte[] data);

	public delegate void VSyncDelegate();

	/*
	state machine:

	LY tracks the current scan line, with an extra 10 invisible scan lines outside the screen
	for LY=0; LY<=153; LY++ {
		mode = 00 = H blank, lasts 201 ticks
		mode = 10 = sprite attr memory is disabled, lasts 77 ticks
		mode = 11 = sprite and video is disabled, lasts 169 ticks
	}
	mode = 01 = V blank, lasts 1386 ticks, i.e. until this total operation has taken 70224 total ticks

	so at 60 FPS thats 70224 * 60 = 4213440 ticks per second
	*/

	private enum State
	{
		HBlank,
		SpriteAttrCopy,
		AllVideoMemCopy,
		VBlank
	}

	[StructLayout(LayoutKind.Explicit, Size = 16)]
	private struct Sprite
	{
		[FieldOffset(0)]
		public byte Y;

		[FieldOffset(1)]
		public byte X;

		[FieldOffset(2)]
		public byte TileIndex;

		[FieldOffset(3)]
		public byte Flags;
	}

	public const int ScreenWidth = 160;
	public const int ScreenHeight = 144;

	public event SetMemoryRegionEnabledDelegate? SetVideoMemoryEnabled;
	public event SetMemoryRegionEnabledDelegate? SetSpriteAttributeMemoryEnabled;

	/// <summary>
	/// Called when a scanline is done drawing.
	/// </summary>
	public event ScanlineAvailableDelegate? ScanlineAvailable;

	/// <summary>
	/// Called when the last scanline is done drawing.
	/// </summary>
	public event VSyncDelegate? VSync;

	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;

	private State state;
	private byte registerLY;
	private UInt64 ticksRemainingInCurrentState;

	public Video(ILoggerFactory loggerFactory, IMemory memory)
	{
		logger = loggerFactory.CreateLogger<Video>();
		this.memory = memory;
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		Clock = 0;

		state = State.VBlank;
		registerLY = 0;
		ticksRemainingInCurrentState = 0;
	}

	public void Step()
	{
		const double FramesPerSecond = 59.73;
		const UInt64 ClockTicksPerFrame = (UInt64)(CPU.ClockTicksPerSecond / FramesPerSecond);
		const UInt64 HBlankTime = 201;
		const UInt64 SpriteAttrCopyTime = 77;
		const UInt64 AllVideoMemCopyTime = 169;
		const UInt64 VBlankTime = ClockTicksPerFrame - (HBlankTime + SpriteAttrCopyTime + AllVideoMemCopyTime) * (ScreenHeight + 10);

		// advance time
		var advance = Math.Min(ticksRemainingInCurrentState, 4);
		Clock += advance;
		ticksRemainingInCurrentState -= advance;

		// advance state machine if enough time has passed
		if (ticksRemainingInCurrentState == 0)
		{
			// control registers
			var registerLCDC = memory.ReadUInt8(Memory.IO_LCDC);
			var registerSTAT = memory.ReadUInt8(Memory.IO_STAT);

			// color palettes
			var registerBGP = extractColorPalette(memory.ReadUInt8(Memory.IO_BGP));
			var registerOBP0 = extractColorPalette(memory.ReadUInt8(Memory.IO_OBP0));
			var registerOBP1 = extractColorPalette(memory.ReadUInt8(Memory.IO_OBP1));
			byte[] extractColorPalette(byte palette)
			{
				return new[] {
					(byte)((palette & 0b0000_0011) >> 0),
					(byte)((palette & 0b0000_1100) >> 2),
					(byte)((palette & 0b0011_0000) >> 4),
					(byte)((palette & 0b1100_0000) >> 6),
				};
			}

			switch (state)
			{
				case State.HBlank:
					// end of mode 00, transition to mode 10, copy sprite attributes
					state = State.SpriteAttrCopy;
					ticksRemainingInCurrentState = SpriteAttrCopyTime;
					logger.LogTrace($"state={state}, ticks={ticksRemainingInCurrentState}");
					setStatMode(0b10);
					triggerSTATInterruptIfMaskSet(0b0010_0000);
					disableSpriteAttributeMemory();
					break;

				case State.SpriteAttrCopy:
					// end of mode 10, transition to mode 11, copy video memory
					state = State.AllVideoMemCopy;
					ticksRemainingInCurrentState = AllVideoMemCopyTime;
					logger.LogTrace($"state={state}, ticks={ticksRemainingInCurrentState}");
					setStatMode(0b11);
					disableVideoMemory();
					break;

				case State.AllVideoMemCopy:
					// actually draw some stuff
					if (registerLY < ScreenHeight)
					{
						drawScanLine();
					}

					// end of mode 11, either transition to mode 00 for a new scan line, or to mode 01 V blank
					registerLY++;
					if (registerLY < ScreenHeight + 10)
					{
						// more scan lines remaining, back to mode 00, H blank
						state = State.HBlank;
						ticksRemainingInCurrentState = HBlankTime;
						setStatMode(0b00);
						triggerSTATInterruptIfMaskSet(0b0000_1000);
						logger.LogTrace($"state={state}, LY={registerLY}, ticks={ticksRemainingInCurrentState}");
					}
					else
					{
						// no scan lines remain, to mode 01, V blank
						state = State.VBlank;
						ticksRemainingInCurrentState = VBlankTime;
						logger.LogTrace($"state={state}, ticks={ticksRemainingInCurrentState}");
						setStatMode(0b10);
						triggerVBlankInterrupt();
						triggerSTATInterruptIfMaskSet(0b0001_0000);
						logger.LogTrace("emitting vsync");
						VSync?.Invoke();
					}
					triggerSTATInterruptBasedOnLYAndLYC();
					enableSpriteAttributeMemory();
					enableVideoMemory();
					break;

				case State.VBlank:
					logger.LogTrace("end V blank");
					state = State.HBlank;
					registerLY = 0;
					ticksRemainingInCurrentState = HBlankTime;
					logger.LogTrace($"state={state}, LY={registerLY}, ticks={ticksRemainingInCurrentState}");
					setStatMode(0b00);
					triggerSTATInterruptIfMaskSet(0b0000_1000);
					triggerSTATInterruptBasedOnLYAndLYC();
					break;
			}

			void triggerVBlankInterrupt()
			{
				memory.WriteUInt8(Memory.IO_IF, (byte)(memory.ReadUInt8(Memory.IO_IF) | Memory.IF_MASK_VBLANK));
			}

			void triggerLCDCInterrupt()
			{
				memory.WriteUInt8(Memory.IO_IF, (byte)(memory.ReadUInt8(Memory.IO_IF) | Memory.IF_MASK_LCDC));
			}

			void triggerSTATInterruptIfMaskSet(byte mask)
			{
				if ((registerSTAT & mask) != 0)
				{
					triggerLCDCInterrupt();
				}
			}

			void triggerSTATInterruptBasedOnLYAndLYC()
			{
				// trigger interrupt based on which scan line we're on
				if ((registerSTAT & 0b0100_0000) != 0)
				{
					var statConditionFlag = (registerSTAT & 0b0000_0100) != 0;
					var registerLYC = memory.ReadUInt8(Memory.IO_LYC);
					if (
						(statConditionFlag && registerLY == registerLYC) ||
						(!statConditionFlag && registerLY != registerLYC)
					)
					{
						triggerLCDCInterrupt();
					}
				}
			}

			void setStatMode(byte mode)
			{
				memory.WriteUInt8(Memory.IO_STAT, (byte)((registerSTAT & 0b1111_1100) | (mode & 0b0000_0011)));
			}

			void enableVideoMemory()
			{
				logger.LogTrace("enabling video memory");
				SetVideoMemoryEnabled?.Invoke(true);
			}

			void disableVideoMemory()
			{
				logger.LogTrace("disabling video memory");
				SetVideoMemoryEnabled?.Invoke(false);
			}

			void enableSpriteAttributeMemory()
			{
				logger.LogTrace("enabling sprite attribute memory");
				SetSpriteAttributeMemoryEnabled?.Invoke(true);
			}

			void disableSpriteAttributeMemory()
			{
				logger.LogTrace("disabling sprite attribute memory");
				SetSpriteAttributeMemoryEnabled?.Invoke(false);
			}

			void drawScanLine()
			{
				const int BackgroundAndWindowSizeInTiles = 32;
				const int TileSizeInPixels = 8;
				const int BackgroundAndWindowSizeInPixels = BackgroundAndWindowSizeInTiles * TileSizeInPixels;
				// the length of the background and window index arrays
				const int BackgroundAndWindowTileIndicesLengthInBytes = BackgroundAndWindowSizeInTiles * BackgroundAndWindowSizeInTiles;
				// tile data is 8x8 but 2 bits per pixel
				const int TileLengthInBytes = 16;
				// how many tiles are there in the block of data that makes up the tile data
				const int TileDataLengthInBytes = 256 * TileLengthInBytes;

				// TODO this method is full of reading big blocks of data, even when not needed, maybe only read the specific bytes required for a particular line

				/*
				unsigned indices, values from 0 to 255
				tile 0 is at 0x8000
				tile 255 is at 0x8ff0
				*/
				var tileData1 = memory.ReadArray(Memory.VIDEO_RAM_START, TileDataLengthInBytes);
				/*
				signed indices, values from -128 to 127
				tile -128 is at 0x8800
				tile 0 is at 0x9000
				tile 127 is at 0x97f0

				the two tile data do overlap
				*/
				var tileData2 = memory.ReadArray(Memory.VIDEO_RAM_START + 0x0800, TileDataLengthInBytes);

				// LCDC flags
				var backgroundAndWindowEnabled = (registerLCDC & 0b0000_0001) != 0;
				var spritesEnabled = (registerLCDC & 0b0000_0010) != 0;
				// true = sprites are drawn in 2s as 8x16, false = sprites are 8x8
				var spritesAreBig = (registerLCDC & 0b0000_0100) != 0;
				// where in video memory the indices of the background tiles are
				var backgroundTileIndicesAddress = (UInt16)((registerLCDC & 0b0000_1000) != 0 ? Memory.VIDEO_RAM_START + 0x1c00 : Memory.VIDEO_RAM_START + 0x1800);
				// where in video memory are the actual 8x8 graphics for background and window
				byte[] backgroundAndWindowTileData;
				bool backgroundAndWindowTileDataIsSigned;
				if ((registerLCDC & 0b0001_0000) != 0)
				{
					backgroundAndWindowTileData = tileData1;
					backgroundAndWindowTileDataIsSigned = false;
				}
				else
				{
					backgroundAndWindowTileData = tileData2;
					backgroundAndWindowTileDataIsSigned = true;
				}
				var windowEnabled = (registerLCDC & 0b0010_0000) != 0;
				// where in video memory the indices of the window tiles are
				var windowTileIndicesAddress = (UInt16)((registerLCDC & 0b0100_0000) != 0 ? Memory.VIDEO_RAM_START + 0x1c00 : Memory.VIDEO_RAM_START + 0x1800);
				// TODO respect LCDC bit 7, display enabled

				var backgroundTileIndices = memory.ReadArray(backgroundTileIndicesAddress, BackgroundAndWindowTileIndicesLengthInBytes);
				var windowTileIndices = memory.ReadArray(windowTileIndicesAddress, BackgroundAndWindowTileIndicesLengthInBytes);

				// background and window offsets
				var registerSCX = memory.ReadUInt8(Memory.IO_SCX);
				var registerSCY = memory.ReadUInt8(Memory.IO_SCY);
				var registerWX = memory.ReadUInt8(Memory.IO_WX);
				var registerWY = memory.ReadUInt8(Memory.IO_WY);

				var outputPixels = new byte[ScreenWidth];

				// first layer for background and window
				if (backgroundAndWindowEnabled)
				{
					drawTileMap(backgroundTileIndices, background: true);
					if (windowEnabled)
					{
						drawTileMap(windowTileIndices, background: true);
					}
				}

				// figure out what sprites are visible and draw the ones in between the two layers
				const int MaxSprites = 40;
				const int MaxVisibleSprites = 10;
				var visibleSprites = new List<Sprite>(capacity: MaxVisibleSprites);
				if (spritesEnabled)
				{
					// get all the sprite attributes
					var spriteBytes = memory.ReadArray(Memory.SPRITE_ATTRIBUTES_START, Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START + 1);
					var sprites = new List<Sprite>(capacity: MaxSprites);
					for (var i = 0; i < MaxSprites; i++)
					{
						var sprite = MemoryMarshal.AsRef<Sprite>(spriteBytes.AsSpan(i * 4, 4));
						if (sprite.X > 0 && sprite.X < ScreenWidth + 8 && sprite.Y > 0 && sprite.Y < ScreenHeight + 16)
						{
							sprites.Add(sprite);
						}
					}
					sprites.Sort((a, b) =>
					{
						// right to left, larger X goes first so that it's drawn underneath the larger X
						var delta = b.X - a.X;
						if (delta != 0)
						{
							return delta;
						}
						// break ties by drawing larger tile index first so that they're underneath the smaller tile index
						return b.TileIndex - a.TileIndex;
					});
					visibleSprites.AddRange(sprites.TakeLast(MaxVisibleSprites));

					foreach (var sprite in visibleSprites)
					{
						if ((sprite.Flags & 0b1000_0000) != 0)
						{
							drawSpriteLine(sprite);
						}
					}
				}

				// second layer for background and window
				if (backgroundAndWindowEnabled)
				{
					drawTileMap(backgroundTileIndices, background: false);
					if (windowEnabled)
					{
						drawTileMap(windowTileIndices, background: false);
					}
				}

				// sprites on top of second layer
				if (spritesEnabled)
				{
					foreach (var sprite in visibleSprites)
					{
						if ((sprite.Flags & 0b1000_0000) == 0)
						{
							drawSpriteLine(sprite);
						}
					}
				}

				logger.LogTrace($"emitting scanline pixels y={registerLY}");
				ScanlineAvailable?.Invoke(registerLY, outputPixels);

				void drawTileMap(byte[] tileIndices, bool background)
				{
					// in pixels on the device display, so 0 to 143
					var screenY = registerLY;
					// in pixels on the tile map, so 0 to 255
					var tileMapPixelY = (screenY + registerSCY) % BackgroundAndWindowSizeInPixels;
					// in tiles on the tile map, so 0 to 31
					var tileMapTileY = tileMapPixelY / TileSizeInPixels;
					// the pixel offset on that tile, so 0 to 7
					var tileY = tileMapPixelY - tileMapTileY * TileSizeInPixels;
					// iterate over device display in pixels, so 0 to 159
					for (var screenX = 0; screenX < ScreenWidth; screenX++)
					{
						// in pixels on the tile map, so 0 to 255
						var tileMapPixelX = (screenX + registerSCX) % BackgroundAndWindowSizeInPixels;
						// in tiles on the tile map, so 0 to 31
						var tileMapTileX = tileMapPixelX / TileSizeInPixels;
						// the pixel offset on that tile, so 0 to 7
						var tileX = tileMapPixelX - tileMapTileX * TileSizeInPixels;
						// the value from the tile map, an index into the tile data
						var tileIndex = tileIndices[tileMapTileX + tileMapTileY * BackgroundAndWindowSizeInTiles];
						// possibly adjusted for the one that has signed offsets
						if (backgroundAndWindowTileDataIsSigned)
						{
							tileIndex = (byte)((sbyte)tileIndex + 128);
						}
						// the index into the tile data for the row of pixels we're drawing
						var tileDataIndex = tileIndex * TileLengthInBytes + tileY * 2;
						// the two bytes that make up this row
						var tileDataLow = backgroundAndWindowTileData[tileDataIndex];
						var tileDataHigh = backgroundAndWindowTileData[tileDataIndex + 1];
						// the index into the palette for this pixel
						int colorShift = 7 - tileX;
						int colorMask = 1 << colorShift;
						var colorIndex = ((tileDataLow & colorMask) >> colorShift) | (((tileDataHigh & colorMask) >> colorShift) << 1);
						// if this pixel is one we want to draw
						if ((colorIndex == 0 && background) || (colorIndex != 0 && !background))
						{
							outputPixels[screenX] = registerBGP[colorIndex];
						}
					}
				}

				void drawSpriteLine(Sprite sprite)
				{
					// in pixels on the device display, so 0 to 143
					var screenY = registerLY;
					// actual sprite coordinates in pixels
					var spriteX = sprite.X - 8;
					var spriteY = sprite.Y - 16;
					// which tile to draw
					var tileIndex = sprite.TileIndex;
					if (spritesAreBig)
					{
						tileIndex = (byte)(tileIndex & 0b1111_1110);
					}
					// the pixel offset on the tile, so 0 to 7
					var tileY = screenY - spriteY;
					if (tileY >= 8)
					{
						tileY -= 8;
						// drawing the bottom half of a big sprite
						tileIndex++;
					}
					// iterate over device display in pixels, so 0 to 159
					for (var screenX = spriteX; screenX < spriteX + 8; screenX++)
					{
						// clip to screen
						if (screenX < 0 || screenX >= ScreenWidth)
						{
							continue;
						}
						// the pixel offset on that tile, so 0 to 7
						var tileX = screenX - spriteX;
						// the index into the tile data for the row of pixels we're drawing
						var tileDataIndex = tileIndex * TileLengthInBytes + tileY * 2;
						// the two bytes that make up this row
						var tileDataLow = backgroundAndWindowTileData[tileDataIndex];
						var tileDataHigh = backgroundAndWindowTileData[tileDataIndex + 1];
						// the index into the palette for this pixel
						int colorShift = 7 - tileX;
						int colorMask = 1 << colorShift;
						var colorIndex = ((tileDataLow & colorMask) >> colorShift) | (((tileDataHigh & colorMask) >> colorShift) << 1);
						// which palette are we using
						var palette = (sprite.Flags & 0b0001_0000) == 0 ? registerOBP0 : registerOBP1;
						// draw the pixel
						outputPixels[screenX] = palette[colorIndex];
					}
				}
			}
		}
	}

	public void ResetLY()
	{
		logger.LogTrace("LY reset");
		// docs are unclear about what this does to the video state machine, just says
		// "Writing will reset the counter."
		registerLY = 0;
	}
}