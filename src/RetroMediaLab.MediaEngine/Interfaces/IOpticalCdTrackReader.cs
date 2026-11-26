namespace RetroMediaLab.MediaEngine.Interfaces;

/// <summary>Reads a formatted MMC table of contents and raw main-channel CD sectors from an optical drive.</summary>
public interface IOpticalCdTrackReader
{
    byte[] ReadTableOfContents();

    byte[] ReadRawCdSectors(uint firstBlock, byte blockCount, bool audio);
}
