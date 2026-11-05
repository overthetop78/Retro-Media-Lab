using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using Address = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Valeurs du paramétrage de carte ; aucun fichier DSC ni stockage de paramètres n'est connu ici.</summary>
/// <remarks>WritePrecompensation conserve les octets reçus pour la restitution mais le 4000 ignore leur valeur :
/// le précompensateur physique dépend du courant réduit et du cavalier de carte.</remarks>
public readonly record struct Acb4000DriveParameters(ushort BlockSize, ushort Cylinders, byte Heads,
    ushort ReducedWriteCurrent, ushort WritePrecompensation, byte LandingZone, byte StepRate)
{
    public static Acb4000DriveParameters Default { get; } = new(C.MinimumBlockSize, C.DefaultCylinders,
        C.DefaultHeads, C.DefaultReducedCurrentCylinder, C.DefaultReducedCurrentCylinder, 0, C.NonBufferedStepCode);

    public bool IsValid => BlockSize is C.MinimumBlockSize or C.MediumBlockSize or C.MaximumBlockSize
        && Cylinders is >= C.MinimumCylinders and <= C.MaximumCylinders
        && Heads is >= C.MinimumHeads and <= C.MaximumHeads
        && ReducedWriteCurrent <= C.MaximumReducedCurrentCylinder
        && StepRate <= C.FastBufferedStepCode;

    public int StepPulseNanoseconds => StepRate switch
    {
        C.NonBufferedStepCode => C.NonBufferedStepNanoseconds,
        C.BufferedStepCode => C.BufferedStepNanoseconds,
        C.FastBufferedStepCode => C.FastBufferedStepNanoseconds,
        _ => throw new InvalidOperationException()
    };

    public int LandingCylinder => (LandingZone & C.LandingBeforeTrackZeroMask) != 0
        ? -(LandingZone & C.LandingDistanceMask)
        : Cylinders - 1 + (LandingZone & C.LandingDistanceMask);

    /// <summary>Décode une liste extent et son éventuelle liste de lecteur. Une extension après les 22 octets
    /// n'est pas interprétée ici ; la commande décide de la longueur de transfert autorisée.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> data, Acb4000DriveParameters previous, out Acb4000DriveParameters result)
    {
        result = previous;
        if (data.Length < C.ExtentParameterLength || data.Length is > C.ExtentParameterLength and < C.DriveParameterLength)
            return false;
        if (data[C.ExtentLengthOffset] != C.ExtentLength) return false;
        for (var index = 0; index < C.ExtentLengthOffset; index++)
            if (data[index] != 0) return false;
        // MFM density and the reserved extent bytes, including the unsupported partial-extent fields.
        for (var index = C.DensityOffset; index < C.BlockSizeHighOffset; index++)
            if (data[index] != 0) return false;
        var blockSize = data[C.BlockSizeHighOffset] << Address.AddressHighShift
            | data[C.BlockSizeMiddleOffset] << Address.AddressMiddleShift | data[C.BlockSizeLowOffset];
        if (blockSize is not (C.MinimumBlockSize or C.MediumBlockSize or C.MaximumBlockSize)) return false;
        var candidate = previous with { BlockSize = (ushort)blockSize };
        if (data.Length >= C.DriveParameterLength)
        {
            if (data[C.ListCodeOffset] != C.ListCode) return false;
            candidate = new((ushort)blockSize,
                BinaryPrimitives.ReadUInt16BigEndian(data[C.CylinderOffset..]), data[C.HeadOffset],
                BinaryPrimitives.ReadUInt16BigEndian(data[C.ReducedCurrentOffset..]),
                BinaryPrimitives.ReadUInt16BigEndian(data[C.PrecompensationOffset..]),
                data[C.LandingOffset], data[C.StepRateOffset]);
        }
        if (!candidate.IsValid) return false;
        result = candidate;
        return true;
    }

    public void Write(Span<byte> destination, bool includeDriveParameters = true)
    {
        if (!IsValid) throw new InvalidOperationException();
        var length = includeDriveParameters ? C.DriveParameterLength : C.ExtentParameterLength;
        if (destination.Length < length) throw new ArgumentException(nameof(destination));
        destination[..length].Clear();
        destination[C.ExtentLengthOffset] = C.ExtentLength;
        destination[C.BlockSizeMiddleOffset] = (byte)(BlockSize >> Address.AddressMiddleShift);
        destination[C.BlockSizeLowOffset] = (byte)BlockSize;
        if (!includeDriveParameters) return;
        destination[C.ListCodeOffset] = C.ListCode;
        BinaryPrimitives.WriteUInt16BigEndian(destination[C.CylinderOffset..], Cylinders);
        destination[C.HeadOffset] = Heads;
        BinaryPrimitives.WriteUInt16BigEndian(destination[C.ReducedCurrentOffset..], ReducedWriteCurrent);
        BinaryPrimitives.WriteUInt16BigEndian(destination[C.PrecompensationOffset..], WritePrecompensation);
        destination[C.LandingOffset] = LandingZone;
        destination[C.StepRateOffset] = StepRate;
    }
}
