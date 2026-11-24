using System.Numerics;

namespace RetroMediaLab.Emulation.Contracts;

/// <summary>
/// Segment du faisceau dans la zone visible normalisee, de (0,0) en haut a gauche a (1,1)
/// en bas a droite. Color contient le RGB lineaire et Intensity le niveau d'emission, entre 0 et 1.
/// Deux points identiques representent un point fixe.
/// </summary>
public readonly record struct VideoVectorSegment(Vector2 Start, Vector2 End, Vector3 Color, float Intensity = 1f);
