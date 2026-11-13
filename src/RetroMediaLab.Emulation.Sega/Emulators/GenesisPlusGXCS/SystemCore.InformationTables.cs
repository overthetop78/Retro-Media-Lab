using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    private static readonly (string Code, string Name)[] companyinfo =
    [
        ("ACLD", "Ballistic"),
        ("RSI", "Razorsoft"),
        ("SEGA", "SEGA"),
        ("TREC", "Treco"),
        ("VRGN", "Virgin Games"),
        ("WSTN", "Westone"),
        ("10", "Takara"),
        ("11", "Taito or Accolade"),
        ("12", "Capcom"),
        ("13", "Data East"),
        ("14", "Namco or Tengen"),
        ("15", "Sunsoft"),
        ("16", "Bandai"),
        ("17", "Dempa"),
        ("18", "Technosoft"),
        ("19", "Technosoft"),
        ("20", "Asmik"),
        ("22", "Micronet"),
        ("23", "Vic Tokai"),
        ("24", "American Sammy"),
        ("29", "Kyugo"),
        ("32", "Wolfteam"),
        ("33", "Kaneko"),
        ("35", "Toaplan"),
        ("36", "Tecmo"),
        ("40", "Toaplan"),
        ("42", "UFL Company Limited"),
        ("43", "Human"),
        ("45", "Game Arts"),
        ("47", "Sage's Creation"),
        ("48", "Tengen"),
        ("49", "Renovation or Telenet"),
        ("50", "Electronic Arts"),
        ("56", "Razorsoft"),
        ("58", "Mentrix"),
        ("60", "Victor Musical Ind."),
        ("69", "Arena"),
        ("70", "Virgin"),
        ("73", "Soft Vision"),
        ("74", "Palsoft"),
        ("76", "Koei"),
        ("79", "U.S. Gold"),
        ("81", "Acclaim/Flying Edge"),
        ("83", "Gametek"),
        ("86", "Absolute"),
        ("87", "Mindscape"),
        ("93", "Sony"),
        ("95", "Konami"),
        ("97", "Tradewest"),
        ("100", "T*HQ Software"),
        ("101", "Tecmagik"),
        ("112", "Designer Software"),
        ("113", "Psygnosis"),
        ("119", "Accolade"),
        ("120", "Code Masters"),
        ("125", "Interplay"),
        ("130", "Activision"),
        ("132", "Shiny & Playmates"),
        ("144", "Atlus"),
        ("151", "Infogrames"),
        ("161", "Fox Interactive"),
        ("177", "Ubisoft"),
        ("239", "Disney Interactive"),
        ("---", "Unknown"),
    ];
    private static readonly (string Code, string Name)[] peripheralinfo =
    [
        ("J", "3B Joypad"),
        ("6", "6B Joypad"),
        ("K", "Keyboard"),
        ("P", "Printer"),
        ("B", "Control Ball"),
        ("F", "Floppy Drive"),
        ("L", "Activator"),
        ("4", "Team Player"),
        ("0", "MS Joypad"),
        ("R", "RS232C Serial"),
        ("T", "Tablet"),
        ("V", "Paddle"),
        ("C", "CD-ROM"),
        ("M", "Mega Mouse"),
        ("G", "Menacer"),
    ];
    public string GetCompany()
    {
        string copyright = rominfo.copyright.PadRight(8, '\0');
        string company = copyright.Substring(3, 5);
        int hyphen = company.IndexOf('-');
        if (hyphen >= 0) company = company[(hyphen + 1)..];
        company = company.TrimEnd(' ', '\0');
        if (company.Length != 0)
            for (int i = 0; i < companyinfo.Length - 1; i++)
                if (companyinfo[i].Code.StartsWith(company, StringComparison.Ordinal)) return companyinfo[i].Name;
        return companyinfo[^1].Name;
    }
    public static string GetPeripheral(int index) => (uint)index < peripheralinfo.Length ? peripheralinfo[index].Name : companyinfo[^1].Name;
}
