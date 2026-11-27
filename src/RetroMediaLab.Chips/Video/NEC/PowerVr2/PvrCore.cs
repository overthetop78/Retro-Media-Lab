using System.IO;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static partial class PvrCore
{
    private const int SaveVersion29 = 824;

    internal static partial void Reset(bool hard)
    {
        PvrRegisters.Reset(hard);
        PvrScanGenerator.Reset(hard);
        if (hard)
            PvrRenderer.Reset();
        PowerVr2Ta.TerminateContexts();
        PvrElan.Reset(hard);
        if (hard)
        {
            PowerVr2Ta.ParseReset();
            PvrMemory.YuvReset();
            PowerVr2Ta.TaRenderPass = 0;
        }
    }

    internal static partial void Init()
    {
        PvrScanGenerator.Init();
        PvrElan.Init();
    }

    internal static partial void Term()
    {
        PowerVr2Ta.TerminateContexts();
        PvrScanGenerator.Term();
        PvrElan.Term();
    }

    internal static partial void Serialize(BinaryWriter writer, bool rollback, bool dryRun)
    {
        PvrMemory.YuvSerialize(writer);

        writer.Write(PvrRegisters.RegisterBytes);

        PvrScanGenerator.Serialize(writer);
        PvrRenderer.Serialize(writer);

        writer.Write(PowerVr2Ta.TaFsm[2048]);
        writer.Write(PowerVr2Ta.TaFsmCurrentList);
        writer.Write(PowerVr2Ta.TaRenderPass);

        PowerVr2Ta.SerializeTaContext(writer, dryRun);

        if (!rollback)
            PvrMemory.Vram.Serialize(writer);
        PvrElan.Serialize(writer, rollback);
    }

    internal static partial void Deserialize(BinaryReader reader, int version, bool rollback)
    {
        PvrMemory.YuvDeserialize(reader);

        reader.BaseStream.ReadExactly(PvrRegisters.RegisterBytes);
        PvrRenderer.UpdateFogTable();

        PvrScanGenerator.Deserialize(reader, version);

        PvrRenderer.Deserialize(reader, version);

        PowerVr2Ta.TaFsm[2048] = reader.ReadByte();
        PowerVr2Ta.TaFsmCurrentList = reader.ReadUInt32();
        if (version >= SaveVersion29)
            PowerVr2Ta.TaRenderPass = reader.ReadUInt32();
        else
            PowerVr2Ta.TaRenderPass = 0;
        PowerVr2Ta.DeserializeTaContext(reader, version);

        if (!rollback)
            PvrMemory.Vram.Deserialize(reader);
        PvrElan.Deserialize(reader, version, rollback);
        PvrRegisters.PalNeedsUpdate = true;
    }
}
