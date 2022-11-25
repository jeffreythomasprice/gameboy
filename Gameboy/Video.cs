using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Video : ISteppable
{
	private readonly ILogger logger;
	private readonly IMemory memory;

	private UInt64 clock;

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
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;

		/*
		TODO JEFF VIDEO!

		summary of docs

		actual screen is 160x144 pixels

		background
		window
		sprites

		tiles are 8x8 pixels, by 2 bytes per line, so 16 bytes total

		tiles 1 at 0x8000 = VIDEO_RAM_START
			can be used for anything: sprites, background, window
			256 tiles * 16 bytes = ends at 0x8fff
			unsigned indices
			tile 0 is at 0x8000
			tile 255 is at 0x8ff0
		tiles 2 at 0x8800 = VIDEO_RAM_START + 0x0800
			overlaps with tiles 1
			can only be used for background or window
			256 tiles * 16 bytes = ends at 0x97ff
			signed indices, so -128 to 127
			tile -128 is at 0x8800
			tile 0 is at 0x9000
			tile 127 is at 0x97f0

		background tile map
		32 rows, each 32 bytes long

		SCROLLX and SCROLLY are coordinates of background that is in the upper left corner
		background wraps around

		background and window indices and which tile maps to use are based on LCDC register

		window is displayed at WNDPOSX-7, WNDPOSY
		changes to WNDPOSX changes are picked up between scan line interrupts, WNDPOSY is only between screen redraws

		background and window always use same tile data table

		sprite attributes are at SPRITE_ATTRIBUTES_START
		4 bytes each, 40 max sprites
		technically 28 bits, because 4 flag bits unused, this is relevant to DMA transfers, see DMA register
		attributes:
			byte 0 = Y
			byte 1 = X
			byte 2 = ID in the sprite tile map, 0x8000
			byte 3 = flags
				bit 7 = priority
					0 = on top of background and window
					1 = behind background and window colors 1, 2, and 3, in front of background and window color 0
				bit 6 = Y flip
				bit 5 = X flip
				bit 4 = palette number, 0 = OBJ0PAL, 1 = OBJ1PAL
		
		can be in 8x8 or 8x16 mode
		in 8x16 mode least significant bit of sprite pattern number is ignored, always 0
		i.e. it takes blocks of sprites adjacent to each other and draws them on top of each other, you can't start at an odd index

		sprites with smaller X have priority and are drawn on top
		i.e. draw them right to left
		when they overlap and have the same X the one with the smaller index in the sprite attribute table wins
		i.e. the one at SPRITE_ATTRIBUTES_START+0 is on top of everything else, then SPRITE_ATTRIBUTES_START+4, then SPRITE_ATTRIBUTES_START+8, etc.

		max 10 sprites per unique Y
		larger X wins, so draw 
		Y = 0 or Y >= 144+16 means won't draw
		X = 0 or X = 160+8 means won't draw, but will count in priority calculations with other sprites on the same Y

		LCDC
			bit 7
				0 = turn off display
				1 = screen on
			bit 6
				0 = window tile indices are from 0x9800 to 0x9bff
				1 = window tile indices are from 0x9c00 to 0x9fff
			bit 5
				0 = window off
				1 = window on
			bit 4
				0 = background and window tiles draw from tiles 2, 0x8800 to 0x97ff
				1 = background and window tiles draw from tiles 1, 0x8000 to 0x8fff
			bit 3
				0 = background tile indices are from 0x9800 to 0x9bff
				1 = background tile indices are from 0x9c00 to 0x9fff
			bit 2
				0 = sprites are 8x8
				1 = sprites are 8*16 (8 width, 16 height)
			bit 1
				0 = sprites off
				1 = sprites on
			bit 0
				0 = background and window off
				1 = background and window on

		STAT
			bits 3 through 6 are interrupt selectors, 1 means that condition triggers LCDC interrupts
			bit 6 = depends on bit 2
			bit 5 = mode 10
			bit 4 = mode 01
			bit 3 = mode 00
			bit 2
				0 = bit 6 means LYC != LY
				1 = bit 6 means LYC == LY
			bit 0 and 1 = mode
				00 = H blank
				01 = V blank
				10 = sprite attributes
				11 = during transfer of data to LCD driver
			
			mode 00 = H blank
			all memory is accessible

			mode 01 = V blank
			all memory is accessible

			mode 10 = sprite attribute memory is being used
			can't access sprite attribute memory

			mode 11 = actually transferring data around
			can't access sprite attributes or video memory

			translating from rough ascii art in the docs
			each scan line goes through modes 00, 10, 11 in that order
			when it's done with all scan lines it goes to mode 01 until it's time to refresh the screen

			complete screen refresh is every 70224 clock ticks
			mode 00 (H blank) lasts between 201-207 clock ticks
			mode 01 (V blank) lasts 4560 clock ticks
			mode 10 (sprite attrs) lasts 77 to 83 clock ticks
			mode 11 (copy) lasts 169-175 clock ticks

			worst case 144 scan lines * (207 + 83 + 175) = 66960 clock ticks < 70224 clock ticks that we want per frame
			at 60 fps 70224*60 = 4213440 ~= 4.02 MHz
		
		SCY, SCX
			background scroll

		LY
			scan line that is actively being displayed
		
		LYC
			LY compare, used depending on STAT to trigger interrupts based on what line we're on

		DMA
			writing a value here triggers a hardware memory copy starting from the address written here, left shift 8 bits
			i.e. write 0x12 here and it starts copying from 0x1200
			while a copy is in progress everything except high RAM (INTERNAL_RAM_2_START) is unavailable
			a copy transfers 0x8c = 140 bytes
			40 sprites * 28 bits = 1120 bits = 140 bytes
			writing to sprite attribute memory is possible during H blank and V blank, but not otherwise
			DMA always works regardless of mode
			copies 1 byte per instruction or 1 byte per 4 ticks, or 0x8c * 4 = 560 clock ticks

		BGP
			palette data for background and window
			bits 76
				color 11
			bits 54
				color 10
			bits 32
				color 01
			bits 10
				color 00
		
		OBP0 and OBP1
			two color palettes for sprites
			exactly as BGP, except a color with a value of 0 is transaprent

		WX, WY
			window position
			window is actually drawn at WX-7, WY
		*/
	}
}