using Microsoft.Extensions.Logging;

namespace Gameboy;

public class Sound : ISteppable
{
	private readonly ILogger logger;

	private byte registerNR10;
	private byte registerNR11;
	private byte registerNR12;
	private byte registerNR13;
	private byte registerNR14;
	private byte registerNR21;
	private byte registerNR22;
	private byte registerNR23;
	private byte registerNR24;
	private byte registerNR30;
	private byte registerNR31;
	private byte registerNR32;
	private byte registerNR33;
	private byte registerNR34;
	private byte registerNR41;
	private byte registerNR42;
	private byte registerNR43;
	private byte registerNR44;
	private byte registerNR50;
	private byte registerNR51;
	private byte registerNR52;
	private byte registerWavePattern0;
	private byte registerWavePattern1;
	private byte registerWavePattern2;
	private byte registerWavePattern3;
	private byte registerWavePattern4;
	private byte registerWavePattern5;
	private byte registerWavePattern6;
	private byte registerWavePattern7;
	private byte registerWavePattern8;
	private byte registerWavePattern9;
	private byte registerWavePattern10;
	private byte registerWavePattern11;
	private byte registerWavePattern12;
	private byte registerWavePattern13;
	private byte registerWavePattern14;
	private byte registerWavePattern15;

	public Sound(ILoggerFactory loggerFactory)
	{
		logger = loggerFactory.CreateLogger<Sound>();
	}

	public ulong Clock { get; set; }

	public void Reset()
	{
		Clock = 0;

		registerNR10 = 0x80;
		registerNR11 = 0xbf;
		registerNR12 = 0xf3;
		registerNR13 = 0x00;
		registerNR14 = 0xbf;
		registerNR21 = 0x3f;
		registerNR22 = 0x00;
		registerNR23 = 0x00;
		registerNR24 = 0xbf;
		registerNR30 = 0x7f;
		registerNR31 = 0xff;
		registerNR32 = 0x9f;
		registerNR33 = 0x00;
		registerNR34 = 0xbf;
		registerNR41 = 0xff;
		registerNR42 = 0x00;
		registerNR43 = 0x00;
		registerNR44 = 0x00;
		registerNR50 = 0x77;
		registerNR51 = 0xf3;
		// f1 for gameboy, f0 for super gameboy
		registerNR52 = 0xf1;
		registerWavePattern0 = 0x00;
		registerWavePattern1 = 0x00;
		registerWavePattern2 = 0x00;
		registerWavePattern3 = 0x00;
		registerWavePattern4 = 0x00;
		registerWavePattern5 = 0x00;
		registerWavePattern6 = 0x00;
		registerWavePattern7 = 0x00;
		registerWavePattern8 = 0x00;
		registerWavePattern9 = 0x00;
		registerWavePattern10 = 0x00;
		registerWavePattern11 = 0x00;
		registerWavePattern12 = 0x00;
		registerWavePattern13 = 0x00;
		registerWavePattern14 = 0x00;
		registerWavePattern15 = 0x00;
	}

	public void Step()
	{
		// minimum instruction size, no need to waste real time going tick by tick
		Clock += 4;

		// TODO implement sound
	}

	public byte RegisterNR10 { get => registerNR10; set => registerNR10 = value; }
	public byte RegisterNR11 { get => registerNR11; set => registerNR11 = value; }
	public byte RegisterNR12 { get => registerNR12; set => registerNR12 = value; }
	public byte RegisterNR13 { get => registerNR13; set => registerNR13 = value; }
	public byte RegisterNR14 { get => registerNR14; set => registerNR14 = value; }
	public byte RegisterNR21 { get => registerNR21; set => registerNR21 = value; }
	public byte RegisterNR22 { get => registerNR22; set => registerNR22 = value; }
	public byte RegisterNR23 { get => registerNR23; set => registerNR23 = value; }
	public byte RegisterNR24 { get => registerNR24; set => registerNR24 = value; }
	public byte RegisterNR30 { get => registerNR30; set => registerNR30 = value; }
	public byte RegisterNR31 { get => registerNR31; set => registerNR31 = value; }
	public byte RegisterNR32 { get => registerNR32; set => registerNR32 = value; }
	public byte RegisterNR33 { get => registerNR33; set => registerNR33 = value; }
	public byte RegisterNR34 { get => registerNR34; set => registerNR34 = value; }
	public byte RegisterNR41 { get => registerNR41; set => registerNR41 = value; }
	public byte RegisterNR42 { get => registerNR42; set => registerNR42 = value; }
	public byte RegisterNR43 { get => registerNR43; set => registerNR43 = value; }
	public byte RegisterNR44 { get => registerNR44; set => registerNR44 = value; }
	public byte RegisterNR50 { get => registerNR50; set => registerNR50 = value; }
	public byte RegisterNR51 { get => registerNR51; set => registerNR51 = value; }
	public byte RegisterNR52 { get => registerNR52; set => registerNR52 = value; }
	public byte RegisterWavePattern0 { get => registerWavePattern0; set => registerWavePattern0 = value; }
	public byte RegisterWavePattern1 { get => registerWavePattern1; set => registerWavePattern1 = value; }
	public byte RegisterWavePattern2 { get => registerWavePattern2; set => registerWavePattern2 = value; }
	public byte RegisterWavePattern3 { get => registerWavePattern3; set => registerWavePattern3 = value; }
	public byte RegisterWavePattern4 { get => registerWavePattern4; set => registerWavePattern4 = value; }
	public byte RegisterWavePattern5 { get => registerWavePattern5; set => registerWavePattern5 = value; }
	public byte RegisterWavePattern6 { get => registerWavePattern6; set => registerWavePattern6 = value; }
	public byte RegisterWavePattern7 { get => registerWavePattern7; set => registerWavePattern7 = value; }
	public byte RegisterWavePattern8 { get => registerWavePattern8; set => registerWavePattern8 = value; }
	public byte RegisterWavePattern9 { get => registerWavePattern9; set => registerWavePattern9 = value; }
	public byte RegisterWavePattern10 { get => registerWavePattern10; set => registerWavePattern10 = value; }
	public byte RegisterWavePattern11 { get => registerWavePattern11; set => registerWavePattern11 = value; }
	public byte RegisterWavePattern12 { get => registerWavePattern12; set => registerWavePattern12 = value; }
	public byte RegisterWavePattern13 { get => registerWavePattern13; set => registerWavePattern13 = value; }
	public byte RegisterWavePattern14 { get => registerWavePattern14; set => registerWavePattern14 = value; }
	public byte RegisterWavePattern15 { get => registerWavePattern15; set => registerWavePattern15 = value; }
}
