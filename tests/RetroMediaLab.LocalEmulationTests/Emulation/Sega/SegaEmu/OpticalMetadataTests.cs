using System.IO;
using System.Text;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class OpticalMetadataTests
{
    [Fact]
    public void ExistingCompiledConsumersCanStillCallTheTrackConstructor()
    {
        var constructor=typeof(OpticalTrackDescriptor).GetConstructors().SingleOrDefault(value=>value.GetParameters().Length==22);
        Assert.NotNull(constructor);
        object?[] arguments=[1,1,OpticalTrackMode.Mode1Data2048,0L,1L,2048,0,2048,
            new MemoryRandomAccessData(new byte[2048]),0L,null,0L,0L,null,null,null,null,0L,0,0L,null,0];
        var track=Assert.IsType<OpticalTrackDescriptor>(constructor.Invoke(arguments));
        Assert.Equal(1,track.SectorCount); Assert.Empty(track.Metadata);
    }

    [Fact]
    public async Task CueRetainsVorbisAndLoopOffsetsThroughItsTextWriter()
    {
        string[] lines=["FILE \"data.bin\" BINARY","TRACK 01 MODE1/2048","INDEX 01 00:00:00",
            "FILE \"audio.ogg\" WAVE","REM LOOP 123","TRACK 02 AUDIO","INDEX 01 00:00:00"];
        CueSheetReader reader=new(); var sheet=reader.Parse(lines);
        Assert.Equal(CueFileKind.Vorbis,sheet.Files[1].Kind);
        Assert.Equal("123",sheet.Files[1].Tracks[0].Metadata["REM:LOOP"]);
        MemoryStream? stream=null;
        try
        {
            stream=new(); await new CueSheetWriter().WriteAsync(sheet,stream);
            var restored=reader.Parse(Encoding.UTF8.GetString(stream.ToArray()).Split('\n'));
            Assert.Equal(CueFileKind.Vorbis,restored.Files[1].Kind);
            Assert.Equal("123",restored.Files[1].Tracks[0].Metadata["REM:LOOP"]);
        }
        finally { stream?.Dispose(); }
    }

    [Fact]
    public void CueNoLoopWithoutAValueOverridesTheEarlierLoop()
    {
        var sheet=new CueSheetReader().Parse(["FILE \"audio.wav\" WAVE","TRACK 01 AUDIO",
            "REM LOOP 150","REM NOLOOP","INDEX 01 00:00:00"]);
        var metadata=sheet.Files[0].Tracks[0].Metadata;
        Assert.True(metadata.ContainsKey("REM:NOLOOP")); Assert.False(metadata.ContainsKey("REM:LOOP"));
    }
}
