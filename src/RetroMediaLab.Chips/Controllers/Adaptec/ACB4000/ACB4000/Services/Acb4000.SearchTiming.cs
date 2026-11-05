using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using T = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchTimingConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private void BeginIndexSearch() => _searchDeadline.BeginIndexWait(T.IndexSetupTStates);
    private void BeginFirstFieldSearch() => _searchDeadline.BeginFirstFieldWait(T.FirstFieldSetupTStates);

    private bool TickSearchTiming()
    {
        if (_stage is not (Acb4000Stage.Parameters or Acb4000Stage.Read or Acb4000Stage.WriteSearch))
        {
            _searchDeadline.Reset();
            return false;
        }
        var expired = _searchDeadline.Tick();
        while (expired != Acb4000SearchWait.None)
        {
            if (expired == Acb4000SearchWait.Index)
            {
                if (_stage == Acb4000Stage.Parameters) ExpireParameterSearch();
                else Complete(T.NoIndex);
                return true;
            }
            // A missing field cannot leave the preceding ID associated with later DATA.
            _matchingReadId = false;
            _readChannel.Reset();
            _readChannel.BeginIdRead();
            if (_searchBudget.ObserveId(checkValid: true) is { } error)
            {
                if (_stage == Acb4000Stage.Parameters) ExpireParameterSearch();
                else Complete(error, addressValid: true);
                return true;
            }
            // Keep the fractional overshoot, including several windows within one owner tick.
            _searchDeadline.ContinueFieldWait();
            expired = _searchDeadline.TakeExpired();
        }
        return false;
    }
}
