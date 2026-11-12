using System.Text.Json.Serialization;
namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Registres programmables du Z80 ; les vues octets partagent le stockage des paires.</summary>
/// <remarks>UM008011-0816, pages 2–3, 124–126. Ce conteneur ne définit ni l'état de
/// mise sous tension ni la séquence RESET ; ceux-ci appartiennent au séquenceur CPU.</remarks>
public struct Z80Registers
{
    [JsonInclude] public ushort AF;
    [JsonInclude] public ushort BC;
    [JsonInclude] public ushort DE;
    [JsonInclude] public ushort HL;
    [JsonInclude] public ushort AlternateAF;
    [JsonInclude] public ushort AlternateBC;
    [JsonInclude] public ushort AlternateDE;
    [JsonInclude] public ushort AlternateHL;
    [JsonInclude] public ushort IX;
    [JsonInclude] public ushort IY;
    [JsonInclude] public ushort SP;
    [JsonInclude] public ushort PC;
    [JsonInclude] public byte I;
    [JsonInclude] public byte R;
    /// <summary>Paire interne WZ/MEMPTR, dont BIT (HL) rend certains bits observables.</summary>
    [JsonInclude] public ushort WZ;
    /// <summary>Historique interne des flags pour SCF/CCF ; zéro après une instruction sans effet ALU.</summary>
    [JsonInclude] public byte Q;

    public byte A { readonly get => (byte)(AF >> 8); set => AF = (ushort)((value << 8) | F); }
    public byte F { readonly get => (byte)AF; set => AF = (ushort)((AF & 0xff00) | value); }
    public byte B { readonly get => (byte)(BC >> 8); set => BC = (ushort)((value << 8) | C); }
    public byte C { readonly get => (byte)BC; set => BC = (ushort)((BC & 0xff00) | value); }
    public byte D { readonly get => (byte)(DE >> 8); set => DE = (ushort)((value << 8) | E); }
    public byte E { readonly get => (byte)DE; set => DE = (ushort)((DE & 0xff00) | value); }
    public byte H { readonly get => (byte)(HL >> 8); set => HL = (ushort)((value << 8) | L); }
    public byte L { readonly get => (byte)HL; set => HL = (ushort)((HL & 0xff00) | value); }
    public byte AlternateA { readonly get => (byte)(AlternateAF >> 8); set => AlternateAF = (ushort)((value << 8) | AlternateF); }
    public byte AlternateF { readonly get => (byte)AlternateAF; set => AlternateAF = (ushort)((AlternateAF & 0xff00) | value); }
    public byte AlternateB { readonly get => (byte)(AlternateBC >> 8); set => AlternateBC = (ushort)((value << 8) | AlternateC); }
    public byte AlternateC { readonly get => (byte)AlternateBC; set => AlternateBC = (ushort)((AlternateBC & 0xff00) | value); }
    public byte AlternateD { readonly get => (byte)(AlternateDE >> 8); set => AlternateDE = (ushort)((value << 8) | AlternateE); }
    public byte AlternateE { readonly get => (byte)AlternateDE; set => AlternateDE = (ushort)((AlternateDE & 0xff00) | value); }
    public byte AlternateH { readonly get => (byte)(AlternateHL >> 8); set => AlternateHL = (ushort)((value << 8) | AlternateL); }
    public byte AlternateL { readonly get => (byte)AlternateHL; set => AlternateHL = (ushort)((AlternateHL & 0xff00) | value); }

    /// <summary>EX AF, AF' ; laisse BC/DE/HL et les registres spéciaux inchangés.</summary>
    public void ExchangeAccumulator() => (AF, AlternateAF) = (AlternateAF, AF);

    /// <summary>EXX ; ne permute pas AF, IX, IY, SP, PC, I ni R.</summary>
    public void ExchangeGeneral()
    {
        (BC, AlternateBC) = (AlternateBC, BC);
        (DE, AlternateDE) = (AlternateDE, DE);
        (HL, AlternateHL) = (AlternateHL, HL);
    }

    public void ExchangeDeHl() => (DE, HL) = (HL, DE);

    /// <summary>Incrément du compteur de rafraîchissement 7 bits ; R7 reste programmé.</summary>
    public void IncrementRefresh() => R = (byte)((R & 0x80) | ((R + 1) & 0x7f));
}
