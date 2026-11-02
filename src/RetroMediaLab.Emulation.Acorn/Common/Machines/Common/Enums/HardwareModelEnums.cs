using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;

public enum HardwareAudioCapability { Sn76489, Buzzer, VidcStereo }

public enum HardwareVideoCapability { Mc6845, VideoUla, Saa5050, SevenSegmentDisplay, Mc6847, Vidc10 }

public enum HardwareCpu { Mos6502A, Rockwell65C12, Rockwell6512, Gte65C102, Intel80186, Mos6502, Motorola6809, Arm2, Arm3 }

public enum HardwareRegion { Pal, Ntsc }

public enum HardwarePortCapability { Keyboard, AnalogJoystick, DigitalJoystick, Mouse }

public enum HardwareStorageCapability { Floppy, Cassette, Cartridge, HardDisk }
