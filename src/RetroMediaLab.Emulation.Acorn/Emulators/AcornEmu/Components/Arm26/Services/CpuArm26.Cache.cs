using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Constants.Arm26Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Services;

internal sealed partial class CpuArm26
{
    // Cache unifié virtuel : quatre ensembles, 64 voies, lignes de quatre mots.
    // Une entrée contient réellement les données : une écriture DMA ne devient
    // visible qu'après invalidation, comme sur le composant sans cohérence DMA.
    private readonly uint[] _cacheWords = new uint[C.CacheSets * C.CacheWays * C.CacheLineWords];
    private readonly uint[] _cacheTags = new uint[C.CacheSets * C.CacheWays];
    private readonly bool[] _cacheValid = new bool[C.CacheSets * C.CacheWays];
    private readonly Dictionary<uint, int> _cacheIndex = new(C.CacheSets * C.CacheWays);
    private uint _cacheControl;
    private uint _cacheable;
    private uint _cacheUpdateable;
    private uint _cacheDisruptive;
    private int _cacheReplacement = C.CacheReplacementInitial;
    private bool _busLocked;
    private int _cacheFillLine = C.NoPendingCacheLine;
    private int _cacheFillNextWord;
    private uint _cacheFillTag;
    private uint _cacheFillAddress;
    private bool _cacheFillUser;
    private bool _cacheFillInstruction;
    private bool _cacheFillAborted;

    private void ResetCache()
    {
        _cacheControl = _cacheable = _cacheUpdateable = _cacheDisruptive = 0;
        _cacheReplacement = C.CacheReplacementInitial;
        _busLocked = false;
        _cacheFillLine = C.NoPendingCacheLine;
        FlushCache();
    }

    private void FlushCache()
    {
        CompleteCacheFill();
        _cacheIndex.Clear();
        Array.Clear(_cacheValid);
    }

    private uint CacheTag(uint address, bool user) => (address & ~C.CacheLineMask) |
        (user && (_cacheControl & C.CacheShared) == 0 ? C.CacheUserTag : 0);

    private static uint CacheRegion(uint address) => 1u << (int)((address & C.AddressByteMask) >> C.CacheRegionShift);

    private bool CachedByteAddress(uint address, bool user) =>
        (_cacheable & CacheRegion(address)) != 0 || _cacheIndex.ContainsKey(CacheTag(address, user));

    private bool ReadCachedWord(uint address, bool user, bool instruction, bool sequential, out uint value)
    {
        var tag = CacheTag(address, user);
        var word = (int)((address & C.CacheLineMask) / C.WordBytes);
        if (_cacheFillLine != C.NoPendingCacheLine)
        {
            if (sequential && tag == _cacheFillTag)
            {
                var pendingLine = _cacheFillLine;
                if (word < _cacheFillNextWord) InternalCycle();
                else ReadCacheFillThrough(word);
                value = _cacheWords[pendingLine * C.CacheLineWords + word];
                return true;
            }
            CompleteCacheFill();
        }
        if (_cacheIndex.TryGetValue(tag, out var line))
        {
            InternalCycle();
            value = _cacheWords[line * C.CacheLineWords + word];
            return true;
        }
        if ((_cacheable & CacheRegion(address)) == 0)
        {
            Cycles++;
            return bus.TryReadWord(address, sequential, user, instruction, out value);
        }
        var set = (int)((address >> C.CacheLineShift) & (C.CacheSets - 1));
        line = set * C.CacheWays + _cacheReplacement;
        if (_cacheValid[line]) _cacheIndex.Remove(_cacheTags[line]);
        _cacheValid[line] = false;
        _cacheFillLine = line;
        _cacheFillNextWord = 0;
        _cacheFillTag = tag;
        _cacheFillAddress = address & ~C.CacheLineMask;
        _cacheFillUser = user;
        _cacheFillInstruction = instruction;
        _cacheFillAborted = false;
        // Générateur de remplacement pseudo-aléatoire à six bits de l'ARM3.
        if (((_cacheReplacement ^ (_cacheReplacement >> 1)) & 1) == 0)
            _cacheReplacement |= C.CacheReplacementFeedback;
        _cacheReplacement >>= 1;
        ReadCacheFillThrough(word);
        value = _cacheFillAborted ? 0 : _cacheWords[line * C.CacheLineWords + word];
        return !_cacheFillAborted;
    }

    private void ReadCacheFillThrough(int word)
    {
        while (_cacheFillLine != C.NoPendingCacheLine && _cacheFillNextWord <= word) ReadPendingCacheWord();
    }

    private void CompleteCacheFill()
    {
        while (_cacheFillLine != C.NoPendingCacheLine) ReadPendingCacheWord();
    }

    private void ReadPendingCacheWord()
    {
        var line = _cacheFillLine;
        var word = _cacheFillNextWord;
        Cycles++;
        var success = bus.TryReadWord(_cacheFillAddress + (uint)(word * C.WordBytes), word != 0,
            _cacheFillUser, _cacheFillInstruction, out var data);
        // ARM3 samples ABORT only for the aligned first word of a line fetch.
        // The remaining words share its page and access permissions.
        if (word == 0 && !success)
        {
            _cacheFillAborted = true;
            _cacheFillLine = C.NoPendingCacheLine;
            return;
        }
        _cacheWords[line * C.CacheLineWords + word] = data;
        if (++_cacheFillNextWord != C.CacheLineWords) return;
        _cacheTags[line] = _cacheFillTag;
        _cacheValid[line] = true;
        _cacheIndex.Add(_cacheFillTag, line);
        _cacheFillLine = C.NoPendingCacheLine;
    }

    private void UpdateCache(uint address, uint value, bool octet, bool user)
    {
        var region = CacheRegion(address);
        if ((_cacheDisruptive & region) != 0) { FlushCache(); return; }
        if ((_cacheControl & C.CacheEnable) == 0 || (_cacheUpdateable & region) == 0 ||
            !_cacheIndex.TryGetValue(CacheTag(address, user), out var line)) return;
        var index = line * C.CacheLineWords + (int)((address & C.CacheLineMask) / C.WordBytes);
        if (!octet) _cacheWords[index] = value;
        else
        {
            var shift = (int)(address & C.WordAlignmentMask) * C.ByteBits;
            _cacheWords[index] = (_cacheWords[index] & ~(C.ByteMask << shift)) | ((value & C.ByteMask) << shift);
        }
    }

    private void CoprocessorInstruction(uint instruction)
    {
        if (model != Arm26Model.Arm3 || Mode == Arm26Mode.User ||
            (instruction & C.CoprocessorRegisterTransferMask) != C.CoprocessorRegisterTransferPattern ||
            ((instruction >> C.CoprocessorNumberShift) & C.RegisterMask) != C.CacheCoprocessor)
        {
            UndefinedInstruction();
            return;
        }
        var register = (int)((instruction >> C.BaseRegisterShift) & C.RegisterMask);
        var armRegister = (int)((instruction >> C.DestinationRegisterShift) & C.RegisterMask);
        InternalCycle();
        if ((instruction & C.Load) != 0)
        {
            var value = register switch
            {
                C.CacheIdRegister => C.Arm3Identification,
                C.CacheControlRegister => _cacheControl,
                C.CacheableRegister => _cacheable,
                C.CacheUpdateableRegister => _cacheUpdateable,
                C.CacheDisruptiveRegister => _cacheDisruptive,
                _ => 0u
            };
            if (armRegister == C.ProgramCounter) SetStatus(value);
            else _registers[armRegister] = value;
            return;
        }
        var written = Operand(armRegister);
        CompleteCacheFill();
        switch (register)
        {
            case C.CacheFlushRegister: FlushCache(); break;
            case C.CacheControlRegister: _cacheControl = written; break;
            case C.CacheableRegister: _cacheable = written; break;
            case C.CacheUpdateableRegister: _cacheUpdateable = written; break;
            case C.CacheDisruptiveRegister: _cacheDisruptive = written; break;
        }
    }
}
