namespace RetroMediaLab.Emulation.Atari.Common.Machines.Common.Constants;


internal static class VideoAudioSettingsConstants
{
    internal const string StandardOption = "retromedialab_atari_video_standard";
    internal const string ResolutionOption = "retromedialab_atari_video_resolution";
    internal const string AspectRatioOption = "retromedialab_atari_video_aspect_ratio";
    internal const string CropOption = MachineOptionConstants.Crop;
    internal const string FrameSkipOption = MachineOptionConstants.Frames;
    internal const string AudioOutputOption = "retromedialab_atari_audio_output";
    internal const string AudioLatencyOption = "retromedialab_atari_audio_latency";
    internal const string AudioVolumeOption = "retromedialab_atari_audio_volume";
    internal const string AudioQualityOption = "retromedialab_atari_audio_quality";
    internal const string FloppySoundOption = "retromedialab_atari_floppy_sound";
    internal const string FloppySoundVolumeOption = "retromedialab_atari_floppy_sound_volume";
    internal const string PolarizedFilterOption = "retromedialab_atari_audio_polarized_filter";

    internal const string Automatic = "auto";
    internal const string Native = "native";
    internal const string PixelAspect = "pixel";
    internal const string FourByThree = "4:3";
    internal const string Enabled = "enabled";
    internal const string Disabled = "disabled";
    internal const string LowQuality = "low";
    internal const string NormalQuality = "normal";
    internal const string HighQuality = "high";

    internal const int MinimumFrameSkip = 0;
    internal const int MaximumFrameSkip = 5;
    internal const int FrameSkipStep = 1;
    internal const int MinimumLatencyMilliseconds = 20;
    internal const int MaximumLatencyMilliseconds = 100;
    internal const int LatencyStepMilliseconds = 10;
    internal const int MinimumVolumePercent = 0;
    internal const int MaximumVolumePercent = 100;
    internal const int VolumeStepPercent = 5;
    internal const string DefaultAudioOutput = "default";
    internal const int DefaultAudioVolumePercent = 100;
    internal static readonly IReadOnlyList<int> FloppySoundVolumesPercent = [25, 50, 75, 100];
}
