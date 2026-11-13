using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM23xx.TMM23256.Services;

public sealed partial class EmuTmm23256
{
    private TimedMemoryOutput<Tmm23256Output>[]? _rangeOutputs;
    private bool _executingRange;
    private uint NativeOutput => (uint)(_output&255)|((_output&Known)!=0?0xff00u:0)|((_output&Driven)!=0?0x10000u:0);

    public MemoryOutputRange<Tmm23256Output> ExecuteInputRange(in MemoryInputRange<Tmm23256Pins> range,bool retainResponseHistory=true)
    {
        range.Validate(_time);
        Span<EmuSignalWord> words=stackalloc EmuSignalWord[range.Changes.Length];
        for(int i=0;i<words.Length;i++)
        {
            ref readonly var input=ref range.Changes[i];
            words[i]=new(input.TimeNanoseconds,EmuMemoryPins.Encode(input.Pins.Address,input.Pins.AddressKnown,input.Pins.CeHigh,input.Pins.OeHigh,true,false));
        }
        long start=_time; var initial=Output;
        var program=new EmuSignalRange(start,range.KnownUntilNanoseconds,0,words);
        var response=ExecuteProgram(in program,retainResponseHistory);
        return new(start,_time,initial,retainResponseHistory?_rangeOutputs.AsSpan(0,response.Responses):[],
            response.Next<0?null:response.Next,Output,retainResponseHistory,response.Responses);
    }

    public EmuMemoryResponse ExecuteProgram(in EmuSignalRange range,bool retainHistory=false)
    {
        range.Validate(_time,MemoryInputRange<Tmm23256Pins>.MaximumChanges);
        if(_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)throw new InvalidOperationException("Exclusive native memory delivery required.");
        var words=range.Words;

        foreach(ref readonly var word in words)
        {
            if((word.Bits&~EmuMemoryPins.AllMask)!=0||(word.Bits&EmuMemoryPins.AddressMask)>=(ulong)_busTiming.Capacity)
                throw new ArgumentOutOfRangeException(nameof(range));
            _=checked(word.Time+_busTiming.MaximumDelay);
        }
        if(retainHistory)_rangeOutputs??=new TimedMemoryOutput<Tmm23256Output>[(MemoryInputRange<Tmm23256Pins>.MaximumChanges+1)*2];
        int index=0,count=0;
        _executingRange=true;
        try
        {
            while(true)
            {
                bool hasInput=index<words.Length;
                long inputTime=hasInput?words[index].Time:range.End;
                InitializeThrough(inputTime);
                if(_nextDue>=0&&_nextDue<=inputTime)
                {
                    if(ResolveOutputAt(_nextDue))
                    { if(retainHistory)_rangeOutputs![count]=new(_time,Output); count++; }
                    continue;
                }
                if(!hasInput)break;
                ref readonly var input=ref words[index++]; _time=input.Time;
                ulong bits=input.Bits;
                ApplyParallelPins(_time,(ushort)bits,(bits&EmuMemoryPins.AddressKnownMask)!=0,
                    (bits&EmuMemoryPins.CsMask)!=0,(bits&EmuMemoryPins.OeMask)!=0);
            }
            _time=range.End;
            return new(NativeOutput,count,_nextDue);
        }
        finally { _announcedDue=_nextDue; _executingRange=false; }
    }
}
