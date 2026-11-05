using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    private static readonly ConditionalWeakTable<Acb4000, DiskClock> Clocks = new();

    private sealed class DiskClock
    {
        internal Disk?[] Drives { get; } = new Disk?[2];
        internal bool ManualCells;
    }

    private static Acb4000 CreateDiskTarget()
    {
        var disk = new Disk();
        return CreateTarget(_ => disk, 2_000_000);
    }

    private static Acb4000 CreateManualTarget(Disk? disk = null)
    {
        disk ??= new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Clocks.GetValue(target, _ => throw new InvalidOperationException()).ManualCells = true;
        return target;
    }

    private static Acb4000 CreateTarget(Func<int, IScsiStorage?> drives, int ticksPerSecond)
    {
        var clock = new DiskClock();
        Acb4000? target = null;
        target = new Acb4000(unit =>
        {
            var drive = drives(unit);
            clock.Drives[unit] = drive as Disk;
            if (drive is Disk disk) disk.UpdateSignals(target!, unit);
            return drive;
        }, ticksPerSecond);
        Clocks.Add(target, clock);
        return target;
    }

    private static void Tick(Acb4000 target)
    {
        if (Clocks.TryGetValue(target, out var clock))
        {
            for (var unit = 0; unit < clock.Drives.Length; unit++) clock.Drives[unit]?.ClockSignals(target, unit);
            if (!clock.ManualCells && target.Unit < clock.Drives.Length) clock.Drives[target.Unit]?.Clock(target);
        }
        target.Tick();
    }

    private static byte[] Short(byte opcode, int unit = 0, int address = 0, int count = 0) =>
        [opcode, (byte)(unit << 5 | address >> 16 & 31), (byte)(address >> 8), (byte)address, (byte)count, 0];

    private static byte[] Long(byte opcode, int unit = 0, uint address = 0, ushort count = 0)
    {
        var result = new byte[10];
        result[0] = opcode;
        result[1] = (byte)(unit << 5);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(2), address);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(7), count);
        return result;
    }

    private static void SendCommand(Acb4000 target, byte[] cdb, bool waitForParameters = true)
    {
        target.SetInitiator(1, true, false, false);
        for (var tick = 0; tick < 10 && !target.Bus.Busy; tick++) Tick(target);
        Assert.True(target.Bus.Busy);
        target.SetInitiator(0, false, false, false);
        foreach (var value in cdb)
        {
            WaitRequest(target);
            Assert.Equal(Acb4000BusPhase.Command, target.Bus.Phase);
            Transfer(target, value);
        }
        for (var tick = 0; waitForParameters && tick < 2_000_000 && target.LoadingParameters; tick++)
        {
            Tick(target);
            if (target.ReadingParameterFields && Clocks.TryGetValue(target, out var manual) && manual.ManualCells)
                manual.Drives[target.Unit]!.FeedParameterCells(target);
        }
        if (waitForParameters) Assert.False(target.LoadingParameters);
        // Manual wire tests supply cells themselves, after the zero-distance positioning sequence.
        if (Clocks.TryGetValue(target, out var mode) && mode.ManualCells && cdb[0] is 0x08 or 0x28 or 0x2f or 0x0f)
            for (var tick = 0; tick < 8 && !target.ReadGate && !target.Bus.Request; tick++) Tick(target);
    }

    private static (byte Status, byte[] Data) Run(Acb4000 target, byte[] cdb, byte[]? write = null)
    {
        SendCommand(target, cdb);
        return ReceiveResult(target, write);
    }

    private static (byte Status, byte[] Data) ReceiveResult(Acb4000 target, byte[]? write = null)
    {
        var data = new List<byte>();
        var offset = 0;
        byte? status = null;
        while (target.Bus.Busy)
        {
            WaitRequest(target);
            switch (target.Bus.Phase)
            {
                case Acb4000BusPhase.DataIn:
                    data.Add(target.Bus.OutputData);
                    Transfer(target, 0);
                    break;
                case Acb4000BusPhase.DataOut:
                    Assert.NotNull(write);
                    Assert.True(offset < write.Length);
                    Transfer(target, write[offset++]);
                    break;
                case Acb4000BusPhase.Status:
                    Assert.Null(status);
                    status = target.Bus.OutputData;
                    Transfer(target, 0);
                    break;
                case Acb4000BusPhase.MessageIn:
                    Assert.Equal(0, target.Bus.OutputData);
                    Transfer(target, 0);
                    break;
                default:
                    Assert.Fail($"Unexpected phase {target.Bus.Phase}");
                    break;
            }
        }
        Assert.NotNull(status);
        if (write is not null) Assert.Equal(write.Length, offset);
        return (status.Value, data.ToArray());
    }

    private static void Transfer(Acb4000 target, byte value)
    {
        target.SetInitiator(value, false, true, false);
        WaitRequestCleared(target);
        Assert.False(target.Bus.Request);
        target.SetInitiator(value, false, false, false);
        Tick(target);
    }

    private static void WaitRequestCleared(Acb4000 target)
    {
        for (var tick = 0; tick < 1_000_000 && target.Bus.Request; tick++) Tick(target);
        Assert.False(target.Bus.Request);
    }

    private static void WaitRequest(Acb4000 target)
    {
        for (var tick = 0; tick < 1_000_000; tick++)
        {
            if (target.Bus.Request) return;
            Tick(target);
        }
        Assert.Fail("Target did not request a byte.");
    }

    private sealed class Disk(ushort blockSize = 256) : IScsiStorage
    {
        private byte[]? _parameters;
        internal byte[] Bytes { get; private set; } = Enumerable.Range(0, 2 * 2 * TrackSectors(blockSize) * blockSize)
            .Select(index => (byte)(index * 13 + 7)).ToArray();
        public long CapacityBytes => Bytes.Length;
        public long Cylinders { get; private set; } = 2;
        public int Heads { get; private set; } = 2;
        public int SectorsPerTrack { get; private set; } = TrackSectors(blockSize);
        public int SectorSize { get; private set; } = blockSize;
        public bool IsReadOnly { get; init; }
        internal int BadReads { get; init; }
        internal uint? PhysicalReadAddress { get; init; }
        internal bool DefectiveReadId { get; init; }
        internal bool CorruptReadId { get; init; }
        internal int Reads { get; private set; }
        internal int Writes { get; private set; }
        internal bool Formatted { get; init; } = true;
        internal bool CorruptParameters { get; init; }
        internal int ParameterAcquisitions { get; private set; }
        internal int ParameterWrites { get; private set; }
        internal int Preparations { get; private set; }
        internal bool FailPreparation { get; init; }
        internal int? FailWriteAfter { get; init; }
        internal bool FailParameterWrite { get; init; }
        internal List<string> Transfers { get; } = [];
        internal List<TrackCapture> TrackWrites { get; } = [];
        internal bool SuppressIndex { get; set; }
        internal bool Ready { get; set; } = true;
        internal bool StuckTrackZero { get; set; }
        internal int MechanicalCylinder { get; set; }
        internal byte LandingZone { get; init; }
        internal bool SequentialParameterIds { get; init; }
        internal bool AllowNegativeCylinder { get; init; }
        internal bool MechanicallySettled => seekCompletionTicks == 0;
        internal int MechanicalSteps { get; private set; }
        internal int SeekCompletionDelayTicks { get; set; } = 200;
        private int seekCompletionTicks;

        internal void UpdateSignals(Acb4000 target, int unit) => target.SetDriveSignals(unit, Ready,
            seekCompletionTicks == 0, StuckTrackZero || MechanicalCylinder == 0);

        internal void ClockSignals(Acb4000 target, int unit)
        {
            if (seekCompletionTicks > 0) seekCompletionTicks--;
            if (target.DriveStepIssued(unit))
            {
                MechanicalSteps++;
                var nextCylinder = MechanicalCylinder + (target.DriveDirectionIn(unit) ? 1 : -1);
                MechanicalCylinder = AllowNegativeCylinder ? nextCylinder : Math.Max(0, nextCylinder);
                seekCompletionTicks = SeekCompletionDelayTicks;
            }
            UpdateSignals(target, unit);
        }
        private bool formattingPrepared;
        private bool acceptingPhysicalData;
        // READ/VERIFY must consume the generated physical fields, never the logical byte API.
        public bool Read(long byteOffset, Span<byte> destination) => throw new InvalidOperationException("Logical READ bypassed the physical channel.");

        private ushort[]? readWords;
        private int readCell;
        private long seekOffset;
        private int rotationCell;
        private Acb4000ReadChannel? writtenChannel;
        private readonly Acb4000ReadChannel observedReadChannel = new(blockSize);
        private Acb4000IdField? observedId;
        private Acb4000IdField? writtenId;
        private TrackCapture? capturing;
        private ushort capturedWord;
        private int capturedWordCells;
        private bool parameterStream;
        private ushort[] parameterWords = [];

        internal void FeedParameterCells(Acb4000 target)
        {
            ParameterAcquisitions++;
            if (!Formatted) { target.ExpireParameterSearch(); return; }
            var words = BuildParameterWords();
            target.SetIndex(false);
            target.SetIndex(true);
            foreach (var word in words)
                for (var bit = 15; bit >= 0; bit--) target.ClockReadCell((word & (1 << bit)) != 0);
            target.SetIndex(false);
        }

        private ushort[] BuildParameterWords()
        {
            var parameters = Acb4000DriveParameters.Default with { BlockSize = (ushort)SectorSize,
                Cylinders = (ushort)Cylinders, Heads = (byte)Heads, LandingZone = LandingZone };
            if (_parameters is not null) Assert.True(Acb4000DriveParameters.TryDecode(_parameters, parameters, out parameters));
            var flags = new byte[9];
            if (!CorruptParameters) Acb4000PhysicalParameters.Write(flags, parameters);
            var words = new ushort[9 * 22 + 8];
            words.AsSpan().Fill(0xaaaa);
            Span<byte> id = stackalloc byte[10];
            for (var index = 0; index < flags.Length; index++)
            {
                Acb4000FieldCodec.WriteId(id, (uint)(SequentialParameterIds ? index : index * 2), flags[index]);
                var previous = false;
                Acb4000MfmEncoding.WriteField(id, words.AsSpan(index * 22 + 8), ref previous);
            }
            return words;
        }

        internal void Clock(Acb4000 target)
        {
            // A bench serializer, not the spindle timing of a native ST-506 drive.
            for (var cell = 0; cell < 5; cell++)
            {
                target.SetIndex(!SuppressIndex && rotationCell == 0);
                if (target.ReadingParameterFields)
                {
                    if (!parameterStream)
                    {
                        ParameterAcquisitions++;
                        parameterWords = BuildParameterWords();
                        parameterStream = true;
                    }
                    if (!SuppressIndex && rotationCell == 0 && !Formatted) target.ExpireParameterSearch();
                    if (Formatted && rotationCell < parameterWords.Length * 16)
                        target.ClockReadCell((parameterWords[rotationCell / 16] & (1 << (15 - rotationCell % 16))) != 0);
                    if (++rotationCell == 166_672) rotationCell = 0;
                    continue;
                }
                parameterStream = false;
                var cylinder = target.FormatCylinder;
                var head = target.FormatHead;
                var written = target.ClockWriteCell(!SuppressIndex && rotationCell == 0);
                if (target.WriteGate)
                {
                    if (capturing is null)
                    {
                        var last = TrackWrites.LastOrDefault();
                        var sameTrack = last is not null && last.Cylinder == cylinder && last.Head == head;
                        capturing = formattingPrepared && sameTrack ? last! : new TrackCapture(cylinder, head);
                        if (!ReferenceEquals(capturing, last)) TrackWrites.Add(capturing);
                        capturing.Windows++;
                        writtenChannel ??= new Acb4000ReadChannel(SectorSize);
                        writtenChannel.Configure(SectorSize);
                        if (!formattingPrepared) writtenId = observedId;
                        else if (!sameTrack) writtenId = null;
                        capturedWord = 0;
                        capturedWordCells = 0;
                    }
                    CapturePhysicalCell(target, written);
                }
                else capturing = null;
                if (++rotationCell == 166_672) rotationCell = 0;
                if (!target.ReadGate) continue;
                if (readWords is null || readCell == readWords.Length * 16)
                {
                    Reads++;
                    Transfers.Add("R" + seekOffset / SectorSize);
                    var id = new byte[10];
                    Acb4000FieldCodec.WriteId(id, PhysicalReadAddress ?? checked((uint)(seekOffset / SectorSize)), 0, DefectiveReadId);
                    if (CorruptReadId) id[^1] ^= 1;
                    var data = new byte[SectorSize + 6];
                    Acb4000FieldCodec.WriteData(data, Bytes.AsSpan((int)seekOffset, SectorSize));
                    if (Reads <= BadReads)
                    {
                        // Two separate ECC faults leave the transferred payload unchanged and cannot be corrected as one burst.
                        data[^4] ^= 1;
                        data[^1] ^= 1;
                    }
                    readWords = new ushort[id.Length + data.Length + 27];
                    readWords.AsSpan().Fill(0xaaaa);
                    var previous = false;
                    Acb4000MfmEncoding.WriteField(id, readWords.AsSpan(8), ref previous);
                    Acb4000MfmEncoding.WriteField(data, readWords.AsSpan(id.Length + 23), ref previous);
                    readCell = 0;
                }
                var value = (readWords[readCell / 16] & (1 << (15 - readCell % 16))) != 0;
                readCell++;
                if (observedReadChannel.Feed(value) == Acb4000FieldKind.Id)
                    observedId = Acb4000FieldCodec.TryReadId(observedReadChannel.Field, false, out var identity, out _)
                        ? identity : null;
                target.ClockReadCell(value);
            }
        }

        private void CapturePhysicalCell(Acb4000 target, bool cell)
        {
            capturing!.Cells++;
            capturedWord = (ushort)((capturedWord << 1) | (cell ? 1 : 0));
            if (++capturedWordCells == 16)
            {
                capturing.Words.Add(capturedWord);
                capturedWord = 0;
                capturedWordCells = 0;
            }
            var kind = writtenChannel!.Feed(cell);
            if (kind != Acb4000FieldKind.None) Assert.True(writtenChannel.FieldClockValid);
            if (kind == Acb4000FieldKind.Id)
            {
                Assert.True(Acb4000FieldCodec.TryReadId(writtenChannel.Field, false, out var id, out _));
                writtenId = id;
                capturing.Ids.Add(id);
            }
            else if (kind == Acb4000FieldKind.Data && writtenId is { IsDefective: false } identity)
            {
                Span<byte> data = stackalloc byte[1024];
                data = data[..SectorSize];
                Assert.True(Acb4000FieldCodec.TryReadData(writtenChannel.Field, data, false, out var valid, out _));
                Assert.True(valid);
                try
                {
                    acceptingPhysicalData = true;
                    Write((long)identity.LogicalAddress * SectorSize, data);
                }
                catch (IOException) { target.SetWriteFault(true); }
                finally { acceptingPhysicalData = false; }
            }
        }

        internal sealed class TrackCapture(int cylinder, int head)
        {
            internal int Cylinder { get; } = cylinder;
            internal int Head { get; } = head;
            internal long Cells { get; set; }
            internal int Windows { get; set; }
            internal List<ushort> Words { get; } = [];
            internal List<Acb4000IdField> Ids { get; } = [];
        }
        public void Write(long byteOffset, ReadOnlySpan<byte> source)
        {
            if (!acceptingPhysicalData)
                throw new InvalidOperationException("WRITE/FORMAT bypassed the physical write channel.");
            if (FailWriteAfter is { } after && Writes >= after) throw new IOException();
            Writes++;
            Transfers.Add("W" + byteOffset / SectorSize);
            source.CopyTo(Bytes.AsSpan((int)byteOffset, source.Length));
        }
        public long Seek(long byteOffset)
        {
            seekOffset = byteOffset;
            readWords = null;
            readCell = 0;
            observedReadChannel.Configure(SectorSize);
            observedId = null;
            return 0;
        }
        public void PrepareFormat(long cylinders, int heads, int sectorsPerTrack, int sectorSize)
        {
            if (FailPreparation) throw new IOException();
            Preparations++;
            formattingPrepared = true;
            var bytes = Bytes;
            Array.Resize(ref bytes, checked((int)(cylinders * heads * sectorsPerTrack * sectorSize)));
            Bytes = bytes;
            Cylinders = cylinders;
            Heads = heads;
            SectorsPerTrack = sectorsPerTrack;
            SectorSize = sectorSize;
        }
        public byte[]? ReadParameters()
        {
            throw new InvalidOperationException("Parameter acquisition bypassed the physical read channel.");
        }
        public void WriteParameters(ReadOnlySpan<byte> parameters)
        {
            if (FailParameterWrite) throw new IOException();
            ParameterWrites++;
            _parameters = parameters.ToArray();
            formattingPrepared = false;
        }
        public void Flush() { }
        private static int TrackSectors(ushort size) => size switch
        {
            256 => 33,
            512 => 18,
            1024 => 9,
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
    }
}
