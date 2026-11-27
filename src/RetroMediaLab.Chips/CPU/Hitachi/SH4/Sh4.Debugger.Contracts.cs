namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed class Sh4DebuggerStopException : Exception
{
}

internal sealed partial class Sh4Debugger
{
    internal const int DefaultPort = 3263;

    internal void Trap(Sh4ExceptionEvent exception) => Trap((uint)exception);

#if GDB_SERVER
    internal partial void Initialize(int port);
    internal partial void Terminate();
    internal partial void Run();
    internal partial void Trap(uint exception);
    internal partial void SubroutineCall();
    internal partial void SubroutineReturn();
#else
    internal void Initialize(int port) { }
    internal void Terminate() { }
    internal void Run() { }
    internal void Trap(uint exception) { }
    internal void SubroutineCall() { }
    internal void SubroutineReturn() { }
#endif
}
