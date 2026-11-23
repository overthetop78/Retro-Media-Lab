namespace RetroMediaLab.VideoPresentation.Enums;

public enum EmulationCrtColorStandards
{
    Pal = 1,
    Secam = 2,
    Ntsc = 4,
    PalSecam = Pal | Secam,
    PalNtsc = Pal | Ntsc,
    PalSecamNtsc = Pal | Secam | Ntsc
}
