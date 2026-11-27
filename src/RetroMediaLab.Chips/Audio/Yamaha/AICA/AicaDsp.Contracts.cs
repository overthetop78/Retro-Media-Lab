using System.IO;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class AicaDsp
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DspState
    {
        internal fixed int TEMP[128];
        internal fixed int MEMS[32];
        internal fixed int MIXS[16];

        internal uint RBP;
        internal uint RBL;
        internal uint MDEC_CT;

        internal int SHIFTED;
        internal int B;
        internal fixed int MEMVAL[4];
        internal int FRC_REG;
        internal int Y_REG;
        internal uint ADRS_REG;

        [MarshalAs(UnmanagedType.U1)]
        internal bool Stopped;

        [MarshalAs(UnmanagedType.U1)]
        internal bool Dirty;

        internal void Serialize(BinaryWriter writer)
        {
            for (int i = 0; i < 128; i++)
                writer.Write(TEMP[i]);
            for (int i = 0; i < 32; i++)
                writer.Write(MEMS[i]);
            for (int i = 0; i < 16; i++)
                writer.Write(MIXS[i]);
            writer.Write(RBP);
            writer.Write(RBL);
            writer.Write(MDEC_CT);
        }

        internal void Deserialize(BinaryReader reader, int version, bool rollback = false)
        {
            const int saveVersion18 = 813;
            if (version < saveVersion18)
                Skip(reader, 4096 * 8);
            for (int i = 0; i < 128; i++)
                TEMP[i] = reader.ReadInt32();
            for (int i = 0; i < 32; i++)
                MEMS[i] = reader.ReadInt32();
            for (int i = 0; i < 16; i++)
                MIXS[i] = reader.ReadInt32();
            RBP = reader.ReadUInt32();
            RBL = reader.ReadUInt32();
            if (version < saveVersion18)
                Skip(reader, 44);
            MDEC_CT = reader.ReadUInt32();
            if (version < saveVersion18)
                Skip(reader, 33596 - 4096 * 8 - sizeof(int) * (128 + 32 + 16) - 4 * 3 - 44);
            if (!rollback)
                Dirty = true;
        }

        private static void Skip(BinaryReader reader, int size)
        {
            if (reader.ReadBytes(size).Length != size)
                throw new EndOfStreamException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Instruction
    {
        internal byte TRA;
        [MarshalAs(UnmanagedType.U1)]
        internal bool TWT;
        internal byte TWA;

        [MarshalAs(UnmanagedType.U1)]
        internal bool XSEL;
        internal byte YSEL;
        internal byte IRA;
        [MarshalAs(UnmanagedType.U1)]
        internal bool IWT;
        internal byte IWA;

        [MarshalAs(UnmanagedType.U1)]
        internal bool EWT;
        internal byte EWA;
        [MarshalAs(UnmanagedType.U1)]
        internal bool ADRL;
        [MarshalAs(UnmanagedType.U1)]
        internal bool FRCL;
        internal byte SHIFT;
        [MarshalAs(UnmanagedType.U1)]
        internal bool YRL;
        [MarshalAs(UnmanagedType.U1)]
        internal bool NEGB;
        [MarshalAs(UnmanagedType.U1)]
        internal bool ZERO;
        [MarshalAs(UnmanagedType.U1)]
        internal bool BSEL;

        [MarshalAs(UnmanagedType.U1)]
        internal bool NOFL;
        [MarshalAs(UnmanagedType.U1)]
        internal bool TABLE;
        [MarshalAs(UnmanagedType.U1)]
        internal bool MWT;
        [MarshalAs(UnmanagedType.U1)]
        internal bool MRD;
        internal byte MASA;
        [MarshalAs(UnmanagedType.U1)]
        internal bool ADREB;
        [MarshalAs(UnmanagedType.U1)]
        internal bool NXADR;
    }
}
