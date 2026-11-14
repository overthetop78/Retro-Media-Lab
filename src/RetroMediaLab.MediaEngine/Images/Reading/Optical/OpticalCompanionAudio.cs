using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using IMediaRandomAccessData = RetroMediaLab.MediaFileSystems.Interfaces.IMediaRandomAccessData;

namespace RetroMediaLab.MediaEngine.Images.Reading.Optical;

/// <summary>Resolves numbered CD-DA sidecars only when requested by an optical-session owner.</summary>
internal static class OpticalCompanionAudio
{
    private static readonly string[] Prefixes = ["", " ", "-", "_", " - "];
    internal static async Task<MediaImageDocument> AttachAsync(MediaImageDocument document,CancellationToken cancellation)
    {
        string extension=Path.GetExtension(document.Source.PrimaryPath);
        if (!extension.Equals(".iso",StringComparison.OrdinalIgnoreCase) && !extension.Equals(".bin",StringComparison.OrdinalIgnoreCase)) return document;
        if (document.Representation is not OpticalMediaImageRepresentation { Tracks.Count:1 } image || image.Tracks[0].IsAudio) return document;
        string stem=Path.Combine(Path.GetDirectoryName(document.Source.PrimaryPath)!,Path.GetFileNameWithoutExtension(document.Source.PrimaryPath));
        int selected=-1, sequenceOffset=1;
        string? current=null;
        for (int format=0;format<20;format++)
        {
            string candidate=Name(stem,format,1);
            if (File.Exists(candidate)) { selected=format; sequenceOffset=0; current=candidate; break; }
            candidate=Name(stem,format,2);
            if (File.Exists(candidate)) { selected=format; current=candidate; break; }
        }
        if (current is null) return document;
        List<OpticalTrackDescriptor> tracks=[..image.Tracks];
        List<string> associated=[..document.Source.AssociatedPaths];
        List<IDisposable> owned=[];
        bool transferred=false;
        try
        {
            long end=checked(tracks[0].FirstSector+tracks[0].SectorCount+tracks[0].PostgapSectors);
            while (current is not null && tracks.Count<99)
            {
                cancellation.ThrowIfCancellationRequested();
                IMediaRandomAccessData? raw=null;
                bool accepted=false;
                try
                {
                    raw=selected<10 ? OpenVorbis(current,cancellation) : WavePcmRandomAccessData.Open(current);
                    if (raw.Length<=0) break;
                    long count=checked((raw.Length+2351)/2352), storedPregap=0;
                    byte[] probe=new byte[4];
                    if (raw.Length>=100*2352L+4)
                    {
                        await raw.ReadExactlyAsync(100*2352L,probe,cancellation).ConfigureAwait(false);
                        if (count>150 && BinaryPrimitives.ReadUInt32LittleEndian(probe)==0) storedPregap=150;
                    }
                    PaddedSource source=new(raw,checked(count*2352));
                    long start=checked(end+150);
                    count-=storedPregap;
                    tracks.Add(new OpticalTrackDescriptor(1,tracks.Count+1,OpticalTrackMode.Audio,start,count,2352,0,2352,
                        source,storedPregap*2352,[new OpticalTrackIndex(0,-150),new OpticalTrackIndex(1,0)],150,
                        storedPregapSectors:storedPregap,storedPregapSourceOffset:storedPregap==0 ? null : 0));
                    owned.Add(source); associated.Add(current); accepted=true;
                    end=checked(start+count);
                }
                catch (Exception error) when (error is IOException or NotSupportedException) { break; }
                finally { if (!accepted && raw is IDisposable disposable) disposable.Dispose(); }
                current=Name(stem,selected,tracks.Count+sequenceOffset);
                if (!File.Exists(current)) current=null;
            }
            if (tracks.Count==1) return document;
            Dictionary<string,string> metadata=new(document.Metadata,StringComparer.Ordinal)
            {
                ["trackCount"]=tracks.Count.ToString(CultureInfo.InvariantCulture),
                ["companionAudioCount"]=(tracks.Count-1).ToString(CultureInfo.InvariantCulture)
            };
            MediaImageDocument result=new(document.Source with {AssociatedPaths=associated},document.FormatId,document.MediaKind,
                new OpticalMediaImageRepresentation(tracks.Sum(track=>checked(track.SectorCount*track.UserDataLength)),tracks,
                    image.LayerCount,image.FaceCount,[document.Source.PrimaryPath,..associated]),
                document.FileSystemVolumes,document.Diagnostics,metadata);
            transferred=true; return result;
        }
        finally { if (!transferred) foreach(IDisposable source in owned) source.Dispose(); }
    }
    private static VorbisPcmRandomAccessData OpenVorbis(string path,CancellationToken cancellation)
    {
        VorbisPcmRandomAccessData? source=null; bool transferred=false;
        try
        {
            source=VorbisPcmRandomAccessData.Open(path,cancellation);
            if (source.Channels!=2 || source.SampleRate!=44100) throw new InvalidDataException("CD audio requires stereo PCM at 44100 Hz.");
            transferred=true; return source;
        }
        finally { if (!transferred) source?.Dispose(); }
    }
    private static string Name(string stem,int format,int index)
    {
        int variant=format%10;
        return stem+Prefixes[variant%5]+index.ToString(variant<5 ? "D2" : "D",CultureInfo.InvariantCulture)+(format<10 ? ".ogg" : ".wav");
    }
    private sealed class PaddedSource(IMediaRandomAccessData source,long length) : IMediaRandomAccessData,IDisposable
    {
        public long Length=>length;
        public async ValueTask ReadExactlyAsync(long offset,Memory<byte> destination,CancellationToken cancellationToken=default)
        {
            if (offset<0 || offset>Length || destination.Length>Length-offset) throw new ArgumentOutOfRangeException(nameof(offset));
            cancellationToken.ThrowIfCancellationRequested();
            int available=(int)Math.Min(destination.Length,Math.Max(0,source.Length-offset));
            if (available>0) await source.ReadExactlyAsync(offset,destination[..available],cancellationToken).ConfigureAwait(false);
            destination.Span[available..].Clear();
        }
        public void Dispose() { if (source is IDisposable disposable) disposable.Dispose(); }
    }
}
