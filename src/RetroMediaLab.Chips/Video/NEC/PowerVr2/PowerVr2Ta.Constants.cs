namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    private sealed partial class TaParser
    {
        private readonly TaListFunction[] _polygonDataTable;
        private readonly TaPolyParamFunction[] _polygonParameterTable;
        private readonly TaPolyParamFunction[] _polygonParameterATable;
        private readonly TaListFunction[] _polygonParameterBTable;

        internal TaParser(int red, int green, int blue, int alpha)
        {
            _red = red;
            _green = green;
            _blue = blue;
            _alpha = alpha;

            _polygonDataTable =
            [
                (data, end) => TaPolyData(data, end, 0, SZ32),
                (data, end) => TaPolyData(data, end, 1, SZ32),
                (data, end) => TaPolyData(data, end, 2, SZ32),
                (data, end) => TaPolyData(data, end, 3, SZ32),
                (data, end) => TaPolyData(data, end, 4, SZ32),
                (data, end) => TaPolyData(data, end, 5, SZ64),
                (data, end) => TaPolyData(data, end, 6, SZ64),
                (data, end) => TaPolyData(data, end, 7, SZ32),
                (data, end) => TaPolyData(data, end, 8, SZ32),
                (data, end) => TaPolyData(data, end, 9, SZ32),
                (data, end) => TaPolyData(data, end, 10, SZ32),
                (data, end) => TaPolyData(data, end, 11, SZ64),
                (data, end) => TaPolyData(data, end, 12, SZ64),
                (data, end) => TaPolyData(data, end, 13, SZ64),
                (data, end) => TaPolyData(data, end, 14, SZ64)
            ];

            _polygonParameterTable =
            [
                AppendPolyParam0,
                AppendPolyParam1,
                AppendPolyParam2Full,
                AppendPolyParam3,
                AppendPolyParam4Full
            ];

            _polygonParameterATable =
            [
                AppendPolyParamInvalid,
                AppendPolyParamInvalid,
                AppendPolyParam2A,
                AppendPolyParamInvalid,
                AppendPolyParam4A
            ];

            _polygonParameterBTable =
            [
                TaPolyBInvalid,
                TaPolyBInvalid,
                (data, end) => TaPolyB32(data, end, 2),
                TaPolyBInvalid,
                (data, end) => TaPolyB32(data, end, 4)
            ];
        }
    }
}
