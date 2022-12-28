namespace Gameboy;

public class InterruptRegisters : IDisposable, ISteppable
{
	public const byte IF_MASK_VBLANK = 0b0000_0001;
	public const byte IF_MASK_LCDC = 0b0000_0010;
	public const byte IF_MASK_TIMER = 0b0000_0100;
	public const byte IF_MASK_SERIAL = 0b0000_1000;
	public const byte IF_MASK_KEYPAD = 0b0001_0000;

	private bool isDisposed = false;
	private UInt64 clock;

	private readonly SerialIO serialIO;
	private readonly Timer timer;
	private readonly Video video;
	private readonly Sound sound;
	private readonly Keypad keypad;

	// IO_IF
	private byte interruptFlags;
	private bool interruptFlagVBlank;
	private bool interruptFlagLCDC;
	private bool interruptFlagTimer;
	private bool interruptFlagSerial;
	private bool interruptFlagKeypad;
	// IO_IE
	private byte interruptsEnabled;
	private bool interruptEnabledVBlank;
	private bool interruptEnabledLCDC;
	private bool interruptEnabledTimer;
	private bool interruptEnabledSerial;
	private bool interruptEnabledKeypad;

	public InterruptRegisters(SerialIO serialIO, Timer timer, Video video, Sound sound, Keypad keypad)
	{
		this.serialIO = serialIO;
		this.timer = timer;
		this.video = video;
		this.sound = sound;
		this.keypad = keypad;

		video.OnVBlankInterrupt += VideoVBlankInterrupt;
		video.OnLCDCInterrupt += VideoLCDCInterrupt;
		timer.OnOverflow += TimerOverflow;
		serialIO.OnDataAvailable += SerialIODataAvailable;
		keypad.OnKeypadRegisterDelta += KeypadRegisterDelta;

		Reset();
	}

	~InterruptRegisters()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(true);
	}

	public UInt64 Clock
	{
		get => clock;
		internal set => clock = value;
	}

	public void Reset()
	{
		Clock = 0;

		InterruptFlags = 0x00;
		InterruptsEnabled = 0x00;
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;
	}

	public byte InterruptFlags
	{
		get => interruptFlags;
		set
		{
			interruptFlags = value;
			interruptFlagVBlank = (value & IF_MASK_VBLANK) != 0;
			interruptFlagLCDC = (value & IF_MASK_LCDC) != 0;
			interruptFlagTimer = (value & IF_MASK_TIMER) != 0;
			interruptFlagSerial = (value & IF_MASK_SERIAL) != 0;
			interruptFlagKeypad = (value & IF_MASK_KEYPAD) != 0;
		}
	}

	public byte InterruptsEnabled
	{
		get => interruptsEnabled;
		set
		{
			interruptsEnabled = value;
			interruptEnabledVBlank = (value & IF_MASK_VBLANK) != 0;
			interruptEnabledLCDC = (value & IF_MASK_LCDC) != 0;
			interruptEnabledTimer = (value & IF_MASK_TIMER) != 0;
			interruptEnabledSerial = (value & IF_MASK_SERIAL) != 0;
			interruptEnabledKeypad = (value & IF_MASK_KEYPAD) != 0;
		}
	}

	public bool InterruptFlagVBlank
	{
		get => interruptFlagVBlank;
		set
		{
			interruptFlagVBlank = value;
			if (value)
			{
				interruptFlags |= IF_MASK_VBLANK;
			}
			else
			{
				interruptFlags = (byte)(interruptFlags & ~IF_MASK_VBLANK);
			}
		}
	}

	public bool InterruptEnabledVBlank
	{
		get => interruptEnabledVBlank;
		set
		{
			interruptEnabledVBlank = value;
			if (value)
			{
				interruptsEnabled |= IF_MASK_VBLANK;
			}
			else
			{
				interruptsEnabled = (byte)(interruptsEnabled & ~IF_MASK_VBLANK);
			}
		}
	}

	public bool InterruptFlagLCDC
	{
		get => interruptFlagLCDC;
		set
		{
			interruptFlagLCDC = value;
			if (value)
			{
				interruptFlags |= IF_MASK_LCDC;
			}
			else
			{
				interruptFlags = (byte)(interruptFlags & ~IF_MASK_LCDC);
			}
		}
	}

	public bool InterruptEnabledLCDC
	{
		get => interruptEnabledLCDC;
		set
		{
			interruptEnabledLCDC = value;
			if (value)
			{
				interruptsEnabled |= IF_MASK_LCDC;
			}
			else
			{
				interruptsEnabled = (byte)(interruptsEnabled & ~IF_MASK_LCDC);
			}
		}
	}

	public bool InterruptFlagTimer
	{
		get => interruptFlagTimer;
		set
		{
			interruptFlagTimer = value;
			if (value)
			{
				interruptFlags |= IF_MASK_TIMER;
			}
			else
			{
				interruptFlags = (byte)(interruptFlags & ~IF_MASK_TIMER);
			}
		}
	}

	public bool InterruptEnabledTimer
	{
		get => interruptEnabledTimer;
		set
		{
			interruptEnabledTimer = value;
			if (value)
			{
				interruptsEnabled |= IF_MASK_TIMER;
			}
			else
			{
				interruptsEnabled = (byte)(interruptsEnabled & ~IF_MASK_TIMER);
			}
		}
	}

	public bool InterruptFlagSerial
	{
		get => interruptFlagSerial;
		set
		{
			interruptFlagSerial = value;
			if (value)
			{
				interruptFlags |= IF_MASK_SERIAL;
			}
			else
			{
				interruptFlags = (byte)(interruptFlags & ~IF_MASK_SERIAL);
			}
		}
	}

	public bool InterruptEnabledSerial
	{
		get => interruptEnabledSerial;
		set
		{
			interruptEnabledSerial = value;
			if (value)
			{
				interruptsEnabled |= IF_MASK_SERIAL;
			}
			else
			{
				interruptsEnabled = (byte)(interruptsEnabled & ~IF_MASK_SERIAL);
			}
		}
	}

	public bool InterruptFlagKeypad
	{
		get => interruptFlagKeypad;
		set
		{
			interruptFlagKeypad = value;
			if (value)
			{
				interruptFlags |= IF_MASK_KEYPAD;
			}
			else
			{
				interruptFlags = (byte)(interruptFlags & ~IF_MASK_KEYPAD);
			}
		}
	}

	public bool InterruptEnabledKeypad
	{
		get => interruptEnabledKeypad;
		set
		{
			interruptEnabledKeypad = value;
			if (value)
			{
				interruptsEnabled |= IF_MASK_KEYPAD;
			}
			else
			{
				interruptsEnabled = (byte)(interruptsEnabled & ~IF_MASK_KEYPAD);
			}
		}
	}

	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			isDisposed = true;
			video.OnVBlankInterrupt -= VideoVBlankInterrupt;
			video.OnLCDCInterrupt -= VideoLCDCInterrupt;
			timer.OnOverflow -= TimerOverflow;
			serialIO.OnDataAvailable -= SerialIODataAvailable;
			keypad.OnKeypadRegisterDelta -= KeypadRegisterDelta;
		}
	}

	private void VideoVBlankInterrupt()
	{
		InterruptFlagVBlank = true;
	}

	private void VideoLCDCInterrupt()
	{
		InterruptFlagLCDC = true;
	}

	private void TimerOverflow()
	{
		InterruptFlagTimer = true;
	}

	private void SerialIODataAvailable(byte value)
	{
		InterruptFlagSerial = true;
	}

	private void KeypadRegisterDelta(byte oldValue, byte newValue)
	{
		InterruptFlagKeypad = true;
	}
}