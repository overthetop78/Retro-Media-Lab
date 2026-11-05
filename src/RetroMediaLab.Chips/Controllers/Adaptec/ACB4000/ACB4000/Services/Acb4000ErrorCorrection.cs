namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Rafale corrigée, position depuis le début du champ, bit fort en premier ; zéro bit signifie résidu déjà nul.</summary>
public readonly record struct Acb4000ErrorCorrection(int FirstBit, int BitCount, byte Pattern);
