using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM20xx.TMM2016.Services;

public sealed partial class EmuTmm2016
{
    private TimedMemoryOutput<Tmm2016Output>[]? _rangeOutputs;
    private bool _executingRange;

    public MemoryOutputRange<Tmm2016Output> ExecuteInputRange(in MemoryInputRange<Tmm2016Pins> range,bool retainResponseHistory=true)
    {
        range.Validate(_time);
        Span<EmuSignalWord> words=stackalloc EmuSignalWord[range.Changes.Length];
        for(int i=0;i<words.Length;i++)
        {
            ref readonly var input=ref range.Changes[i];
            words[i]=new(input.TimeNanoseconds,PackPins(input.Pins));
        }
        long start=_time; var initial=Output;
        var program=new EmuSignalRange(start,range.KnownUntilNanoseconds,0,words);
        var response=ExecuteProgram(in program,retainResponseHistory);
        return new(start,_time,initial,retainResponseHistory?_rangeOutputs.AsSpan(0,response.Responses):[],
            response.Next<0?null:response.Next,Output,retainResponseHistory,response.Responses);
    }

    public EmuMemoryResponse ReceiveWiredPins(long time, ulong pins)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, _time);
        if (_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("Exclusive native memory delivery required.");
        if ((pins & ~EmuMemoryPins.AllMask) != 0 || (pins & EmuMemoryPins.AddressMask) >= (ulong)_timing.Capacity)
            throw new ArgumentOutOfRangeException(nameof(pins));
        ulong received = (pins & ~(0xffUL << 24)) | (((pins >> 24) & (pins >> 32) & 255) << 24);
        if (received != _pins) _ = checked(time + _maximumDelay);
        _executingRange = true;
        int responses = 0;
        try
        {
            while (_internalNext >= 0 && _internalNext <= time)
                if (ResolveOutputAt(_internalNext)) responses++;
            _time = time;
            ApplyPackedPins(time, received);
            return new(OutputBits, responses, _next);
        }
        finally { _announced = _next; _executingRange = false; }
    }

    public EmuMemoryResponse AdvanceWiredTime(long time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, _time);
        if (_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)
            throw new InvalidOperationException("Exclusive native memory delivery required.");
        _executingRange = true;
        int responses = 0;
        try
        {
            while (_internalNext >= 0 && _internalNext <= time)
                if (ResolveOutputAt(_internalNext)) responses++;
            _time = time;
            return new(OutputBits, responses, _next);
        }
        finally { _announced = _next; _executingRange = false; }
    }

    public EmuMemoryResponse ExecuteProgram(in EmuSignalRange range,bool retainHistory=false)
    {
        range.Validate(_time,MemoryInputRange<Tmm2016Pins>.MaximumChanges);
        if(_publishing || _executingRange || OutputChanged is not null || NextEventNanosecondsChanged is not null)throw new InvalidOperationException("Exclusive native memory delivery required.");
        var words=range.Words;
        ulong previous=_pins;
        foreach(ref readonly var word in words)
        {
            if((word.Bits&~EmuMemoryPins.AllMask)!=0||(word.Bits&EmuMemoryPins.AddressMask)>=(ulong)_timing.Capacity)
                throw new ArgumentOutOfRangeException(nameof(range));
            ulong received=(word.Bits&~(0xffUL<<24))|(((word.Bits>>24)&(word.Bits>>32)&255)<<24);
            if(received!=previous)_=checked(word.Time+_maximumDelay);
            previous=received;
        }
        if(retainHistory)_rangeOutputs??=new TimedMemoryOutput<Tmm2016Output>[(MemoryInputRange<Tmm2016Pins>.MaximumChanges+1)*5];
        int index=0,count=0;
        _executingRange=true;
        try
        {
            while(true)
            {
                bool hasInput=index<words.Length;
                long inputTime=hasInput?words[index].Time:range.End;
                if(_internalNext>=0&&_internalNext<=inputTime)
                {
                    if(ResolveOutputAt(_internalNext))
                    { if(retainHistory)_rangeOutputs![count]=new(_time,Output); count++; }
                    continue;
                }
                if(!hasInput)break;
                ref readonly var input=ref words[index++]; _time=input.Time;
                ulong received=(input.Bits&~(0xffUL<<24))|(((input.Bits>>24)&(input.Bits>>32)&255)<<24);
                ApplyPackedPins(_time,received);
            }
            _time=range.End;
            return new(OutputBits,count,_next);
        }
        finally { _announced=_next; _executingRange=false; }
    }
}
