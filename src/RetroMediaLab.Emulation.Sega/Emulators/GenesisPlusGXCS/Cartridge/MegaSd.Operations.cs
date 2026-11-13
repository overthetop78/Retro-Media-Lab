using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class MegaSd
{
    private void megasd_reset()
    {
        unchecked
        {
            ClearState();
            megasd_hw.special = unchecked((byte)(7));
            memory_map[(int)(3)].read8 = megasd_ctrl_read_byte;
            memory_map[(int)(3)].read16 = megasd_ctrl_read_word;
            memory_map[(int)(3)].write8 = megasd_ctrl_write_byte;
            memory_map[(int)(3)].write16 = megasd_ctrl_write_word;
            zbank_memory_map[(int)(3)].read = megasd_ctrl_read_byte;
            if ((unchecked((int)(system_hw)) != 132)) {
            pcm_reset();
            cdd_reset();
            cd_control = unchecked((byte)(1));
            }
        }
    }
    private void megasd_enhanced_ssf2_mapper_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            int updateLastBank = 0;
            switch ((address & unchecked((uint)(15)))) {
            case 0:
            {
            if (((data & unchecked((uint)(128))) != 0)) {
            megasd_hw.unlock = unchecked((byte)(1));
            megasd_hw.writeEnable = unchecked((byte)((data & unchecked((uint)(32)))));
            for (i = 0; (i < 8); i++) {
            memory_map[(int)(i)].@base = ((new CPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)(((unchecked((int)(megasd_hw.bank0)) & 15) << 19))) & cart.mask)))) + (i << 16));
            }
            } else {
            megasd_hw.unlock = unchecked((byte)(0));
            megasd_hw.writeEnable = unchecked((byte)(0));
            for (i = 0; (i < 8); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + (i << 16));
            }
            }
            if ((megasd_hw.writeEnable != 0)) {
            for (i = 0; (i < 56); i++) {
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            } else {
            for (i = 0; (i < 56); i++) {
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            memory_map[(int)(3)].write8 = megasd_ctrl_write_byte;
            memory_map[(int)(3)].write16 = megasd_ctrl_write_word;
            }
            updateLastBank = 1;
            break;
            }
            case 15:
            {
            megasd_hw.special = unchecked((byte)(data));
            updateLastBank = 1;
            break;
            }
            default:
            {
            if (((address & unchecked((uint)(1))) != 0)) {
            CPointer<byte> src = (new CPointer<byte>(cart.rom) + unchecked((int)((((data & unchecked((uint)(15))) << 19) & cart.mask))));
            byte bank = unchecked((byte)(((address << 2) & unchecked((uint)(56)))));
            if (((unchecked((int)(bank)) != 0) || (megasd_hw.unlock != 0))) {
            for (i = 0; (i < 8); i++) {
            memory_map[(int)((unchecked((int)(bank)) + i))].@base = (src + (i << 16));
            }
            }
            } else {
            m68k_unused_8_w(address, data);
            }
            break;
            }
            }
            if ((updateLastBank != 0)) {
            if ((unchecked((int)(megasd_hw.special)) == 128)) {
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = ((megasd_hw.writeEnable != 0) ? sram_write_byte : m68k_unused_8_w);
            memory_map[(int)(i)].write16 = ((megasd_hw.writeEnable != 0) ? sram_write_word : m68k_unused_16_w);
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = ((megasd_hw.writeEnable != 0) ? sram_write_byte : zbank_unused_w);
            }
            } else if ((unchecked((int)(megasd_hw.special)) == 129)) {
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].@base = default(CPointer<byte>);
            memory_map[(int)(i)].read8 = megasd_pcm_read_byte;
            memory_map[(int)(i)].read16 = megasd_pcm_read_word;
            memory_map[(int)(i)].write8 = ((megasd_hw.writeEnable != 0) ? megasd_pcm_write_byte : m68k_unused_8_w);
            memory_map[(int)(i)].write16 = ((megasd_hw.writeEnable != 0) ? megasd_pcm_write_word : m68k_unused_16_w);
            zbank_memory_map[(int)(i)].read = megasd_pcm_read_byte;
            zbank_memory_map[(int)(i)].write = ((megasd_hw.writeEnable != 0) ? megasd_pcm_write_byte : zbank_unused_w);
            }
            } else {
            CPointer<byte> src = (new CPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)(((unchecked((int)(megasd_hw.special)) & 15) << 19))) & cart.mask))));
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].@base = (src + (i << 16));
            ;
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = ((megasd_hw.writeEnable != 0) ? null : m68k_unused_8_w);
            memory_map[(int)(i)].write16 = ((megasd_hw.writeEnable != 0) ? null : m68k_unused_16_w);
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = ((megasd_hw.writeEnable != 0) ? null : zbank_unused_w);
            ;
            }
            }
            }
        }
    }
    private void megasd_rom_mapper_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            if (((address & unchecked((uint)(255))) == unchecked((uint)(255)))) {
            if ((data == unchecked((uint)(87)))) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            } else {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            memory_map[(int)(3)].write8 = megasd_ctrl_write_byte;
            memory_map[(int)(3)].write16 = megasd_ctrl_write_word;
            }
            } else {
            m68k_unused_8_w(address, data);
            }
        }
    }
    private void megasd_update_cdda(uint samples)
    {
        unchecked
        {
            uint count = default;
            while ((samples > unchecked((uint)(0)))) {
            if ((unchecked((int)(cd_control)) == 1)) {
            cdd_read_audio(samples);
            break;
            }
            count = samples;
            if (((megasd_hw.fadeoutSamplesCount > 0) && (count > unchecked((uint)(megasd_hw.fadeoutSamplesCount))))) {
            count = unchecked((uint)(megasd_hw.fadeoutSamplesCount));
            }
            if (((megasd_hw.playbackSamplesCount > 0) && (count > unchecked((uint)(megasd_hw.playbackSamplesCount))))) {
            count = unchecked((uint)(megasd_hw.playbackSamplesCount));
            }
            cdd_read_audio(count);
            samples -= count;
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            megasd_hw.fadeoutSamplesCount -= unchecked((int)(count));
            if ((megasd_hw.fadeoutSamplesCount <= 0)) {
            cd_control = unchecked((byte)(1));
            cd_status = unchecked((byte)(4));
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = megasd_hw.fadeoutStartVolume);
            } else {
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = unchecked((ushort)(((megasd_hw.fadeoutSamplesCount * unchecked((int)(megasd_hw.fadeoutStartVolume))) / megasd_hw.fadeoutSamplesTotal))));
            }
            }
            if ((megasd_hw.playbackSamplesCount > 0)) {
            megasd_hw.playbackSamplesCount -= unchecked((int)(count));
            if ((megasd_hw.playbackSamplesCount <= 0)) {
            if ((cd_index < unchecked((int)(megasd_hw.playbackEndTrack)))) {
            cdd_seek_audio((cd_index + 1), TrackStart((cd_index + 1)));
            cd_index++;
            if ((cd_index == unchecked((int)(megasd_hw.playbackEndTrack)))) {
            megasd_hw.playbackSamplesCount = ((megasd_hw.playbackEndSector - TrackStart(cd_index)) * 588);
            } else {
            megasd_hw.playbackSamplesCount = ((TrackEnd(cd_index) - TrackStart(cd_index)) * 588);
            }
            } else if ((megasd_hw.playbackLoop != 0)) {
            cdd_seek_audio(unchecked((int)(megasd_hw.playbackLoopTrack)), megasd_hw.playbackLoopSector);
            cd_index = unchecked((int)(megasd_hw.playbackLoopTrack));
            if ((cd_index == unchecked((int)(megasd_hw.playbackEndTrack)))) {
            megasd_hw.playbackSamplesCount = ((megasd_hw.playbackEndSector - megasd_hw.playbackLoopSector) * 588);
            } else {
            megasd_hw.playbackSamplesCount = ((TrackEnd(cd_index) - megasd_hw.playbackLoopSector) * 588);
            }
            } else {
            cd_status = unchecked((byte)(0));
            cd_control = unchecked((byte)(1));
            }
            }
            }
            }
        }
    }
    private void megasd_ctrl_write_byte(uint address, uint data)
    {
        unchecked
        {
            if ((megasd_hw.overlayEnable != 0)) {
            if ((address >= unchecked((uint)(260096)))) {
            megasd_hw.buffer[(int)((address & unchecked((uint)(2047))))] = unchecked((byte)(data));
            return;
            }
            }
            m68k_unused_8_w(address, data);
        }
    }
    private void megasd_ctrl_write_word(uint address, uint data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(260090)))) {
            megasd_hw.overlayEnable = unchecked((byte)(((data == unchecked((uint)(52564))) ? 1 : 0)));
            return;
            }
            if ((megasd_hw.overlayEnable != 0)) {
            if ((address == unchecked((uint)(260094)))) {
            switch (((data >> 8) & unchecked((uint)(255)))) {
            case 16:
            {
            CopyMemory(new CPointer<byte>(megasd_hw.buffer), new CPointer<byte>(megasd_version), 16);
            return;
            }
            case 17:
            case 18:
            case 26:
            {
            if ((cd_loaded != 0)) {
            int index = unchecked((int)(((data & unchecked((uint)(255))) - unchecked((uint)(1)))));
            if ((((index >= 0) && (index < track_count)) && (!(TrackType(index) != 0)))) {
            megasd_hw.playbackEndTrack = unchecked((byte)(index));
            megasd_hw.playbackEndSector = TrackEnd(index);
            cdd_seek_audio(index, TrackStart(index));
            cd_index = index;
            cd_status = unchecked((byte)(1));
            cd_control = unchecked((byte)(0));
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            megasd_hw.fadeoutSamplesCount = 0;
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = megasd_hw.fadeoutStartVolume);
            }
            megasd_hw.playbackSamplesCount = ((TrackEnd(index) - TrackStart(index)) * 588);
            if ((TrackLoopEnabled(index) != 0)) {
            megasd_hw.playbackLoop = unchecked((byte)((TrackLoopEnabled(index) + 1)));
            megasd_hw.playbackLoopSector = (TrackStart(index) + TrackLoopOffset(index));
            } else {
            megasd_hw.playbackLoop = unchecked((byte)(((data >> 8) & unchecked((uint)(2)))));
            if (((data >> 8) == unchecked((uint)(26)))) {
            megasd_hw.playbackLoopSector = unchecked((int)((unchecked((uint)(TrackStart(index))) + new CPointer<byte>(megasd_hw.buffer).Cast<uint>()[0])));
            } else {
            megasd_hw.playbackLoopSector = TrackStart(index);
            }
            }
            if (((megasd_hw.playbackLoopSector < TrackStart(index)) || (megasd_hw.playbackLoopSector >= TrackEnd(index)))) {
            megasd_hw.playbackLoopSector = TrackStart(index);
            }
            megasd_hw.playbackLoopTrack = unchecked((byte)(index));
            }
            }
            return;
            }
            case 19:
            {
            if ((unchecked((int)(cd_control)) == 0)) {
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = megasd_hw.fadeoutStartVolume);
            }
            megasd_hw.fadeoutSamplesCount = unchecked((int)(((data & unchecked((uint)(255))) * unchecked((uint)(588)))));
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            megasd_hw.fadeoutSamplesTotal = megasd_hw.fadeoutSamplesCount;
            megasd_hw.fadeoutStartVolume = cd_fader[(int)(0)];
            } else {
            cd_control = unchecked((byte)(1));
            cd_status = unchecked((byte)(4));
            }
            }
            return;
            }
            case 20:
            {
            if ((unchecked((int)(cd_status)) == 4)) {
            cd_control = unchecked((byte)(0));
            cd_status = unchecked((byte)(1));
            }
            return;
            }
            case 21:
            {
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            megasd_hw.fadeoutStartVolume = unchecked((ushort)((((data & unchecked((uint)(255))) * unchecked((uint)(1024))) / unchecked((uint)(255)))));
            } else {
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = unchecked((ushort)((((data & unchecked((uint)(255))) * unchecked((uint)(1024))) / unchecked((uint)(255))))));
            }
            return;
            }
            case 22:
            {
            megasd_hw.result = unchecked((ushort)(((unchecked((int)(cd_control)) == 0) ? 1 : 0)));
            return;
            }
            case 23:
            {
            if (((cd_loaded != 0) && (TrackType(0) != 0))) {
            int lba = unchecked((int)((((((unchecked((uint)(megasd_hw.buffer[(int)(0)])) << 24) + unchecked((uint)((unchecked((int)(megasd_hw.buffer[(int)(1)])) << 16)))) + unchecked((uint)((unchecked((int)(megasd_hw.buffer[(int)(2)])) << 8)))) + unchecked((uint)(megasd_hw.buffer[(int)(3)]))) - unchecked((uint)(150)))));
            if (((lba >= 0) && (lba < TrackEnd(0)))) {
            cd_lba = lba;
            cd_index = 0;
            cd_status = unchecked((byte)(1));
            cd_control = unchecked((byte)(1));
            }
            }
            return;
            }
            case 24:
            {
            if ((((cd_loaded != 0) && (unchecked((int)(cd_status)) == 1)) && (TrackType(cd_index) != 0))) {
            cdd_read_data(new CPointer<byte>(megasd_hw.buffer), default(CPointer<byte>));
            }
            return;
            }
            case 25:
            {
            if ((((cd_loaded != 0) && (unchecked((int)(cd_status)) == 1)) && (TrackType(cd_index) != 0))) {
            if ((cd_lba < (TrackEnd(0) - 1))) {
            cd_lba++;
            }
            }
            return;
            }
            case 27:
            {
            if ((cd_loaded != 0)) {
            int lba = (((((unchecked((int)(megasd_hw.buffer[(int)(0)])) << 24) + (unchecked((int)(megasd_hw.buffer[(int)(1)])) << 16)) + (unchecked((int)(megasd_hw.buffer[(int)(2)])) << 8)) + unchecked((int)(megasd_hw.buffer[(int)(3)]))) - 150);
            int index = 0;
            while (((TrackEnd(index) <= lba) && (index < track_count))) {
            index++;
            }
            if (((index < track_count) && (TrackType(index) == 0))) {
            if ((lba < TrackStart(index))) {
            lba = TrackStart(index);
            }
            cdd_seek_audio(index, lba);
            cd_index = index;
            cd_status = unchecked((byte)(1));
            cd_control = unchecked((byte)(0));
            if ((megasd_hw.fadeoutSamplesCount > 0)) {
            megasd_hw.fadeoutSamplesCount = 0;
            cd_fader[(int)(0)] = (cd_fader[(int)(1)] = megasd_hw.fadeoutStartVolume);
            }
            megasd_hw.playbackEndSector = unchecked((int)(((new CPointer<byte>(megasd_hw.buffer) + 4).Cast<uint>()[0] - unchecked((uint)(150)))));
            if ((megasd_hw.playbackEndSector > lba)) {
            megasd_hw.playbackEndTrack = unchecked((byte)(index));
            while (((TrackEnd(unchecked((int)(megasd_hw.playbackEndTrack))) <= megasd_hw.playbackEndSector) && (unchecked((int)(megasd_hw.playbackEndTrack)) < track_count))) {
            megasd_hw.playbackEndTrack++;
            }
            if ((unchecked((int)(megasd_hw.playbackEndTrack)) == track_count)) {
            megasd_hw.playbackEndSector = TrackEnd((track_count - 1));
            megasd_hw.playbackEndTrack = unchecked((byte)((track_count - 1)));
            }
            } else {
            megasd_hw.playbackEndSector = TrackEnd(index);
            megasd_hw.playbackEndTrack = unchecked((byte)(index));
            }
            if ((unchecked((int)(megasd_hw.playbackEndTrack)) == index)) {
            megasd_hw.playbackSamplesCount = ((megasd_hw.playbackEndSector - lba) * 588);
            } else {
            megasd_hw.playbackSamplesCount = ((TrackEnd(index) - lba) * 588);
            }
            if ((TrackLoopEnabled(index) != 0)) {
            megasd_hw.playbackLoop = unchecked((byte)((TrackLoopEnabled(index) + 1)));
            megasd_hw.playbackLoopSector = (TrackStart(index) + TrackLoopOffset(index));
            } else {
            megasd_hw.playbackLoop = unchecked((byte)((data & unchecked((uint)(1)))));
            if ((megasd_hw.playbackLoop != 0)) {
            megasd_hw.playbackLoopSector = unchecked((int)(((new CPointer<byte>(megasd_hw.buffer) + 8).Cast<uint>()[0] - unchecked((uint)(150)))));
            } else {
            megasd_hw.playbackLoopSector = TrackStart(index);
            }
            }
            if ((((megasd_hw.playbackLoopSector < TrackStart(index)) || (megasd_hw.playbackLoopSector >= TrackEnd(index))) || (megasd_hw.playbackLoopSector >= megasd_hw.playbackEndSector))) {
            megasd_hw.playbackLoopSector = TrackStart(index);
            }
            megasd_hw.playbackLoopTrack = unchecked((byte)(index));
            }
            }
            return;
            }
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
            {
            megasd_hw.result = unchecked((ushort)(0));
            return;
            }
            default:
            {
            m68k_unused_16_w(address, data);
            return;
            }
            }
            }
            if ((address >= unchecked((uint)(260096)))) {
            megasd_hw.buffer[(int)((address & unchecked((uint)(2046))))] = unchecked((byte)(((data >> 8) & unchecked((uint)(255)))));
            megasd_hw.buffer[(int)(((address & unchecked((uint)(2046))) + unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            return;
            }
            }
            m68k_unused_16_w(address, data);
        }
    }
    private uint megasd_ctrl_read_byte(uint address)
    {
        unchecked
        {
            if ((megasd_hw.overlayEnable != 0)) {
            if (((address >= unchecked((uint)(260086))) && (address <= unchecked((uint)(260089))))) {
            return unchecked((uint)(megasd_id[(int)((address & unchecked((uint)(3))))]));
            }
            if (((address >= unchecked((uint)(260090))) && (address <= unchecked((uint)(260091))))) {
            return unchecked((uint)((((address & unchecked((uint)(1))) != 0) ? 84 : 205)));
            }
            if (((address >= unchecked((uint)(260092))) && (address <= unchecked((uint)(260093))))) {
            return unchecked((uint)((((address & unchecked((uint)(1))) != 0) ? (unchecked((int)(megasd_hw.result)) & 255) : (unchecked((int)(megasd_hw.result)) >> 8))));
            }
            if (((address >= unchecked((uint)(260094))) && (address <= unchecked((uint)(260095))))) {
            return unchecked((uint)(0));
            }
            if ((address >= unchecked((uint)(260096)))) {
            return unchecked((uint)(megasd_hw.buffer[(int)((address & unchecked((uint)(2047))))]));
            }
            }
            return unchecked((uint)(memory_map[(int)(3)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint megasd_ctrl_read_word(uint address)
    {
        unchecked
        {
            if ((megasd_hw.overlayEnable != 0)) {
            if (((address == unchecked((uint)(260086))) || (address == unchecked((uint)(260088))))) {
            return unchecked((uint)(((unchecked((int)(megasd_id[(int)((address - unchecked((uint)(260086))))])) << 8) | unchecked((int)(megasd_id[(int)(((address - unchecked((uint)(260086))) + unchecked((uint)(1))))])))));
            }
            if ((address == unchecked((uint)(260090)))) {
            return unchecked((uint)(52564));
            }
            if ((address == unchecked((uint)(260092)))) {
            return unchecked((uint)(megasd_hw.result));
            }
            if ((address == unchecked((uint)(260094)))) {
            return unchecked((uint)(0));
            }
            if ((address >= unchecked((uint)(260096)))) {
            return unchecked((uint)(((unchecked((int)(megasd_hw.buffer[(int)((address & unchecked((uint)(2046))))])) << 8) | unchecked((int)(megasd_hw.buffer[(int)(((address & unchecked((uint)(2046))) + unchecked((uint)(1))))])))));
            }
            }
            return unchecked((uint)((memory_map[(int)(3)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void megasd_pcm_write_byte(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            pcm_write(((address >> 1) & unchecked((uint)(8191))), unchecked((byte)(data)), ((main_cycles * cd_cycles_per_line) / unchecked((uint)(3420))));
            return;
            }
            m68k_unused_8_w(address, data);
            return;
        }
    }
    private void megasd_pcm_write_word(uint address, uint data)
    {
        unchecked
        {
            pcm_write(((address >> 1) & unchecked((uint)(8191))), unchecked((byte)((data & unchecked((uint)(255))))), ((main_cycles * cd_cycles_per_line) / unchecked((uint)(3420))));
        }
    }
    private uint megasd_pcm_read_byte(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(pcm_read(((address >> 1) & unchecked((uint)(8191))), ((main_cycles * cd_cycles_per_line) / unchecked((uint)(3420))))));
            }
            return unchecked((uint)(0));
        }
    }
    private uint megasd_pcm_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)(pcm_read(((address >> 1) & unchecked((uint)(8191))), ((main_cycles * cd_cycles_per_line) / unchecked((uint)(3420))))));
        }
    }
}
