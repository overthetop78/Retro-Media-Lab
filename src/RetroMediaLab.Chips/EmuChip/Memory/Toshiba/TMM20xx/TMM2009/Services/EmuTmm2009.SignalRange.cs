using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM20xx.TMM2009.Services;

public sealed partial class EmuTmm2009
{
    private TimedMemoryOutput<Tmm2009Output>[]? _rangeOutputs;

    public MemoryOutputRange<Tmm2009Output> ExecuteInputRange(in MemoryInputRange<Tmm2009Pins> range,bool retainResponseHistory=true)
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

    public EmuMemoryResponse ExecuteProgram(in EmuSignalRange range,bool retainHistory=false)
    {
        range.Validate(_time,MemoryInputRange<Tmm2009Pins>.MaximumChanges);
        if(_publishing || OutputChanged is not null)throw new InvalidOperationException("Exclusive native memory delivery required.");
        var words=range.Words;

        foreach(ref readonly var word in words)
        {
            if((word.Bits&~EmuMemoryPins.AllMask)!=0||(word.Bits&EmuMemoryPins.AddressMask)>=1024)
                throw new ArgumentOutOfRangeException(nameof(range));

        }
        if(retainHistory)_rangeOutputs??=new TimedMemoryOutput<Tmm2009Output>[MemoryInputRange<Tmm2009Pins>.MaximumChanges];
        int index=0,count=0;
        try
        {
            while(true)
            {
                bool hasInput=index<words.Length;
                long inputTime=hasInput?words[index].Time:range.End;
                if(!hasInput)break;
                ref readonly var input=ref words[index++]; _time=input.Time;
                if(ApplyPackedPins(input.Bits))
                { if(retainHistory)_rangeOutputs![count]=new(_time,Output); count++; }
            }
            _time=range.End;
            return new(_outputBits,count,-1);
        }
        finally {  }
    }
}
