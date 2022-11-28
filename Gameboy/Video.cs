using System.Diagnostics;
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
	for LY=0; LY<=ScreenHeight; LY++ {
		mode = 00 = H blank, lasts 201 ticks
		mode = 10 = sprite attr memory is disabled, lasts 77 ticks
		mode = 11 = sprite and video is disabled, lasts 169 ticks
	}
	for LY=ScreenHeight; LY<=ScreenHeight + 9; LY++ {
		mode = 01 = V blank, lasts 585 ticks
	}
	last extra line line, LY = ScreenHeight + 10, mode = 01 = V blank, lasts 591 ticks

	total frame time is therefore 70224 total ticks
	4194304 ticks per second / 70224 ~= 59.73 FPS
	*/

	private enum State
	{
		HBlank,
		SpriteAttrCopy,
		AllVideoMemCopy,
		VBlank
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
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

		public int AdjustedX => X - 8;
		public int AdjustedY => Y - 16;

		public bool WrapX => (Flags & 0b0010_0000) != 0;
		public bool WrapY => (Flags & 0b0100_0000) != 0;
		public bool PaletteIsOBP0 => (Flags & 0b0001_0000) == 0;
		public bool DrawOnTopOfBackgroundAndWindow => (Flags & 0b1000_0000) == 0;
	}

	private struct ColorPalette
	{
		private byte registerValue;

		public ColorPalette(byte registerValue)
		{
			this.registerValue = registerValue;
		}

		public byte this[int index] =>
			index switch
			{
				0 => (byte)((registerValue & 0b0000_0011) >> 0),
				1 => (byte)((registerValue & 0b0000_1100) >> 2),
				2 => (byte)((registerValue & 0b0011_0000) >> 4),
				3 => (byte)((registerValue & 0b1100_0000) >> 6),
				_ => throw new ArgumentOutOfRangeException(nameof(index)),
			};
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


	// total time between screen redraws, including V blank mode
	private const UInt64 ClockTicksPerFrame = 70224;
	// how long it stays in each mode when drawing a real scan line
	private const UInt64 HBlankTime = 201;
	private const UInt64 SpriteAttrCopyTime = 77;
	private const UInt64 AllVideoMemCopyTime = 169;
	// how many extra values of LY during which it's in V blank mode
	private const int ExtraVBlankScanLines = 10;
	// how long it stays in V blank mode per "extra" scan line
	private const UInt64 VBlankTime = (ClockTicksPerFrame - (HBlankTime + SpriteAttrCopyTime + AllVideoMemCopyTime) * ScreenHeight) / ExtraVBlankScanLines;
	// doesn't divide evenly, so some extra time on the last one
	private const UInt64 VBlankTimeOnLastLine = VBlankTime + (ClockTicksPerFrame - (HBlankTime + SpriteAttrCopyTime + AllVideoMemCopyTime) * ScreenHeight - VBlankTime * ExtraVBlankScanLines);

	private const int BackgroundAndWindowSizeInTiles = 32;
	private const int TileSizeInPixels = 8;
	private const int BackgroundAndWindowSizeInPixels = BackgroundAndWindowSizeInTiles * TileSizeInPixels;
	// the length of the background and window index arrays
	private const int BackgroundAndWindowTileIndicesLengthInBytes = BackgroundAndWindowSizeInTiles * BackgroundAndWindowSizeInTiles;
	// tile data is 8x8 but 2 bits per pixel
	private const int TileLengthInBytes = 16;
	// how many tiles are there in the block of data that makes up the tile data
	// there are two overlapping tile data sections
	// there's a section of 128 tiles in one, then a section of 128 they share, then a section of 128 in the other
	private const int TileDataSegmentLength = 128 * TileLengthInBytes;
	private const int OneTileDataSectionLengthInBytes = TileDataSegmentLength * 2;
	private const int BothTileDataSectionsLengthInBytes = TileDataSegmentLength * 3;

	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;

	private State state;
	private UInt64 ticksRemainingInCurrentState;

	private bool expectedLYUpdate;

	// filled in during writes with copy of relevant part of video memory
	private readonly byte[] combinedTileData = new byte[BothTileDataSectionsLengthInBytes];
	private readonly byte[] backgroundTileIndices = new byte[BackgroundAndWindowTileIndicesLengthInBytes];
	private readonly byte[] windowTileIndices = new byte[BackgroundAndWindowTileIndicesLengthInBytes];
	private readonly byte[] spriteAttributeData = new byte[Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START + 1];

	// used during output of a scanline
	private readonly byte[] outputPixels = new byte[ScreenWidth];
	private readonly byte[] backgroundAndWindowColorIndex = new byte[ScreenWidth];

	// TODO JEFF timing debugging
	internal readonly Stopwatch TileDataReadStopwatch = new();
	internal readonly Stopwatch BackgroundAndWindowStopwatch = new();
	internal readonly Stopwatch SpritesStopwatch = new();
	internal readonly Stopwatch EmitScanlineStopwatch = new();

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
		RegisterLY = 0;
		ticksRemainingInCurrentState = 0;

		TileDataReadStopwatch.Reset();
	}

	public void Step()
	{
		// advance time
		var advance = Math.Min(ticksRemainingInCurrentState, 4);
		Clock += advance;
		ticksRemainingInCurrentState -= advance;

		// advance state machine if enough time has passed
		if (ticksRemainingInCurrentState == 0)
		{
			switch (state)
			{
				case State.HBlank:
					// end of mode 00, transition to mode 10, copy sprite attributes
					state = State.SpriteAttrCopy;
					ticksRemainingInCurrentState = SpriteAttrCopyTime;
#if DEBUG
					logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
					setStatMode(0b10);
					triggerSTATInterruptIfMaskSet(0b0010_0000);
					disableSpriteAttributeMemory();
					break;

				case State.SpriteAttrCopy:
					// end of mode 10, transition to mode 11, copy video memory
					state = State.AllVideoMemCopy;
					ticksRemainingInCurrentState = AllVideoMemCopyTime;
#if DEBUG
					logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
					setStatMode(0b11);
					disableVideoMemory();
					break;

				case State.AllVideoMemCopy:
					// actually draw some stuff
					if (RegisterLY < ScreenHeight)
					{
						drawScanLine();
					}
					// end of mode 11, either transition to mode 00 for a new scan line, or to mode 01 V blank
					RegisterLY++;
					if (RegisterLY < ScreenHeight)
					{
						// more scan lines remaining, back to mode 00, H blank
						state = State.HBlank;
						ticksRemainingInCurrentState = HBlankTime;
						setStatMode(0b00);
						triggerSTATInterruptIfMaskSet(0b0000_1000);
#if DEBUG
						logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
					}
					else
					{
						// no scan lines remain, to mode 01, V blank
						state = State.VBlank;
						ticksRemainingInCurrentState = VBlankTime;
#if DEBUG
						logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
						setStatMode(0b01);
						triggerVBlankInterrupt();
						triggerSTATInterruptIfMaskSet(0b0001_0000);
#if DEBUG
						logger.LogTrace("emitting vsync");
#endif
						VSync?.Invoke();
					}
					triggerSTATInterruptBasedOnLYAndLYC();
					enableSpriteAttributeMemory();
					enableVideoMemory();
					break;

				case State.VBlank:
					RegisterLY++;
					if (RegisterLY < ScreenHeight + ExtraVBlankScanLines)
					{
#if DEBUG
						logger.LogTrace("stay in V blank");
#endif
						if (RegisterLY == ScreenHeight + ExtraVBlankScanLines - 1)
						{
							ticksRemainingInCurrentState = VBlankTimeOnLastLine;
						}
						else
						{
							ticksRemainingInCurrentState = VBlankTime;
						}
#if DEBUG
						logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
					}
					else
					{
#if DEBUG
						logger.LogTrace("end V blank");
#endif
						state = State.HBlank;
						RegisterLY = 0;
						ticksRemainingInCurrentState = HBlankTime;
#if DEBUG
						logger.LogTrace($"state={state}, LY={RegisterLY}, ticks={ticksRemainingInCurrentState}");
#endif
						setStatMode(0b00);
						triggerSTATInterruptIfMaskSet(0b0000_1000);
						triggerSTATInterruptBasedOnLYAndLYC();
					}
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
				if ((RegisterSTAT & mask) != 0)
				{
					triggerLCDCInterrupt();
				}
			}

			void triggerSTATInterruptBasedOnLYAndLYC()
			{
				// trigger interrupt based on which scan line we're on
				if ((RegisterSTAT & 0b0100_0000) != 0)
				{
					var statConditionFlag = (RegisterSTAT & 0b0000_0100) != 0;
					if (
						(statConditionFlag && RegisterLY == RegisterLYC) ||
						(!statConditionFlag && RegisterLY != RegisterLYC)
					)
					{
						triggerLCDCInterrupt();
					}
				}
			}

			void setStatMode(byte mode)
			{
				RegisterSTAT = (byte)((RegisterSTAT & 0b1111_1100) | (mode & 0b0000_0011));
			}

			// TODO I'm wrong about how memory gets disabled during video hardware states
			// actually disabling video memory has test programs just randomly fail to write stuff, implying that either
			// 1. they're not checking the STAT bits for what mode
			// 2. I'm wrong about what they should be checking for what mode so they don't think writes are unsafe

			void enableVideoMemory()
			{
#if DEBUG
				logger.LogTrace("enabling video memory");
#endif
				// SetVideoMemoryEnabled?.Invoke(true);
			}

			void disableVideoMemory()
			{
#if DEBUG
				logger.LogTrace("disabling video memory");
#endif
				// SetVideoMemoryEnabled?.Invoke(false);
			}

			void enableSpriteAttributeMemory()
			{
#if DEBUG
				logger.LogTrace("enabling sprite attribute memory");
#endif
				// SetSpriteAttributeMemoryEnabled?.Invoke(true);
			}

			void disableSpriteAttributeMemory()
			{
#if DEBUG
				logger.LogTrace("disabling sprite attribute memory");
#endif
				// SetSpriteAttributeMemoryEnabled?.Invoke(false);
			}

			void drawScanLine()
			{
				TileDataReadStopwatch.Start();
				memory.ReadArray(combinedTileData, 0, Memory.VIDEO_RAM_START, combinedTileData.Length);
				/*
				unsigned indices, values from 0 to 255
				tile 0 is at 0x8000
				tile 255 is at 0x8ff0
				*/
				var tileData1 = combinedTileData.AsSpan(0, OneTileDataSectionLengthInBytes);
				/*
				signed indices, values from -128 to 127
				tile -128 is at 0x8800
				tile 0 is at 0x9000
				tile 127 is at 0x97f0

				the two tile data do overlap
				*/
				var tileData2 = combinedTileData.AsSpan(TileDataSegmentLength, OneTileDataSectionLengthInBytes);
				TileDataReadStopwatch.Stop();

				// LCDC flags
				var backgroundAndWindowEnabled = (RegisterLCDC & 0b0000_0001) != 0;
				var spritesEnabled = (RegisterLCDC & 0b0000_0010) != 0;
				// true = sprites are drawn in 2s as 8x16, false = sprites are 8x8
				var spritesAreBig = (RegisterLCDC & 0b0000_0100) != 0;
				// where in video memory the indices of the background tiles are
				var backgroundTileIndicesAddress = (UInt16)((RegisterLCDC & 0b0000_1000) != 0 ? Memory.VIDEO_RAM_START + 0x1c00 : Memory.VIDEO_RAM_START + 0x1800);
				// where in video memory are the actual 8x8 graphics for background and window
				Span<byte> backgroundAndWindowTileData;
				bool backgroundAndWindowTileDataIsSigned;
				if ((RegisterLCDC & 0b0001_0000) != 0)
				{
					backgroundAndWindowTileData = tileData1;
					backgroundAndWindowTileDataIsSigned = false;
				}
				else
				{
					backgroundAndWindowTileData = tileData2;
					backgroundAndWindowTileDataIsSigned = true;
				}
				var windowEnabled = (RegisterLCDC & 0b0010_0000) != 0;
				// where in video memory the indices of the window tiles are
				var windowTileIndicesAddress = (UInt16)((RegisterLCDC & 0b0100_0000) != 0 ? Memory.VIDEO_RAM_START + 0x1c00 : Memory.VIDEO_RAM_START + 0x1800);
				// TODO respect LCDC bit 7, display enabled

				BackgroundAndWindowStopwatch.Start();
				if (backgroundAndWindowEnabled)
				{
					memory.ReadArray(backgroundTileIndices, 0, backgroundTileIndicesAddress, backgroundTileIndices.Length);
					drawTileMap(
						tileData: backgroundAndWindowTileData,
						tileIndices: backgroundTileIndices,
						scrollX: RegisterSCX,
						scrollY: RegisterSCY,
						wrap: true
					);
					if (windowEnabled)
					{
						// TODO window addr might be same as background, in which case we should reuse the same data
						memory.ReadArray(windowTileIndices, 0, windowTileIndicesAddress, windowTileIndices.Length);
						drawTileMap(
							tileData: backgroundAndWindowTileData,
							tileIndices: windowTileIndices,
							scrollX: -(RegisterWX - 7),
							scrollY: -RegisterWY,
							wrap: false
						);
					}
				}
				else
				{
					// background disabled, so init the output arrays so sprites can always draw on top correctly
					for (var i = 0; i < ScreenWidth; i++)
					{
						outputPixels[i] = 0;
						backgroundAndWindowColorIndex[i] = 0;
					}
				}
				BackgroundAndWindowStopwatch.Stop();

				SpritesStopwatch.Start();
				const int MaxSprites = 40;
				const int MaxVisibleSprites = 10;
				var spriteHeight = spritesAreBig ? TileSizeInPixels * 2 : TileSizeInPixels;
				var visibleSprites = new List<Sprite>(capacity: MaxVisibleSprites);
				if (spritesEnabled)
				{
					// get all the sprite attributes
					memory.ReadArray(spriteAttributeData, 0, Memory.SPRITE_ATTRIBUTES_START, spriteAttributeData.Length);
					var sprites = new List<Sprite>(capacity: MaxSprites);
					for (var i = 0; i < MaxSprites; i++)
					{
						var sprite = MemoryMarshal.AsRef<Sprite>(spriteAttributeData.AsSpan(i * 4, 4));
						if (sprite.X > 0 && sprite.X < ScreenWidth + 8 && sprite.Y > 0 && RegisterLY >= sprite.AdjustedY && RegisterLY < sprite.AdjustedY + spriteHeight)
						{
							sprites.Add(sprite);
						}
					}
					/*
					sprite priority:
					- sprites to the left are on top
					- resolve ties, sprites with lower tile indices are on top

					so sort in ascending order by x, break ties by ascending order by tile
					if too many, drop at from the end of the list
					draw from back of list to front
					*/
					sprites.Sort((a, b) =>
					{
						var delta = a.X - b.X;
						if (delta != 0)
						{
							return delta;
						}
						return a.TileIndex - b.TileIndex;
					});
					visibleSprites.AddRange(sprites.Take(MaxVisibleSprites));
					visibleSprites.Reverse();

					foreach (var sprite in visibleSprites)
					{
						drawSpriteLine(tileData1, sprite);
					}
				}
				SpritesStopwatch.Stop();

				EmitScanlineStopwatch.Start();
#if DEBUG
				logger.LogTrace($"emitting scanline pixels y={RegisterLY}");
#endif
				ScanlineAvailable?.Invoke(RegisterLY, outputPixels);
				EmitScanlineStopwatch.Stop();

				void drawTileMap(Span<byte> tileData, byte[] tileIndices, int scrollX, int scrollY, bool wrap)
				{
					// in pixels on the device display, so 0 to 143
					var screenY = RegisterLY;
					// in pixels on the tile map, so 0 to 255
					var tileMapPixelY = (screenY + scrollY);
					if (wrap)
					{
						// drawing the background, which wraps around
						tileMapPixelY %= BackgroundAndWindowSizeInPixels;
					}
					else if (tileMapPixelY < 0 || tileMapPixelY >= BackgroundAndWindowSizeInPixels)
					{
						// drawing the window which does not wrap, and we're out of bounds so nothing to draw
						return;
					}
					// in tiles on the tile map, so 0 to 31
					var tileMapTileY = tileMapPixelY / TileSizeInPixels;
					// the pixel offset on that tile, so 0 to 7
					var tileY = tileMapPixelY - tileMapTileY * TileSizeInPixels;
					// iterate over device display in pixels, so 0 to 159
					for (var screenX = 0; screenX < ScreenWidth; screenX++)
					{
						// in pixels on the tile map, so 0 to 255
						var tileMapPixelX = (screenX + scrollX);
						if (wrap)
						{
							// drawing the background, which wraps around
							tileMapPixelX %= BackgroundAndWindowSizeInPixels;
						}
						else if (tileMapPixelX < 0 || tileMapPixelX >= BackgroundAndWindowSizeInPixels)
						{
							// drawing the window which does not wrap, and we're out of bounds so nothing to draw
							continue;
						}
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
						// remember both the actual color and the index
						var colorIndex = getColorIndexFromTile(tileData, tileIndex, tileX, tileY);
						outputPixels[screenX] = RegisterBGP[colorIndex];
						backgroundAndWindowColorIndex[screenX] = colorIndex;
					}
				}

				void drawSpriteLine(Span<byte> tileData, Sprite sprite)
				{
					// in pixels on the device display, so 0 to 143
					var screenY = RegisterLY;
					// which tile to draw
					var tileIndex = sprite.TileIndex;
					if (spritesAreBig)
					{
						tileIndex = (byte)(tileIndex & 0b1111_1110);
					}
					// the pixel offset on the tile, so 0 to 7
					var tileY = screenY - sprite.AdjustedY;
					if (tileY >= 8)
					{
						tileY -= 8;
						// drawing the bottom half of a big sprite
						tileIndex++;
					}
					if (sprite.WrapY)
					{
						tileY = 7 - tileY;
					}
					// iterate over x coordinates in pixels that are part of this sprite, so in range 0 to 159
					for (var screenX = sprite.AdjustedX; screenX < sprite.AdjustedX + 8; screenX++)
					{
						// clip to screen
						if (screenX < 0 || screenX >= ScreenWidth)
						{
							continue;
						}
						// the pixel offset on that tile, so 0 to 7
						var tileX = screenX - sprite.AdjustedX;
						if (sprite.WrapX)
						{
							tileX = 7 - tileX;
						}
						// draw the pixel, but index 0 is transparent
						var colorIndex = getColorIndexFromTile(tileData, tileIndex, tileX, tileY);
						if (colorIndex != 0)
						{
							bool shouldDraw;
							if (!backgroundAndWindowEnabled)
							{
								// only sprites are visible
								shouldDraw = true;
							}
							else if (sprite.DrawOnTopOfBackgroundAndWindow)
							{
								// always draw this sprite on top of background and window
								shouldDraw = true;
							}
							else
							{
								// only draw sprites on top of color 0, behind other colors
								shouldDraw = backgroundAndWindowColorIndex[screenX] == 0;
							}
							if (shouldDraw)
							{
								var palette = sprite.PaletteIsOBP0 ? RegisterOBP0 : RegisterOBP1;
								outputPixels[screenX] = palette[colorIndex];
							}
						}
					}
				}

				byte getColorIndexFromTile(Span<byte> tileData, int tileIndex, int tileX, int tileY)
				{
					// the index into the tile data for the row of pixels we're drawing
					var tileDataIndex = tileIndex * TileLengthInBytes + tileY * 2;
					// the two bytes that make up this row
					var tileDataLow = tileData[tileDataIndex];
					var tileDataHigh = tileData[tileDataIndex + 1];
					// the index into the palette for this pixel
					int colorShift = 7 - tileX;
					int colorMask = 1 << colorShift;
					return (byte)(((tileDataLow & colorMask) >> colorShift) | (((tileDataHigh & colorMask) >> colorShift) << 1));
				}
			}
		}
	}

	public void RegisterLYWrite(byte oldValue, ref byte newValue)
	{
		// if update flag is true it means we're actively trying to update this value as part of the scan line state machine
		// in that case we let the intended write go through normally
		// if we're not in the middle of that, it means the CPU is writing here expecting to cause it to reset
		if (expectedLYUpdate)
		{
			expectedLYUpdate = false;
		}
		else
		{
#if DEBUG
			logger.LogTrace("LY reset");
#endif
			// docs are unclear about what this does to the video state machine, just says
			// "Writing will reset the counter."
			newValue = 0;
		}
	}

	// TODO register caching so we're not reading mem all the time

	private byte RegisterLCDC
	{
		get => memory.ReadUInt8(Memory.IO_LCDC);
		set => memory.WriteUInt8(Memory.IO_LCDC, value);
	}

	private byte RegisterSTAT
	{
		get => memory.ReadUInt8(Memory.IO_STAT);
		set => memory.WriteUInt8(Memory.IO_STAT, value);
	}

	private byte RegisterLY
	{
		get => memory.ReadUInt8(Memory.IO_LY);
		set
		{
			// we need to update this register value, but memory will emit events when we do
			// this flag gets checked when we're handling the memory write event
			expectedLYUpdate = true;
			memory.WriteUInt8(Memory.IO_LY, value);
		}
	}

	private byte RegisterLYC =>
		memory.ReadUInt8(Memory.IO_LYC);

	private byte RegisterSCX =>
		memory.ReadUInt8(Memory.IO_SCX);

	private byte RegisterSCY =>
		memory.ReadUInt8(Memory.IO_SCY);

	private byte RegisterWX =>
		memory.ReadUInt8(Memory.IO_WX);

	private byte RegisterWY =>
		memory.ReadUInt8(Memory.IO_WY);

	private ColorPalette RegisterBGP =>
		new ColorPalette(memory.ReadUInt8(Memory.IO_BGP));

	private ColorPalette RegisterOBP0 =>
		new ColorPalette(memory.ReadUInt8(Memory.IO_OBP0));

	private ColorPalette RegisterOBP1 =>
		new ColorPalette(memory.ReadUInt8(Memory.IO_OBP1));
}