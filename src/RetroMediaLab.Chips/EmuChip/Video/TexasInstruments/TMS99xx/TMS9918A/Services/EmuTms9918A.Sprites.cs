using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class EmuTms9918A
{
    private readonly int[] _selectedSprites = new int[5];
    private readonly Tms9918SpriteChannelState[] _preparingSprites = new Tms9918SpriteChannelState[4];
    private readonly Tms9918SpriteChannelState[] _activeSprites = new Tms9918SpriteChannelState[4];
    private int _spriteScanLine = -1, _spriteSelectedCount, _preparingSpriteLine = -1, _activeSpriteLine = -1;
    private bool _spriteScanTerminated, _spriteScanKnown, _preparingSpritesKnown, _activeSpritesKnown;

    internal Tms9918SpriteState CaptureSpriteState() => new(_spriteScanLine, _spriteSelectedCount,
        _spriteScanTerminated, _spriteScanKnown, (int[])_selectedSprites.Clone(),
        _preparingSpriteLine, _preparingSpritesKnown, (Tms9918SpriteChannelState[])_preparingSprites.Clone(),
        _activeSpriteLine, _activeSpritesKnown, (Tms9918SpriteChannelState[])_activeSprites.Clone());

    private bool SelectSpriteAddress(Tms9918VramSlot slot, int line, out Tms9918FetchAddress address)
    {
        address = default;
        if (slot.Kind == K.SpriteVisibility)
        {
            if (slot.Index == 0)
            {
                _spriteScanLine = (line + 2) % 262;
                _spriteSelectedCount = 0;
                _spriteScanTerminated = false;
                _spriteScanKnown = true;
            }
            if (_spriteScanTerminated) return false;
            address = new((ushort)(_registers.SpriteAttributeBase | slot.Index << 2), true);
            return true;
        }
        int channel = slot.Index;
        if (slot.Kind == K.SpriteY && channel == 0)
        {
            _preparingSpriteLine = _spriteScanLine;
            _preparingSpritesKnown = _spriteScanKnown;
            Array.Clear(_preparingSprites);
            for (int index = 0; index < Math.Min(_spriteSelectedCount, 4); index++)
                _preparingSprites[index] = new() { Selected = true };
        }
        if (!_preparingSprites[channel].Selected) return false;
        var sprite = _preparingSprites[channel];
        if (slot.Kind is K.SpritePatternLeft or K.SpritePatternRight)
        {
            if (slot.Kind == K.SpritePatternRight && !_registers.LargeSprites) return false;
            int name = _registers.LargeSprites ? sprite.Name & 0xfc : sprite.Name;
            int row = sprite.PatternRow & (_registers.LargeSprites ? 15 : 7);
            int offset = (name << 3) | row;
            if (slot.Kind == K.SpritePatternRight) offset |= 16;
            address = new((ushort)(_registers.SpritePatternBase | offset), (sprite.KnownMask & 5) == 5);
        }
        else
        {
            int attribute = slot.Kind switch { K.SpriteY => 0, K.SpriteX => 1, K.SpriteName => 2, K.SpriteColour => 3,
                _ => throw new InvalidOperationException("Invalid sprite VRAM destination.") };
            address = new((ushort)(_registers.SpriteAttributeBase | _selectedSprites[channel] << 2 | attribute), true);
        }
        return true;
    }

    private void LoadSpriteByte(Tms9918VramSlot slot, byte value, bool known)
    {
        if (slot.Kind == K.SpriteVisibility)
        {
            if (_spriteScanTerminated) return;
            if (_spriteScanLine is >= 0 and <= 192) _status.SpriteCounterAdvanced(slot.Index);
            _spriteScanKnown &= known;
            if (!known) return;
            if (value == 0xd0) { _spriteScanTerminated = true; return; }
            if (_spriteScanLine is < 0 or > 192) return;
            int height = (_registers.LargeSprites ? 16 : 8) * (_registers.MagnifiedSprites ? 2 : 1);
            int range = _registers.SpriteRows.Offset(slot.Index, _spriteScanLine, value, height);
            if (range < 0 || range >= height) return;
            _selectedSprites[_spriteSelectedCount++] = slot.Index;
            if (_spriteSelectedCount == 5)
            {
                if (_spriteScanKnown) _status.FifthSpriteDetected(slot.Index);
                _spriteScanTerminated = true;
            }
            return;
        }
        int index = slot.Index;
        var sprite = _preparingSprites[index];
        int bit;
        switch (slot.Kind)
        {
            case K.SpriteY:
                int height = (_registers.LargeSprites ? 16 : 8) * (_registers.MagnifiedSprites ? 2 : 1);
                int range = _registers.SpriteRows.Offset(_selectedSprites[index], _preparingSpriteLine, value, height);
                int row = (range >> (_registers.MagnifiedSprites ? 1 : 0)) & (_registers.LargeSprites ? 15 : 7);
                sprite = sprite with { Y = value, PatternRow = row }; bit = 1; break;
            case K.SpriteX: sprite = sprite with { X = value }; bit = 2; break;
            case K.SpriteName: sprite = sprite with { Name = value }; bit = 4; break;
            case K.SpriteColour: sprite = sprite with { Tag = value }; bit = 8; break;
            case K.SpritePatternLeft: sprite = sprite with { Left = value }; bit = 16; break;
            case K.SpritePatternRight: sprite = sprite with { Right = value }; bit = 32; break;
            default: throw new InvalidOperationException("Invalid sprite RAM load.");
        }
        _preparingSprites[index] = sprite with
            { KnownMask = (byte)(known ? sprite.KnownMask | bit : sprite.KnownMask & ~bit) };
    }

    private void BeginSpriteLine(int line)
    {
        Array.Copy(_preparingSprites, _activeSprites, 4);
        _activeSpriteLine = _preparingSpriteLine;
        _activeSpritesKnown = _preparingSpritesKnown && _activeSpriteLine == line;
    }

    private Tms9918Pixel SpritePixel(int dot, int line)
    {
        if (!_registers.DisplayEnabled || _registers.UsesTextTiming
            || _registers.Mode == Tms9918DisplayMode.UndocumentedCombination) return new(0, true);
        bool next = dot >= 310;
        int targetLine = next ? (line + 1) % 262 : line;
        if (targetLine > 192) return new(0, true);
        var rows = next ? _preparingSprites : _activeSprites;
        bool known = next ? _preparingSpritesKnown && _preparingSpriteLine == targetLine : _activeSpritesKnown;
        bool selectionKnown = known;
        int x = next ? dot - 342 : dot;
        int size = _registers.LargeSprites ? 16 : 8;
        int scale = _registers.MagnifiedSprites ? 2 : 1;
        int coincidence = 0;
        byte colour = 0;
        bool colourChosen = false;
        int required = _registers.LargeSprites ? 63 : 31;
        for (int index = 0; index < 4; index++)
        {
            var sprite = rows[index];
            if (!sprite.Selected) continue;
            if ((sprite.KnownMask & 10) != 10) { known = false; continue; }
            bool channelKnown = (sprite.KnownMask & required) == required;
            int start = sprite.X - ((sprite.Tag & 0x80) != 0 ? 32 : 0);
            int position = x - start;
            if (position < 0 || position >= size * scale) continue;
            known &= channelKnown;
            position /= scale;
            int pattern = position < 8 ? sprite.Left : sprite.Right;
            if ((pattern & (0x80 >> (position & 7))) == 0) continue;
            if (channelKnown) coincidence++;
            byte candidate = (byte)(sprite.Tag & 15);
            if (!colourChosen && candidate != 0)
            { colour = candidate; colourChosen = true; }
        }
        if (selectionKnown && coincidence > 1) _status.SpriteCoincidence();
        return new(colour, known);
    }

    private void ResetSpritePipeline()
    {
        _spriteScanLine = _preparingSpriteLine = _activeSpriteLine = -1;
        _spriteSelectedCount = 0;
        _spriteScanKnown = _spriteScanTerminated = _preparingSpritesKnown = _activeSpritesKnown = false;
        Array.Clear(_selectedSprites); Array.Clear(_preparingSprites); Array.Clear(_activeSprites);
    }
}
