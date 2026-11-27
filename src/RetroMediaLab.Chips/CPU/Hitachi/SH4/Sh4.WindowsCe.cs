namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Mmu
{
    private bool ResolveWindowsCeAddress(uint address, ref Sh4TlbEntry entry)
    {
        if ((address & 0x80000000) == 0)
        {
            uint tableBase = _registers.TranslationTableBase;
            uint group = ReadWindowsCeRam32(unchecked(tableBase + ((address >> 25) << 2)));
            uint page = ((address >> 16) & 0x1FF) << 2;
            uint physicalAddress = ReadWindowsCeRam32(unchecked(group + page));

            if ((physicalAddress & 0x80000000) != 0)
            {
                uint owner = ReadWindowsCeRam32(unchecked(_context.BankedRegisters[4] + 0x14));
                if (owner != ReadWindowsCeRam32(physicalAddress))
                {
                    uint entryData = ReadWindowsCeRam32(unchecked(physicalAddress + 12 + ((address >> 10) & 0x3C)));
                    if (entryData != 0)
                    {
                        entry.Data = entryData - 1;
                        entry.Address = (entry.Address & ~0xFFu) | (_registers.PageTableEntryHigh & 0xFF);
                        entry.Assistance = 0;
                        entry.VirtualPageNumber = (address & PageMasks[entry.PageSize]) >> 10;
                        return true;
                    }
                }
            }
        }
        else if ((address >> 26 & 0x3F) == 0x38)
        {
            uint offset = (address - 0xE0000000) & 0xFFF00000;
            uint entryData = 0x0E0001F5 + offset;
            entry.Data = entryData - 1;
            entry.Assistance = entryData >> 29;
            entry.Address = (entry.Address & ~0xFFu) | (_registers.PageTableEntryHigh & 0xFF);
            entry.VirtualPageNumber = (address & PageMasks[entry.PageSize]) >> 10;
            return true;
        }
        return false;
    }

    private unsafe uint ReadWindowsCeRam32(uint address) =>
        *(uint*)(_memory.RamBuffer + (address & _memory.RamMask));

#if TRACE_WINCE_SYSCALLS
    private const uint FirstMethod = 0xFFFFFE01;
    private const uint ApiCallScale = 2;
    private const uint UserKernelData = 0x5800;
    private const uint SystemHandleOffset = 0x004;
    private const uint SystemHandleBase = 64;
    private const uint Win32Handle = 0;
    private const uint CurrentThreadHandle = 1;
    private const uint CurrentProcessHandle = 2;
    private uint _unresolvedAsciiString;
    private uint _unresolvedUnicodeString;

    private static readonly string[] WindowsCeApis =
    [
        "WIN32", "CURTHREAD", "CURPROC", "WDMAPI", "EVENT", "MUTEX", "APISET", "FILE", "FIND", "DBFILE", "DBFIND", "SOCKET",
            "INTERFACE", "SEMAPHORE", "FSMAP", "WNETENUM", "GDI", "WMGR", "INIT", "COMM", "FILESYS", "SHELL", "DEVMGR", "TAPI",
            "PATCHER", "IMM", "WNET", "?", "?", "?", "?", "USER"
    ];

    private static readonly string[][] WindowsCeMethods =
    [
        ["Nop", "N/S", "CreateAPISet", "VirtualAlloc", "VirtualFree", "VirtualProtect", "VirtualQuery", "VirtualCopy", "LoadLibraryW",
            "FreeLibrary", "GetProcAddressW", "ThreadAttachAllDLLs", "ThreadDetachAllDLLs", "GetTickCount", "OutputDebugStringW",
            "TlsCall", "GetSystemInfo", "(ropen)", "(rread)", "(rwrite)", "(rlseek)", "(rclose)", "RegisterDbgZones", "NKvDbgPrintfW",
            "ProfileSyscall", "FindResource", "LoadResource", "SizeofResource", "GetRealTime", "SetRealTime", "ProcessDetachAllDLLs",
            "ExtractResource", "GetRomFileInfo", "GetRomFileBytes", "CacheRangeFlush", "AddTrackedItem", "DeleteTrackedItem",
            "PrintTrackedItem", "GetKPhys", "GiveKPhys", "SetExceptionHandler", "RegisterTrackedItem", "FilterTrackedItem",
            "SetKernelAlarm", "RefreshKernelAlarm", "CeGetRandomSeed", "CloseProcOE", "SetGwesOOMEvent", "StringCompress",
            "StringDecompress", "BinaryCompress", "BinaryDecompress", "CreateEvent", "CreateProc", "CreateThread", "InputDebugCharW",
            "TakeCritSec", "LeaveCritSec", "WaitForMultiple", "MapPtrToProcess", "MapPtrUnsecure", "GetProcFromPtr", "IsBadPtr",
            "GetProcAddrBits", "GetFSHeapInfo", "OtherThreadsRunning", "KillAllOtherThreads", "GetOwnerProcess", "GetCallerProcess",
            "GetIdleTime", "SetLowestScheduledPriority", "IsPrimaryThread", "SetProcPermissions", "GetCurrentPermissions",
            "(74?)", "SetDaylightTime", "SetTimeZoneBias", "SetCleanRebootFlag", "CreateCrit", "PowerOffSystem", "CreateMutex",
            "SetDbgZone", "Sleep", "TurnOnProfiling", "TurnOffProfiling", "CeGetCurrentTrust", "CeGetCallerTrust", "NKTerminateThread",
            "SetLastError", "GetLastError", "GetProcName", "TerminateSelf", "CloseAllHandles", "SetHandleOwner", "LoadDriver",
            "CreateFileMapping", "UnmapViewOfFile", "FlushViewOfFile", "CreateFileForMappingW", "KernelIoControl", "GetThreadCallStack",
            "PPSHRestart", "(102?)", "UpdateNLSInfo", "ConnectDebugger", "InterruptInitialize", "InterruptDone", "InterruptDisable",
            "SetKMode", "SetPowerOffHandler", "SetGwesPowerHandler", "SetHardwareWatch", "QueryAPISetID", "PerformCallBack",
            "CaptureContext", "GetCallerProcessIndex", "WaitForDebugEvent", "ContinueDebugEvent", "DebugNotify", "OpenProcess",
            "THCreateSnapshot", "THGrow", "NotifyForceCleanboot", "DumpKCallProfile", "GetProcessVersion", "GetModuleFileNameW",
            "QueryPerformanceCounter", "QueryPerformanceFrequency", "KernExtractIcons", "ForcePageout", "GetThreadTimes", "GetModuleHandleW",
            "SetWDevicePowerHandler", "SetStdioPathW", "GetStdioPathW", "ReadRegistryFromOEM", "WriteRegistryToOEM", "WriteDebugLED",
            "LockPages", "UnlockPages", "VirtualSetAttributes", "SetRAMMode", "SetStoreQueueBase", "FlushViewOfFileMaybe"],
        ["CloseHandle", "(AllHandle_Wait)", "Suspend", "Resume", "SetThreadPrio", "GetThreadPrio", "GetRetCode", "GetContext",
            "SetContext", "Terminate", "CeGetPrio", "CeSetPrio", "CeGetQuant", "CeSetQuant"],
        ["CloseHandle", "(AllHandle_Wait)", "Terminate", "GetRetCode", "FlushICache", "ReadMemory", "WriteMemory", "DebugActive",
            "GetModInfo", "SetVer"],
        [],
        ["CloseHandle", "(AllHandle_Wait)", "Modify", "AddAccess", "GetData", "SetData"],
        ["CloseHandle", "(AllHandle_Wait)", "ReleaseMutex"],
        ["CloseHandle", "(AllHandle_Wait)", "Register", "CreateHandle", "Verify"],
        ["CloseFileHandle", "(NULL)", "ReadFile", "WriteFile", "GetFileSize", "SetFilePointer", "GetFileInformationByHandle",
            "FlushFileBuffers", "GetFileTime", "SetFileTime", "SetEndOfFile", "DeviceIoControl", "ReadFileWithSeek", "WriteFileWithSeek"],
        ["FindClose", "(NULL)", "FindNextFile"],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        ["ProcNotify", "(AllHandle_Wait)", "CreateDirectory", "RemoveDirectory", "MoveFile", "CopyFile", "DeleteFile", "GetFileAttibutes",
            "FindFirstFile", "CreateFile", "CeRegisterFileSystemNotification", "CeRegisterReplNotification", "CeOidGetInfoEx",
            "CeFindFirstDatabaseEx", "CeCreateDatabaseEx", "CeSetDatabaseInfoEx", "CeOpenDatabaseEx", "RegCloseKey", "RegCreateKeyExW",
            "RegDeleteKeyW", "RegDeleteValueW", "RegEnumValueW", "RegEnumKeyExW", "RegOpenKeyExW", "RegQueryInfoKeyW", "RegQueryValueExW",
            "RegSetValueExW", "GetTempPathW", "CeDeleteDatabaseEx", "CheckPassword", "SetPassword", "SetFileAttributesW", "GetStoreInformation",
            "CeGetReplChangeMask", "CeSetReplChangeMask", "CeGetReplChangeBitsEx", "CeClearReplChangeBitsEx", "CeGetReplOtherBitsEx",
            "CeSetReplOtherBitsEx", "GetSystemMemoryDivision", "SetSystemMemoryDivision", "RegCopyFile", "CloseAllFileHandles",
            "PrestoChangoFileName", "RegRestoreFile", "RegisterAFS", "DeregisterAFS", "GetPasswordActive", "SetPasswordActive",
            "RegFlushKey", "NotifyMountedFS", "CeSetReplChangeBitsEx", "RegisterAFSName", "DeregisterAFSName", "GetDiskFreeSpaceExW",
            "RegisterHiddenAFS", "CeChangeDatabaseLCID", "DumpHeap", "CeMountDBVol", "CeEnumDBVolumes", "CeUnmountDBVol", "CeFlushDBVol",
            "CeFreeNotification"],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        ["NotifyCallback", "(unused)", "RegisterClass", "UnregisterClass", "CreateWindowEx", "PostMessage", "PostQuitMessage",
            "SendMessage", "GetMessage", "TranslateMessage", "DispatchMessage", "GetCapture", "SetCapture", "ReleaseCapture",
            "(ununsed)", "(ununsed)", "(ununsed)", "(ununsed)", "(ununsed)", "GetSystemMetrics", "ImageList_GetDragImage",
            "ImageList_GetIconSize", "ImageList_SetIconSize", "ImageList_GetImageInfo", "ImageList_Merge", "ShowCursor", "SetCursorPos",
            "ImageList_CopyDitherImage", "ImageList_DrawIndirect", "ImageList_DragShowNolock", "CWindowManager::WindowFromPoint(tagPOINT)",
            "(unused)"],
    ];

    private bool ReadWindowsCeMemory32(uint address, out uint data)
    {
        if (LookupFull(address, out _, out uint physicalAddress) != Sh4MmuError.None)
        {
            data = default;
            return false;
        }

        data = _memory.Read32WithoutMmu(physicalAddress);
        return true;
    }

    private bool ReadWindowsCeMemory16(uint address, out ushort data)
    {
        if (LookupFull(address, out _, out uint physicalAddress) != Sh4MmuError.None)
        {
            data = default;
            return false;
        }

        data = _memory.ReadMemory16WithoutMmu(physicalAddress);
        return true;
    }

    private bool ReadWindowsCeMemory8(uint address, out byte data)
    {
        if (LookupFull(address, out _, out uint physicalAddress) != Sh4MmuError.None)
        {
            data = default;
            return false;
        }

        data = _memory.Read8WithoutMmu(physicalAddress);
        return true;
    }

    private uint GetWindowsCeThreadId()
    {
        uint address = UserKernelData + SystemHandleOffset + CurrentThreadHandle * 4;
        return ReadWindowsCeMemory32(address, out uint threadId) ? threadId : 0;
    }

    private uint GetWindowsCeProcessId()
    {
        uint address = UserKernelData + SystemHandleOffset + CurrentProcessHandle * 4;
        return ReadWindowsCeMemory32(address, out uint processId) ? processId : 0;
    }

    private string ReadWindowsCeUnicodeString(uint address)
    {
        System.Text.StringBuilder text = new();
        while (true)
        {
            if (!ReadWindowsCeMemory16(address, out ushort value))
            {
                _unresolvedUnicodeString = address;
                return "(page fault)";
            }
            if (value == 0)
                return text.ToString();
            text.Append((char)(byte)value);
            address += 2;
        }
    }

    private string ReadWindowsCeAsciiString(uint address)
    {
        System.Text.StringBuilder text = new();
        while (true)
        {
            if (!ReadWindowsCeMemory8(address++, out byte value))
            {
                _unresolvedAsciiString = address;
                return "(page fault)";
            }
            if (value == 0)
                return text.ToString();
            text.Append((char)value);
        }
    }

    private bool TraceWindowsCeSystemCall(uint address)
    {
        if ((address & 1) == 0)
            return false;
        if (address is 0xFFFFFD5D or 0xFFFFFD05)
            return true;
        if (address == 0xFFFFFDE7)
            return true;

        uint api = address <= FirstMethod ? ((FirstMethod - address) / ApiCallScale) >> 8 & 0x3F : 0;
        uint method = (address <= FirstMethod ? FirstMethod - address : address - FirstMethod) / ApiCallScale & 0xFF;
        string apiName = api < WindowsCeApis.Length ? WindowsCeApis[api] : $"[{api}]";
        string methodName = api < WindowsCeMethods.Length && method < WindowsCeMethods[api].Length
            ? WindowsCeMethods[api][method] : $"[{method}]";
        Console.Write($"WinCE {address:x8} {GetWindowsCeProcessId() & 0xFFFF:x4}.{GetWindowsCeThreadId() & 0xFFFF:x4} {apiName}: {methodName}");
        if (address == 0xFFFFFD51)
            Console.WriteLine($" dwErrCode = {_context.Registers[4]:x}");
        else if (address == 0xFFFFD5EF)
            Console.WriteLine($" lpFileName = {ReadWindowsCeUnicodeString(_context.Registers[4])}");
        else if (address == 0xFFFFFD97)
            Console.WriteLine($" imageName = {ReadWindowsCeUnicodeString(_context.Registers[4])}, commandLine = {ReadWindowsCeUnicodeString(_context.Registers[5])}");
        else if (methodName == "DebugNotify")
            Console.WriteLine($" {_context.Registers[4]:x}, {_context.Registers[5]:x}");
        else if (address == 0xFFFFD5D3)
            Console.WriteLine($" hKey = {_context.Registers[4]:x}, lpSubKey = {ReadWindowsCeUnicodeString(_context.Registers[5])}");
        else if (methodName == "LoadLibraryW")
            Console.WriteLine($" fileName = {ReadWindowsCeUnicodeString(_context.Registers[4])}");
        else if (methodName == "GetProcAddressW")
            Console.WriteLine($" hModule = {_context.Registers[4]:x}, procName = {ReadWindowsCeUnicodeString(_context.Registers[5])}");
        else if (methodName == "NKvDbgPrintfW")
            Console.WriteLine($" fmt = {ReadWindowsCeUnicodeString(_context.Registers[4])}");
        else if (methodName == "OutputDebugStringW")
            Console.WriteLine($" str = {ReadWindowsCeUnicodeString(_context.Registers[4])}");
        else if (methodName == "RegisterAFSName")
            Console.WriteLine($" name = {ReadWindowsCeUnicodeString(_context.Registers[4])}");
        else if (methodName == "CreateAPISet")
            Console.WriteLine($" name = {ReadWindowsCeAsciiString(_context.Registers[4])}");
        else if (methodName == "Register" && apiName == "APISET")
            Console.WriteLine($" p = {_context.Registers[4]:x}, id = {_context.Registers[5]:x}");
        else
            Console.WriteLine();
        return true;
    }

    private void TraceResolvedWindowsCeString(uint address)
    {
        if (_unresolvedUnicodeString == 0 || address != _unresolvedUnicodeString)
            return;
        _unresolvedUnicodeString = 0;
        System.Diagnostics.Trace.WriteLine($"RESOLVED {ReadWindowsCeUnicodeString(address)}");
    }
#endif
}
