using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Gameboy;

public abstract class Video : ISteppable
{
	public enum Palette
	{
		None,
		Background,
		Window,
		SpriteOBJ0,
		SpriteOBJ1
	}

	/// <param name="Value">a 2-bit value, high 6 bits will be 0</param>
	/// <param name="Palette">which type of rendering produced this</param>
	public record struct Color(
		byte Value,
		Palette Palette
	)
	{ }

	public delegate void ScanlineAvailableDelegate(int y);
	public delegate void VSyncDelegate();
	public delegate void VBlankInterruptDelegate();
	public delegate void LCDCInterruptDelegate();

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
		private byte[] values;

		public ColorPalette(byte registerValue)
		{
			values = new byte[]
			{
				(byte)((registerValue & 0b0000_0011) >> 0),
				(byte)((registerValue & 0b0000_1100) >> 2),
				(byte)((registerValue & 0b0011_0000) >> 4),
				(byte)((registerValue & 0b1100_0000) >> 6),
			};
		}

		public byte this[int index] =>
			values[index];
	}

	public const int ScreenWidth = 160;
	public const int ScreenHeight = 144;

	public event ScanlineAvailableDelegate? OnScanlineAvailable;
	public event VSyncDelegate? OnVSync;
	public event VBlankInterruptDelegate? OnVBlankInterrupt;
	public event LCDCInterruptDelegate? OnLCDCInterrupt;

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
	private readonly byte[] videoData = new byte[Memory.VIDEO_RAM_END - Memory.VIDEO_RAM_START + 1];
	private readonly byte[] spriteAttributesData = new byte[Memory.SPRITE_ATTRIBUTES_END - Memory.SPRITE_ATTRIBUTES_START + 1];

	private UInt64 clock;

	private bool videoDataWriteEnabled;
	private bool spriteAttributesDataWriteEnabled;

	private byte registerLCDC;
	private byte registerSTAT;
	private byte registerSCY;
	private byte registerSCX;
	private byte registerLY;
	private byte registerLYC;
	private byte registerBGP;
	private byte registerOBP0;
	private byte registerOBP1;
	private byte registerWY;
	private byte registerWX;

	private ColorPalette registerBGPPalette;
	private ColorPalette registerOBP0Palette;
	private ColorPalette registerOBP1Palette;

	private State state;
	private UInt64 ticksRemainingInCurrentState;

	// used during output of a scanline
	private readonly byte[] backgroundAndWindowColorIndex = new byte[ScreenWidth];

	// TODO timing debugging
	internal readonly Stopwatch TileDataReadStopwatch = new();
	internal readonly Stopwatch BackgroundAndWindowStopwatch = new();
	internal readonly Stopwatch SpritesStopwatch = new();
	internal readonly Stopwatch EmitScanlineStopwatch = new();

	public Video(ILoggerFactory loggerFactory)
	{
		logger = loggerFactory.CreateLogger<Video>();
		Reset();
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		Clock = 0;

		videoDataWriteEnabled = true;
		spriteAttributesDataWriteEnabled = true;

		RegisterLCDC = 0x91;
		RegisterSTAT = 0x00;
		RegisterSCY = 0x00;
		RegisterSCX = 0x00;
		RegisterLY = 0x00;
		RegisterLYC = 0x00;
		RegisterBGP = 0xfc;
		RegisterOBP0 = 0xff;
		RegisterOBP1 = 0xff;
		RegisterWY = 0x00;
		RegisterWX = 0x00;

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
					// intentionally don't write to the accessors, that clears it
					registerLY++;
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
						OnVBlankInterrupt?.Invoke();
						triggerSTATInterruptIfMaskSet(0b0001_0000);
#if DEBUG
						logger.LogTrace("emitting vsync");
#endif
						OnVSync?.Invoke();
						VSync();
					}
					triggerSTATInterruptBasedOnLYAndLYC();
					enableSpriteAttributeMemory();
					enableVideoMemory();
					break;

				case State.VBlank:
					// intentionally don't write to the accessors, that clears it
					registerLY++;
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

			void triggerSTATInterruptIfMaskSet(byte mask)
			{
				if ((RegisterSTAT & mask) != 0)
				{
					OnLCDCInterrupt?.Invoke();
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
						OnLCDCInterrupt?.Invoke();
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
				// VideoDataWriteEnabled = true;
			}

			void disableVideoMemory()
			{
#if DEBUG
				logger.LogTrace("disabling video memory");
#endif
				// VideoDataWriteEnabled = false;
			}

			void enableSpriteAttributeMemory()
			{
#if DEBUG
				logger.LogTrace("enabling sprite attribute memory");
#endif
				// SpriteAttributesDataWriteEnabled = true;
			}

			void disableSpriteAttributeMemory()
			{
#if DEBUG
				logger.LogTrace("disabling sprite attribute memory");
#endif
				// SpriteAttributesDataWriteEnabled = false;
			}

			void drawScanLine()
			{
				TileDataReadStopwatch.Start();
				/*
				unsigned indices, values from 0 to 255
				tile 0 is at 0x8000
				tile 255 is at 0x8ff0
				*/
				var tileData1 = videoData.AsSpan(0, OneTileDataSectionLengthInBytes);
				/*
				signed indices, values from -128 to 127
				tile -128 is at 0x8800
				tile 0 is at 0x9000
				tile 127 is at 0x97f0

				the two tile data do overlap
				*/
				var tileData2 = videoData.AsSpan(TileDataSegmentLength, OneTileDataSectionLengthInBytes);
				TileDataReadStopwatch.Stop();

				// LCDC flags
				var backgroundAndWindowEnabled = (RegisterLCDC & 0b0000_0001) != 0;
				var spritesEnabled = (RegisterLCDC & 0b0000_0010) != 0;
				// true = sprites are drawn in 2s as 8x16, false = sprites are 8x8
				var spritesAreBig = (RegisterLCDC & 0b0000_0100) != 0;
				// where in video memory the indices of the background tiles are
				var backgroundTileIndicesAddress = (UInt16)((RegisterLCDC & 0b0000_1000) != 0 ? 0x1c00 : 0x1800);
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
				var windowTileIndicesAddress = (UInt16)((RegisterLCDC & 0b0100_0000) != 0 ? 0x1c00 : 0x1800);
				// TODO respect LCDC bit 7, display enabled

				BackgroundAndWindowStopwatch.Start();
				if (backgroundAndWindowEnabled)
				{
					drawTileMap(
						tileData: backgroundAndWindowTileData,
						tileIndices: videoData.AsSpan(backgroundTileIndicesAddress, BackgroundAndWindowTileIndicesLengthInBytes),
						scrollX: RegisterSCX,
						scrollY: RegisterSCY,
						wrap: true,
						palette: Palette.Background
					);
					if (windowEnabled)
					{
						drawTileMap(
							tileData: backgroundAndWindowTileData,
							tileIndices: videoData.AsSpan(windowTileIndicesAddress, BackgroundAndWindowTileIndicesLengthInBytes),
							scrollX: -(RegisterWX - 7),
							scrollY: -RegisterWY,
							wrap: false,
							palette: Palette.Window
						);
					}
				}
				else
				{
					// background disabled, so init the output arrays so sprites can always draw on top correctly
					for (var i = 0; i < ScreenWidth; i++)
					{
						SetPixel(i, RegisterLY, new Color(0, Palette.None));
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
					var sprites = new List<Sprite>(capacity: MaxSprites);
					for (var i = 0; i < MaxSprites; i++)
					{
						var sprite = MemoryMarshal.AsRef<Sprite>(spriteAttributesData.AsSpan(i * 4, 4));
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
				ScanLineAvailable(RegisterLY);
				OnScanlineAvailable?.Invoke(RegisterLY);
				EmitScanlineStopwatch.Stop();

				void drawTileMap(Span<byte> tileData, Span<byte> tileIndices, int scrollX, int scrollY, bool wrap, Palette palette)
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
						SetPixel(screenX, RegisterLY, new(registerBGPPalette[colorIndex], palette));
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
								if (sprite.PaletteIsOBP0)
								{
									SetPixel(screenX, RegisterLY, new(registerOBP0Palette[colorIndex], Palette.SpriteOBJ0));
								}
								else
								{
									SetPixel(screenX, RegisterLY, new(registerOBP1Palette[colorIndex], Palette.SpriteOBJ1));
								}
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

	public bool VideoDataWriteEnabled
	{
		get => videoDataWriteEnabled;
		internal set => videoDataWriteEnabled = value;
	}

	public bool SpriteAttributesDataWriteEnabled
	{
		get => spriteAttributesDataWriteEnabled;
		internal set => spriteAttributesDataWriteEnabled = value;
	}

	public byte RegisterLCDC
	{
		get => registerLCDC;
		set => registerLCDC = value;
	}

	public byte RegisterSTAT
	{
		get => registerSTAT;
		set => registerSTAT = value;
	}

	public byte RegisterSCY
	{
		get => registerSCY;
		set => registerSCY = value;
	}

	public byte RegisterSCX
	{
		get => registerSCX;
		set => registerSCX = value;
	}

	public byte RegisterLY
	{
		get => registerLY;
		set
		{
			// docs are unclear about what this does to the video state machine, just says
			// "Writing will reset the counter."
			registerLY = 0;
		}
	}

	public byte RegisterLYC
	{
		get => registerLYC;
		set => registerLYC = value;
	}

	public byte RegisterBGP
	{
		get => registerBGP;
		set
		{
			registerBGP = value;
			registerBGPPalette = new(value);
		}
	}

	public byte RegisterOBP0
	{
		get => registerOBP0;
		set
		{
			registerOBP0 = value;
			registerOBP0Palette = new(value);
		}
	}

	public byte RegisterOBP1
	{
		get => registerOBP1;
		set
		{
			registerOBP1 = value;
			registerOBP1Palette = new(value);
		}
	}

	public byte RegisterWY
	{
		get => registerWY;
		set => registerWY = value;
	}

	public byte RegisterWX
	{
		get => registerWX;
		set => registerWX = value;
	}

	public byte ReadVideoUInt8(UInt16 address)
	{
		if (videoDataWriteEnabled)
		{
			return videoData[address];
		}
		else
		{
			return 0xff;
		}
	}

	public void WriteVideoUInt8(UInt16 address, byte value)
	{
		if (videoDataWriteEnabled)
		{
			videoData[address] = value;
		}
	}

	public byte ReadSpriteAttributesUInt8(UInt16 address)
	{
		if (spriteAttributesDataWriteEnabled)
		{
			return spriteAttributesData[address];
		}
		else
		{
			return 0xff;
		}
	}

	public void WriteSpriteAttributesUInt8(UInt16 address, byte value)
	{
		if (spriteAttributesDataWriteEnabled)
		{
			spriteAttributesData[address] = value;
		}
	}

	/// <summary>
	/// Intended for use by the memory controller to do DMA copies.
	/// </summary>
	internal void WriteSpriteAttributesUInt8IgnoreWriteControl(UInt16 address, byte value)
	{
		spriteAttributesData[address] = value;
	}

	protected abstract void SetPixel(int x, int y, Color color);
	protected abstract void ScanLineAvailable(int y);
	protected abstract void VSync();
}