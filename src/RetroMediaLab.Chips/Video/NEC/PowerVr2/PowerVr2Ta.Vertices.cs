using System.Diagnostics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    private const uint ListType_None = uint.MaxValue;
    private static readonly byte[] FloatSatTable = InitializeFloatSatTable();
    private static TaContext? _vertexContext;
    private static RenderContext VertexRenderContext => _vertexContext!.Rend;

    private static byte FloatToSaturatedByte(float value) => FloatSatTable[BitConverter.SingleToUInt32Bits(value) >> 16];
    private static float Float16(ushort value) => BitConverter.UInt32BitsToSingle((uint)value << 16);

    [Conditional("DEBUG")]
    private static void VerifyTa(bool condition) => Sh4HostRuntime.Verify(condition);

    private delegate Ta_Dma* TaListFunction(Ta_Dma* data, Ta_Dma* end);
    private delegate void TaPolyParamFunction(void* data);

    private static uint _tileClipValue;
    private static readonly byte[] FaceBaseColor = new byte[4];
    private static readonly byte[] FaceOffsetColor = new byte[4];
    private static readonly byte[] FaceBaseColor1 = new byte[4];
    private static readonly byte[] FaceOffsetColor1 = new byte[4];
    private static uint _spriteBaseColor;
    private static uint _spriteOffsetColor;
    private static int _lastModifierTriangle = -1;
    private static uint _currentList;
    private static TaListFunction _vertexDataFunction = null!;
    private static List<PolyParam>? _currentPolygonList;
    private static int _currentPolygon = -1;
    private static TaListFunction _taCommand = null!;
    private static bool _fetchTextures = true;

    private static readonly TaParser RgbaParser = new(0, 1, 2, 3);
    private static readonly TaParser BgraParser = new(2, 1, 0, 3);

    private static Ta_Dma* NullVertexData(Ta_Dma* data, Ta_Dma* end)
    {
        Trace.TraceInformation("TA: Invalid state, ignoring VTX data");
        return data + SZ32;
    }

    private static bool StartList(uint listType)
    {
        if (_currentList != ListType_None)
            return true;
        switch (listType)
        {
            case ListType_Opaque:
                _currentPolygonList = VertexRenderContext.GlobalParamOp;
                break;
            case ListType_Punch_Through:
                _currentPolygonList = VertexRenderContext.GlobalParamPt;
                break;
            case ListType_Translucent:
                _currentPolygonList = VertexRenderContext.GlobalParamTr;
                break;
            case ListType_Opaque_Modifier_Volume:
            case ListType_Translucent_Modifier_Volume:
                break;
            default:
                Trace.TraceWarning($"Invalid list type {listType}");
                return false;
        }
        _currentList = listType;
        _currentPolygon = -1;
        return true;
    }

    private static void EndList()
    {
        if (_currentList == ListType_None)
            return;
        if (_currentPolygon != -1 && _currentPolygonList![_currentPolygon].Count == 0
            && _currentPolygon == _currentPolygonList.Count - 1)
            _currentPolygonList.RemoveAt(_currentPolygon);
        _currentPolygon = -1;
        _currentPolygonList = null;
        if (_currentList == ListType_Opaque_Modifier_Volume || _currentList == ListType_Translucent_Modifier_Volume)
            EndModifierVolume();
        _currentList = ListType_None;
        _vertexDataFunction = NullVertexData;
    }

    private static void EndModifierVolume()
    {
        List<ModifierVolumeParam> list;
        if (_currentList == ListType_Opaque_Modifier_Volume)
            list = VertexRenderContext.GlobalParamMvo;
        else if (_currentList == ListType_Translucent_Modifier_Volume)
            list = VertexRenderContext.GlobalParamMvoTr;
        else
            return;
        if (list.Count != 0)
        {
            ref ModifierVolumeParam parameter = ref CollectionsMarshal.AsSpan(list)[^1];
            parameter.Count = (uint)VertexRenderContext.ModTrig.Count - parameter.First;
            if (parameter.Count == 0)
                list.RemoveAt(list.Count - 1);
        }
    }

    private static void ResetParserState()
    {
        Array.Fill(FaceBaseColor, byte.MaxValue);
        Array.Fill(FaceOffsetColor, byte.MaxValue);
        Array.Fill(FaceBaseColor1, byte.MaxValue);
        Array.Fill(FaceOffsetColor1, byte.MaxValue);
        _spriteBaseColor = 0;
        _spriteOffsetColor = 0;
        _lastModifierTriangle = -1;
        _currentList = ListType_None;
        _currentPolygon = -1;
        _currentPolygonList = null;
        _vertexDataFunction = NullVertexData;
    }

    private sealed partial class TaParser
    {
        private readonly int _red;
        private readonly int _green;
        private readonly int _blue;
        private readonly int _alpha;

        private Ta_Dma* HandlePolygon(Ta_Dma* data, Ta_Dma* end, uint polygonType, int part)
        {
            var vertex = (TA_VertexParam*)data;
            uint size = 0;
            if (part == 2)
                _taCommand = TaMain;

            switch (polygonType)
            {
                case 0:
                    AppendPolyVertex0(&vertex->vtx0);
                    size = SZ32;
                    break;
                case 1:
                    AppendPolyVertex1(&vertex->vtx1);
                    size = SZ32;
                    break;
                case 2:
                    AppendPolyVertex2(&vertex->vtx2);
                    size = SZ32;
                    break;
                case 3:
                    AppendPolyVertex3(&vertex->vtx3);
                    size = SZ32;
                    break;
                case 4:
                    AppendPolyVertex4(&vertex->vtx4);
                    size = SZ32;
                    break;
                case 7:
                    AppendPolyVertex7(&vertex->vtx7);
                    size = SZ32;
                    break;
                case 8:
                    AppendPolyVertex8(&vertex->vtx8);
                    size = SZ32;
                    break;
                case 9:
                    AppendPolyVertex9(&vertex->vtx9);
                    size = SZ32;
                    break;
                case 10:
                    AppendPolyVertex10(&vertex->vtx10);
                    size = SZ32;
                    break;
                case 5:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex5A(&vertex->vtx5A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex5B(&vertex->vtx5B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex5B((TA_Vertex5B*)data);
                        size += SZ32;
                    }
                    break;
                case 6:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex6A(&vertex->vtx6A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex6B(&vertex->vtx6B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex6B((TA_Vertex6B*)data);
                        size += SZ32;
                    }
                    break;
                case 11:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex11A(&vertex->vtx11A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex11B(&vertex->vtx11B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex11B((TA_Vertex11B*)data);
                        size += SZ32;
                    }
                    break;
                case 12:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex12A(&vertex->vtx12A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex12B(&vertex->vtx12B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex12B((TA_Vertex12B*)data);
                        size += SZ32;
                    }
                    break;
                case 13:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex13A(&vertex->vtx13A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex13B(&vertex->vtx13B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex13B((TA_Vertex13B*)data);
                        size += SZ32;
                    }
                    break;
                case 14:
                    if (part != 2)
                    {
                        size += SZ32;
                        AppendPolyVertex14A(&vertex->vtx14A);
                    }
                    if (part == 0)
                    {
                        AppendPolyVertex14B(&vertex->vtx14B);
                        size += SZ32;
                    }
                    else if (part == 2)
                    {
                        AppendPolyVertex14B((TA_Vertex14B*)data);
                        size += SZ32;
                    }
                    break;
            }
            return data + size;
        }

        private Ta_Dma* TaModifierVolumeB32(Ta_Dma* data, Ta_Dma* end)
        {
            AppendModifierVolumeVertexB((TA_ModVolB*)data);
            _taCommand = TaMain;
            return data + SZ32;
        }

        private Ta_Dma* TaModifierVolumeData(Ta_Dma* data, Ta_Dma* end)
        {
            var vertex = (TA_VertexParam*)data;
            if (data == end - SZ32)
            {
                AppendModifierVolumeVertexA(&vertex->mvolA);
                _taCommand = TaModifierVolumeB32;
                return data + SZ32;
            }
            else
            {
                AppendModifierVolumeVertexA(&vertex->mvolA);
                AppendModifierVolumeVertexB(&vertex->mvolB);
                return data + SZ64;
            }
        }

        private Ta_Dma* TaSpriteBData(Ta_Dma* data, Ta_Dma* end)
        {
            _taCommand = TaMain;
            AppendSpriteVertexB((TA_Sprite1B*)data);
            return data + SZ32;
        }

        private Ta_Dma* TaSpriteData(Ta_Dma* data, Ta_Dma* end)
        {
            VerifyTa(data->pcw.ParaType == ParamType_Vertex_Parameter);
            if (data == end - SZ32)
            {
                _taCommand = TaSpriteBData;
                var vertex = (TA_VertexParam*)data;
                AppendSpriteVertexA(&vertex->spr1A);
                return data + SZ32;
            }
            else
            {
                var vertex = (TA_VertexParam*)data;
                AppendSpriteVertexA(&vertex->spr1A);
                AppendSpriteVertexB(&vertex->spr1B);
                return data + SZ64;
            }
        }

        private Ta_Dma* TaPolyData(Ta_Dma* data, Ta_Dma* end, uint polygonType, uint polygonSize)
        {
            VerifyTa(data < end);
            if (!(polygonSize != SZ32 && data == end - SZ32))
            {
                do
                {
                    VerifyTa(data->pcw.ParaType == ParamType_Vertex_Parameter);
                    HandlePolygon(data, null, polygonType, 0);
                    if (data->pcw.EndOfStrip != 0)
                        goto StripEnd;
                    data += polygonSize;
                } while (data <= end - polygonSize);
            }

            if (polygonSize != SZ32 && data == end - SZ32)
            {
                HandlePolygon(data, null, polygonType, 1);
                if (data->pcw.EndOfStrip != 0)
                    EndPolygonStrip();
                _taCommand = (next, nextEnd) => HandlePolygon(next, nextEnd, polygonType, 2);
                data += SZ32;
            }
            return data;

        StripEnd:
            _taCommand = TaMain;
            if (data->pcw.EndOfStrip != 0)
                EndPolygonStrip();
            return data + polygonSize;
        }

        private void AppendPolyParam2Full(void* value)
        {
            var parameters = (Ta_Dma*)value;
            AppendPolyParam2A((TA_PolyParam2A*)&parameters[0]);
            AppendPolyParam2B((TA_PolyParam2B*)&parameters[1]);
        }

        private void AppendPolyParam4Full(void* value)
        {
            var parameters = (Ta_Dma*)value;
            AppendPolyParam4A((TA_PolyParam4A*)&parameters[0]);
            AppendPolyParam4B((TA_PolyParam4B*)&parameters[1]);
        }

        private Ta_Dma* TaPolyB32(Ta_Dma* data, Ta_Dma* end, int type)
        {
            if (type == 2)
                AppendPolyParam2B((TA_PolyParam2B*)data);
            else
                AppendPolyParam4B((TA_PolyParam4B*)data);
            _taCommand = TaMain;
            return data + SZ32;
        }

        private Ta_Dma* TaPolyBInvalid(Ta_Dma* data, Ta_Dma* end)
        {
            Trace.TraceWarning("ta_poly_B_Invalid");
            _taCommand = TaMain;
            return data + SZ32;
        }

        private Ta_Dma* TaMain(Ta_Dma* data, Ta_Dma* end)
        {
            while (data < end)
            {
                if (Sh4DreamcastRuntime.Platform.IsNaomi2 && (data->pcw.full & 0x08000000) != 0)
                {
                    Debug.WriteLine("Naomi 2 command detected");
                    break;
                }
                switch (data->pcw.ParaType)
                {
                    case ParamType_End_Of_List:
                        EndList();
                        data += SZ32;
                        break;

                    case ParamType_User_Tile_Clip:
                        SetClipRect(data->data_32[3] & 63, data->data_32[4] & 31, data->data_32[5] & 63, data->data_32[6] & 31);
                        data += SZ32;
                        break;

                    case ParamType_Object_List_Set:
                        Trace.TraceInformation("Unsupported list type: ParamType_Object_List_Set");
                        data += SZ32;
                        break;

                    case ParamType_Polygon_or_Modifier_Volume:
                        SetClipMode(data->pcw.User_Clip);
                        if (_currentList == ListType_None && !StartList(data->pcw.ListType))
                        {
                            data += SZ32;
                        }
                        else if (IsModVolList(_currentList))
                        {
                            StartModifierVolume((TA_ModVolParam*)data);
                            _vertexDataFunction = TaModifierVolumeData;
                            data += SZ32;
                        }
                        else
                        {
                            uint uid = TaTypeLut.Instance.Table[data->pcw.obj_ctrl];
                            if (uid == TaTypeLut.InvalidType)
                            {
                                Trace.TraceWarning($"Invalid TA type {data->pcw.full:x8}");
                                data += SZ32;
                            }
                            else
                            {
                                uint parameterSize = uid >> 30;
                                byte dataId = (byte)uid;
                                byte parameterId = (byte)(uid >> 8);
                                _vertexDataFunction = _polygonDataTable[dataId];
                                if (data <= end - parameterSize)
                                {
                                    _polygonParameterTable[parameterId](data);
                                    data += parameterSize;
                                }
                                else
                                {
                                    _polygonParameterATable[parameterId](data);
                                    _taCommand = _polygonParameterBTable[parameterId];
                                    data += SZ32;
                                }
                            }
                        }
                        break;

                    case ParamType_Sprite:
                        SetClipMode(data->pcw.User_Clip);
                        if (_currentList != ListType_None || StartList(data->pcw.ListType))
                        {
                            _vertexDataFunction = TaSpriteData;
                            AppendSpriteParam((TA_SpriteParam*)data);
                        }
                        data += SZ32;
                        break;

                    case ParamType_Vertex_Parameter:
                        data = _vertexDataFunction(data, end);
                        break;

                    case 3:
                    case 6:
                        Trace.TraceWarning($"Unhandled param type pcw {data->pcw.full:x8}");
                        throw new TaParserException();
                }
            }
            return data;
        }

        internal void Reset()
        {
            _taCommand = TaMain;
            ResetParserState();
            SetClipRect(0, 0, 39, 14);
            SetClipMode(0);
        }

        private static void SetClipRect(uint xmin, uint ymin, uint xmax, uint ymax)
        {
            uint value = _tileClipValue & 0xf0000000;
            value |= xmin;
            value |= xmax << 6;
            value |= ymin << 12;
            value |= ymax << 17;
            _tileClipValue = value;
        }

        private static void SetClipMode(uint mode)
        {
            _tileClipValue = (_tileClipValue & ~0xf0000000u) | (mode << 28);
        }

        private static void SetGlobalParameter(TA_PolyParam0* value)
        {
            if (_currentPolygon == -1 || _currentPolygonList![_currentPolygon].Count != 0)
            {
                _currentPolygonList!.Add(default);
                _currentPolygon = _currentPolygonList.Count - 1;
            }
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon];
            polygon.Init();
            polygon.First = (uint)VertexRenderContext.Verts.Count;
            polygon.Isp = value->isp;
            polygon.Tsp = value->tsp;
            polygon.Tcw = value->tcw;
            polygon.Pcw = value->pcw;
            polygon.TileClip = _tileClipValue;
            if (polygon.Pcw.Texture != 0 && _fetchTextures)
                polygon.Texture = PvrRenderer.Backend!.GetTexture(polygon.Tsp, polygon.Tcw);
        }

        private void SetFloatColor(Span<byte> target, float a, float r, float g, float b)
        {
            target[_red] = FloatToSaturatedByte(r);
            target[_green] = FloatToSaturatedByte(g);
            target[_blue] = FloatToSaturatedByte(b);
            target[_alpha] = FloatToSaturatedByte(a);
        }

        private static void AppendPolyParam0(void* value) => SetGlobalParameter((TA_PolyParam0*)value);

        private void AppendPolyParam1(void* value)
        {
            var parameter = (TA_PolyParam1*)value;
            SetGlobalParameter((TA_PolyParam0*)parameter);
            SetFloatColor(FaceBaseColor, parameter->FaceColorA, parameter->FaceColorR, parameter->FaceColorG, parameter->FaceColorB);
        }

        private static void AppendPolyParam2A(void* value) => SetGlobalParameter((TA_PolyParam0*)value);

        private void AppendPolyParam2B(void* value)
        {
            var parameter = (TA_PolyParam2B*)value;
            SetFloatColor(FaceBaseColor, parameter->FaceColorA, parameter->FaceColorR, parameter->FaceColorG, parameter->FaceColorB);
            SetFloatColor(FaceOffsetColor, parameter->FaceOffsetA, parameter->FaceOffsetR, parameter->FaceOffsetG, parameter->FaceOffsetB);
        }

        private static void AppendPolyParam3(void* value)
        {
            var parameter = (TA_PolyParam3*)value;
            SetGlobalParameter((TA_PolyParam0*)parameter);
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon];
            polygon.Tsp1.full = parameter->tsp1.full;
            polygon.Tcw1.full = parameter->tcw1.full;
            if (parameter->pcw.Texture != 0 && _fetchTextures)
                polygon.Texture1 = PvrRenderer.Backend!.GetTexture(parameter->tsp1, parameter->tcw1, 1);
        }

        private static void AppendPolyParam4A(void* value)
        {
            var parameter = (TA_PolyParam4A*)value;
            SetGlobalParameter((TA_PolyParam0*)parameter);
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon];
            polygon.Tsp1.full = parameter->tsp1.full;
            polygon.Tcw1.full = parameter->tcw1.full;
            if (parameter->pcw.Texture != 0 && _fetchTextures)
                polygon.Texture1 = PvrRenderer.Backend!.GetTexture(parameter->tsp1, parameter->tcw1, 1);
        }

        private void AppendPolyParam4B(void* value)
        {
            var parameter = (TA_PolyParam4B*)value;
            SetFloatColor(FaceBaseColor, parameter->FaceColor0A, parameter->FaceColor0R, parameter->FaceColor0G, parameter->FaceColor0B);
            SetFloatColor(FaceBaseColor1, parameter->FaceColor1A, parameter->FaceColor1R, parameter->FaceColor1G, parameter->FaceColor1B);
        }

        private static void AppendPolyParamInvalid(void* value) => Trace.TraceWarning("AppendPolyParamInvalid");

        private static void EndPolygonStrip()
        {
            ref PolyParam current = ref CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon];
            current.Count = (uint)VertexRenderContext.Verts.Count - current.First;
            if (current.Count > 0)
            {
                _currentPolygonList!.Add(current);
                _currentPolygon = _currentPolygonList.Count - 1;
                ref PolyParam next = ref CollectionsMarshal.AsSpan(_currentPolygonList)[_currentPolygon];
                next.First = (uint)VertexRenderContext.Verts.Count;
                next.Count = 0;
            }
        }

        private static void UpdateFz(float z)
        {
            int zBits = BitConverter.SingleToInt32Bits(z);
            if (BitConverter.SingleToInt32Bits(VertexRenderContext.FZMax) < zBits && zBits < 0x49800000)
                VertexRenderContext.FZMax = z;
        }

        private static ref TaVertex ConvertBaseVertex(float* position)
        {
            float inverseW = position[2];
            VertexRenderContext.Verts.Add(default);
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            vertex.X = position[0];
            vertex.Y = position[1];
            vertex.Z = inverseW;
            UpdateFz(inverseW);
            return ref vertex;
        }

        internal void SetPackedColor(Span<byte> target, uint source)
        {
            target[_blue] = (byte)source;
            source >>= 8;
            target[_green] = (byte)source;
            source >>= 8;
            target[_red] = (byte)source;
            source >>= 8;
            target[_alpha] = (byte)source;
        }

        private void ApplyFaceColor(Span<byte> target, byte[] faceColor, float value)
        {
            uint intensity = FloatToSaturatedByte(value);
            target[_red] = (byte)(faceColor[_red] * intensity / 256);
            target[_green] = (byte)(faceColor[_green] * intensity / 256);
            target[_blue] = (byte)(faceColor[_blue] * intensity / 256);
            target[_alpha] = faceColor[_alpha];
        }

        private void AppendPolyVertex0(TA_Vertex0* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol);
        }

        private void AppendPolyVertex1(TA_Vertex1* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetFloatColor(new Span<byte>(color, 4), vtx->BaseA, vtx->BaseR, vtx->BaseG, vtx->BaseB);
        }

        private void AppendPolyVertex2(TA_Vertex2* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt);
        }

        private void AppendPolyVertex3(TA_Vertex3* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol);
            fixed (byte* color = vertex.Spc)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol);
            vertex.U = vtx->u;
            vertex.V = vtx->v;
        }

        private void AppendPolyVertex4(TA_Vertex4* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol);
            fixed (byte* color = vertex.Spc)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol);
            vertex.U = Float16(vtx->u);
            vertex.V = Float16(vtx->v);
        }

        private void AppendPolyVertex5A(TA_Vertex5A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            vertex.U = vtx->u;
            vertex.V = vtx->v;
        }

        private void AppendPolyVertex5B(TA_Vertex5B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col)
                SetFloatColor(new Span<byte>(color, 4), vtx->BaseA, vtx->BaseR, vtx->BaseG, vtx->BaseB);
            fixed (byte* color = vertex.Spc)
                SetFloatColor(new Span<byte>(color, 4), vtx->OffsA, vtx->OffsR, vtx->OffsG, vtx->OffsB);
        }

        private void AppendPolyVertex6A(TA_Vertex6A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            vertex.U = Float16(vtx->u);
            vertex.V = Float16(vtx->v);
        }

        private void AppendPolyVertex6B(TA_Vertex6B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col)
                SetFloatColor(new Span<byte>(color, 4), vtx->BaseA, vtx->BaseR, vtx->BaseG, vtx->BaseB);
            fixed (byte* color = vertex.Spc)
                SetFloatColor(new Span<byte>(color, 4), vtx->OffsA, vtx->OffsR, vtx->OffsG, vtx->OffsB);
        }

        private void AppendPolyVertex7(TA_Vertex7* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt);
            fixed (byte* color = vertex.Spc)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor, vtx->OffsInt);
            vertex.U = vtx->u;
            vertex.V = vtx->v;
        }

        private void AppendPolyVertex8(TA_Vertex8* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt);
            fixed (byte* color = vertex.Spc)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor, vtx->OffsInt);
            vertex.U = Float16(vtx->u);
            vertex.V = Float16(vtx->v);
        }

        private void AppendPolyVertex9(TA_Vertex9* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol0);
            fixed (byte* color = vertex.Col1)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol1);
        }

        private void AppendPolyVertex10(TA_Vertex10* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt0);
            fixed (byte* color = vertex.Col1)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor1, vtx->BaseInt1);
        }

        private void AppendPolyVertex11A(TA_Vertex11A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol0);
            fixed (byte* color = vertex.Spc)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol0);
            vertex.U = vtx->u0;
            vertex.V = vtx->v0;
        }

        private void AppendPolyVertex11B(TA_Vertex11B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col1)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol1);
            fixed (byte* color = vertex.Spc1)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol1);
            vertex.U1 = vtx->u1;
            vertex.V1 = vtx->v1;
        }

        private void AppendPolyVertex12A(TA_Vertex12A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol0);
            fixed (byte* color = vertex.Spc)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol0);
            vertex.U = Float16(vtx->u0);
            vertex.V = Float16(vtx->v0);
        }

        private void AppendPolyVertex12B(TA_Vertex12B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col1)
                SetPackedColor(new Span<byte>(color, 4), vtx->BaseCol1);
            fixed (byte* color = vertex.Spc1)
                SetPackedColor(new Span<byte>(color, 4), vtx->OffsCol1);
            vertex.U1 = Float16(vtx->u1);
            vertex.V1 = Float16(vtx->v1);
        }

        private void AppendPolyVertex13A(TA_Vertex13A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt0);
            fixed (byte* color = vertex.Spc)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor, vtx->OffsInt0);
            vertex.U = vtx->u0;
            vertex.V = vtx->v0;
        }

        private void AppendPolyVertex13B(TA_Vertex13B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col1)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor1, vtx->BaseInt1);
            fixed (byte* color = vertex.Spc1)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor1, vtx->OffsInt1);
            vertex.U1 = vtx->u1;
            vertex.V1 = vtx->v1;
        }

        private void AppendPolyVertex14A(TA_Vertex14A* vtx)
        {
            ref TaVertex vertex = ref ConvertBaseVertex(vtx->xyz);
            fixed (byte* color = vertex.Col)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor, vtx->BaseInt0);
            fixed (byte* color = vertex.Spc)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor, vtx->OffsInt0);
            vertex.U = Float16(vtx->u0);
            vertex.V = Float16(vtx->v0);
        }

        private void AppendPolyVertex14B(TA_Vertex14B* vtx)
        {
            ref TaVertex vertex = ref CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^1];
            fixed (byte* color = vertex.Col1)
                ApplyFaceColor(new Span<byte>(color, 4), FaceBaseColor1, vtx->BaseInt1);
            fixed (byte* color = vertex.Spc1)
                ApplyFaceColor(new Span<byte>(color, 4), FaceOffsetColor1, vtx->OffsInt1);
            vertex.U1 = Float16(vtx->u1);
            vertex.V1 = Float16(vtx->v1);
        }

        private void AppendSpriteParam(TA_SpriteParam* sprite)
        {
            if (_currentPolygon == -1 || _currentPolygonList![_currentPolygon].Count != 0)
            {
                if (_currentPolygonList is null)
                    return;
                _currentPolygonList.Add(default);
                _currentPolygon = _currentPolygonList.Count - 1;
            }
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon];
            polygon.Init();
            polygon.First = (uint)VertexRenderContext.Verts.Count;
            polygon.Isp = sprite->isp;
            polygon.Tsp = sprite->tsp;
            polygon.Tcw = sprite->tcw;
            polygon.Pcw = sprite->pcw;
            polygon.TileClip = _tileClipValue;
            if (polygon.Pcw.Texture != 0 && _fetchTextures)
                polygon.Texture = PvrRenderer.Backend!.GetTexture(polygon.Tsp, polygon.Tcw);
            _spriteBaseColor = sprite->BaseCol;
            _spriteOffsetColor = sprite->OffsCol;
            polygon.Isp.CullMode ^= 1;
        }

        private void AppendSpriteVertexA(TA_Sprite1A* sprite)
        {
            if (_currentPolygon == -1)
                return;
            CollectionsMarshal.AsSpan(_currentPolygonList!)[_currentPolygon].Count = 4;

            List<TaVertex> vertices = VertexRenderContext.Verts;
            int first = vertices.Count;
            for (int i = 0; i < 4; i++)
                vertices.Add(default);

            fixed (TaVertex* vertex = CollectionsMarshal.AsSpan(vertices)[first..])
            {
                for (int i = 0; i < 4; i++)
                {
                    SetPackedColor(new Span<byte>(vertex[i].Col, 4), _spriteBaseColor);
                    SetPackedColor(new Span<byte>(vertex[i].Spc, 4), _spriteOffsetColor);
                }
                vertex[2].X = sprite->x0;
                vertex[2].Y = sprite->y0;
                vertex[2].Z = sprite->z0;
                UpdateFz(sprite->z0);

                vertex[3].X = sprite->x1;
                vertex[3].Y = sprite->y1;
                vertex[3].Z = sprite->z1;
                UpdateFz(sprite->z1);

                vertex[1].X = sprite->x2;
            }
        }

        private static void CalculateSpritePlane(TaVertex* vertices)
        {
            ref readonly TaVertex a = ref vertices[2];
            ref readonly TaVertex b = ref vertices[3];
            ref readonly TaVertex c = ref vertices[1];
            ref TaVertex p = ref vertices[0];
            float acX = c.X - a.X, acY = c.Y - a.Y, acZ = c.Z - a.Z;
            float abX = b.X - a.X, abY = b.Y - a.Y, abZ = b.Z - a.Z;
            float apX = p.X - a.X, apY = p.Y - a.Y;
            float pY = p.Y, pX = p.X, aX = a.X, aY = a.Y, aZ = a.Z;
            float abV = b.V - a.V, abU = b.U - a.U;
            float acV = c.V - a.V, acU = c.U - a.U;
            float aV = a.V, aU = a.U;
            float k3 = acX * abY - acY * abX;
            float k2 = (apX * abY - apY * abX) / k3;
            float k1;
            if (abX == 0)
                k1 = (pY - aY - k2 * acY) / abY;
            else
                k1 = (pX - aX - k2 * acX) / abX;

            p.Z = aZ + k1 * abZ + k2 * acZ;
            p.U = aU + k1 * abU + k2 * acU;
            p.V = aV + k1 * abV + k2 * acV;
        }

        private static void AppendSpriteVertexB(TA_Sprite1B* sprite)
        {
            if (_currentPolygon == -1)
                return;
            fixed (TaVertex* vertex = CollectionsMarshal.AsSpan(VertexRenderContext.Verts)[^4..])
            {
                vertex[1].Y = sprite->y2;
                vertex[1].Z = sprite->z2;
                UpdateFz(sprite->z2);

                vertex[0].X = sprite->x3;
                vertex[0].Y = sprite->y3;

                vertex[2].U = Float16(sprite->u0);
                vertex[2].V = Float16(sprite->v0);
                vertex[3].U = Float16(sprite->u1);
                vertex[3].V = Float16(sprite->v1);
                vertex[1].U = Float16(sprite->u2);
                vertex[1].V = Float16(sprite->v2);

                CalculateSpritePlane(vertex);
                UpdateFz(vertex[0].Z);
            }

            _currentPolygonList!.Add(_currentPolygonList[_currentPolygon]);
            _currentPolygon = _currentPolygonList.Count - 1;
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_currentPolygonList)[_currentPolygon];
            polygon.First = (uint)VertexRenderContext.Verts.Count;
            polygon.Count = 0;
        }

        private static void StartModifierVolume(TA_ModVolParam* value)
        {
            EndModifierVolume();
            List<ModifierVolumeParam> list;
            if (_currentList == ListType_Opaque_Modifier_Volume)
                list = VertexRenderContext.GlobalParamMvo;
            else if (_currentList == ListType_Translucent_Modifier_Volume)
                list = VertexRenderContext.GlobalParamMvoTr;
            else
                return;
            list.Add(default);
            ref ModifierVolumeParam parameter = ref CollectionsMarshal.AsSpan(list)[^1];
            parameter.Init();
            parameter.Isp.full = value->isp.full;
            parameter.Isp.VolumeLast = value->pcw.Volume != 0 ? 1u : 0u;
            parameter.First = (uint)VertexRenderContext.ModTrig.Count;
            parameter.TileClip = _tileClipValue;
        }

        private static void AppendModifierVolumeVertexA(TA_ModVolA* vertex)
        {
            if (_currentList != ListType_Opaque_Modifier_Volume && _currentList != ListType_Translucent_Modifier_Volume)
                return;
            VertexRenderContext.ModTrig.Add(default);
            _lastModifierTriangle = VertexRenderContext.ModTrig.Count - 1;
            ref ModTriangle triangle = ref CollectionsMarshal.AsSpan(VertexRenderContext.ModTrig)[_lastModifierTriangle];
            triangle.X0 = vertex->x0;
            triangle.Y0 = vertex->y0;
            triangle.Z0 = vertex->z0;
            triangle.X1 = vertex->x1;
            triangle.Y1 = vertex->y1;
            triangle.Z1 = vertex->z1;
            triangle.X2 = vertex->x2;
        }

        private static void AppendModifierVolumeVertexB(TA_ModVolB* vertex)
        {
            if (_currentList != ListType_Opaque_Modifier_Volume && _currentList != ListType_Translucent_Modifier_Volume)
                return;
            ref ModTriangle triangle = ref CollectionsMarshal.AsSpan(VertexRenderContext.ModTrig)[_lastModifierTriangle];
            triangle.Y2 = vertex->y2;
            triangle.Z2 = vertex->z2;
        }
    }

    private static void ParseRenderPass(ref RenderPass pass, in RenderPass previousPass,
        RenderContext context, bool primitiveRestart)
    {
        bool perPixel = PvrConfiguration.RendererType is PvrRenderType.OpenGLOit
            or PvrRenderType.DirectX11Oit or PvrRenderType.VulkanOit;
        bool mergeTranslucent = PvrConfiguration.PerStripSorting || perPixel;

        if (PvrConfiguration.RenderResolution > 480 && !PvrConfiguration.EmulateFramebuffer && PvrConfiguration.FixUpscaleBleedingEdge)
        {
            FixTextureBleeding(context.GlobalParamOp, (int)previousPass.OpCount, (int)pass.OpCount, context);
            FixTextureBleeding(context.GlobalParamPt, (int)previousPass.PtCount, (int)pass.PtCount, context);
            FixTextureBleeding(context.GlobalParamTr, (int)previousPass.TrCount, (int)pass.TrCount, context);
        }
        if (primitiveRestart)
        {
            MakePrimRestartIndex(context.GlobalParamOp, (int)previousPass.OpCount, (int)pass.OpCount, true, context);
            MakePrimRestartIndex(context.GlobalParamPt, (int)previousPass.PtCount, (int)pass.PtCount, true, context);
        }
        else
        {
            MakeIndex(context.GlobalParamOp, (int)previousPass.OpCount, (int)pass.OpCount, true, context);
            MakeIndex(context.GlobalParamPt, (int)previousPass.PtCount, (int)pass.PtCount, true, context);
        }

        pass.SortedTrCount = previousPass.SortedTrCount;
        if (pass.AutoSort && !perPixel)
        {
            if (PvrConfiguration.PerStripSorting)
                SortPolyParams(context.GlobalParamTr, (int)previousPass.TrCount, (int)pass.TrCount, context);
            else
                SortTriangles(context, ref pass, previousPass);
        }
        if (!pass.AutoSort || perPixel || PvrConfiguration.PerStripSorting)
        {
            if (primitiveRestart)
                MakePrimRestartIndex(context.GlobalParamTr, (int)previousPass.TrCount, (int)pass.TrCount, mergeTranslucent, context);
            else
                MakeIndex(context.GlobalParamTr, (int)previousPass.TrCount, (int)pass.TrCount, mergeTranslucent, context);
        }
    }

    internal static void SetTileClipping(RenderContext context)
    {
        GetRegionTileClipping(out uint xmin, out uint xmax, out uint ymin, out uint ymax);
        context.TileClip.Origin.X = (int)xmin;
        context.TileClip.Origin.Y = (int)ymin;
        xmax = Math.Min(xmax + 32, (uint)context.GlobClip.X);
        ymax = Math.Min(ymax + 32, (uint)context.GlobClip.Y);
        context.TileClip.Size.X = (int)(xmax - xmin);
        context.TileClip.Size.Y = (int)(ymax - ymin);
    }

    private static void ParseDreamcast(TaContext context, bool primitiveRestart)
    {
        VerifyTa(_vertexContext is null);
        _vertexContext = context;
        try
        {
            ParseReset();

            ref PolyParam background = ref CollectionsMarshal.AsSpan(VertexRenderContext.GlobalParamOp)[0];
            if (background.Pcw.Texture != 0)
                background.Texture = PvrRenderer.Backend!.GetTexture(background.Tsp, background.Tcw);

            TaContext? childContext = context;
            int passIndex = 0;
            RenderPass previousPass = default;
            while (childContext is not null)
            {
                var data = (Ta_Dma*)childContext.GetTaDataBegin();
                var end = (Ta_Dma*)childContext.GetTaDataEnd();
                while (data < end)
                {
                    try
                    {
                        data = _taCommand(data, end);
                    }
                    catch (TaParserException)
                    {
                        break;
                    }
                }

                if (passIndex == 0)
                {
                    foreach (ref PolyParam polygon in CollectionsMarshal.AsSpan(VertexRenderContext.GlobalParamOp))
                    {
                        polygon.Tsp.DstInstr = 0;
                        polygon.Tsp.SrcInstr = 1;
                    }
                }

                bool emptyPass = VertexRenderContext.GlobalParamOp.Count == (passIndex == 0 ? 0 : (int)VertexRenderContext.RenderPasses[^1].OpCount)
                    && VertexRenderContext.GlobalParamPt.Count == (passIndex == 0 ? 0 : (int)VertexRenderContext.RenderPasses[^1].PtCount)
                    && VertexRenderContext.GlobalParamTr.Count == (passIndex == 0 ? 0 : (int)VertexRenderContext.RenderPasses[^1].TrCount);
                if (passIndex == 0 || !emptyPass)
                {
                    VertexRenderContext.RenderPasses.Add(default);
                    ref RenderPass pass = ref CollectionsMarshal.AsSpan(VertexRenderContext.RenderPasses)[^1];
                    GetRegionSettings(passIndex, ref pass);
                    pass.OpCount = (uint)VertexRenderContext.GlobalParamOp.Count;
                    pass.PtCount = (uint)VertexRenderContext.GlobalParamPt.Count;
                    pass.TrCount = (uint)VertexRenderContext.GlobalParamTr.Count;
                    pass.SortedTrCount = 0;
                    pass.MvoCount = (uint)VertexRenderContext.GlobalParamMvo.Count;
                    pass.MvoTrCount = (uint)VertexRenderContext.GlobalParamMvoTr.Count;

                    ParseRenderPass(ref pass, previousPass, VertexRenderContext, primitiveRestart);
                    previousPass = pass;
                }
                childContext = childContext.NextContext;
                passIndex++;
            }
        }
        finally
        {
            _vertexContext = null;
        }
    }

    private static void ParseNaomi2(TaContext context, bool primitiveRestart)
    {
        foreach (ref PolyParam polygon in CollectionsMarshal.AsSpan(context.Rend.GlobalParamOp))
        {
            if (polygon.Pcw.Texture != 0)
                polygon.Texture = PvrRenderer.Backend!.GetTexture(polygon.Tsp, polygon.Tcw, 0);
            if (polygon.Tsp1.full != uint.MaxValue)
                polygon.Texture1 = PvrRenderer.Backend!.GetTexture(polygon.Tsp1, polygon.Tcw1, 1);
        }
        foreach (ref PolyParam polygon in CollectionsMarshal.AsSpan(context.Rend.GlobalParamPt))
        {
            if (polygon.Pcw.Texture != 0)
                polygon.Texture = PvrRenderer.Backend!.GetTexture(polygon.Tsp, polygon.Tcw, 0);
            if (polygon.Tsp1.full != uint.MaxValue)
                polygon.Texture1 = PvrRenderer.Backend!.GetTexture(polygon.Tsp1, polygon.Tcw1, 1);
        }
        foreach (ref PolyParam polygon in CollectionsMarshal.AsSpan(context.Rend.GlobalParamTr))
        {
            if (polygon.Pcw.Texture != 0)
                polygon.Texture = PvrRenderer.Backend!.GetTexture(polygon.Tsp, polygon.Tcw, 0);
            if (polygon.Tsp1.full != uint.MaxValue)
                polygon.Texture1 = PvrRenderer.Backend!.GetTexture(polygon.Tsp1, polygon.Tcw1, 1);
        }

        context.Rend.NewRenderPass();
        RenderPass previousPass = default;
        var passes = CollectionsMarshal.AsSpan(context.Rend.RenderPasses);
        for (int i = 0; i < passes.Length; i++)
        {
            ref RenderPass pass = ref passes[i];
            ParseRenderPass(ref pass, previousPass, context.Rend, primitiveRestart);
            if (i == 0)
            {
                foreach (ref PolyParam polygon in CollectionsMarshal.AsSpan(context.Rend.GlobalParamOp))
                {
                    polygon.Tsp.DstInstr = 0;
                    polygon.Tsp.SrcInstr = 1;
                }
            }
            previousPass = pass;
        }
    }

    internal static void Parse(TaContext context, bool primitiveRestart)
    {
        if (Sh4DreamcastRuntime.Platform.IsNaomi2)
            ParseNaomi2(context, primitiveRestart);
        else
            ParseDreamcast(context, primitiveRestart);
    }

    private static List<PolyParam>? _naomiPolygonList;
    private static int _naomiPolygon = -1;
    private static List<ModifierVolumeParam>? _naomiModifierList;
    private static int _naomiModifier = -1;

    private static readonly float[] IdentityMatrix =
    [
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f
    ];

    private const int IdentityMatrixIndex = 0;
    private const int NoLightIndex = 0;

    private static void SetDefaultMatrices()
    {
        if (CurrentContext!.Rend.Matrices.Count == 0)
        {
            fixed (float* matrix = IdentityMatrix)
                CurrentContext.Rend.Matrices.Add(*(N2Matrix*)matrix);
        }
    }

    private static void SetDefaultLight()
    {
        if (CurrentContext!.Rend.LightModels.Count == 0)
            CurrentContext.Rend.LightModels.Add(default);
    }

    internal static void AddPolygon(in PolyParam parameter)
    {
        VerifyTa(CurrentContext is not null);
        VerifyTa(_vertexContext is null);
        _vertexContext = CurrentContext;
        try
        {
            StartList(parameter.Pcw.ListType);
            _currentPolygonList!.Add(parameter);
            _currentPolygon = -1;
            _naomiPolygonList = _currentPolygonList;
            _naomiPolygon = _naomiPolygonList.Count - 1;
            ref PolyParam polygon = ref CollectionsMarshal.AsSpan(_naomiPolygonList)[_naomiPolygon];
            polygon.First = (uint)CurrentContext!.Rend.Verts.Count;
            polygon.Count = 0;
            polygon.TileClip = _tileClipValue;
            SetDefaultMatrices();
            if (polygon.MvMatrix == -1)
                polygon.MvMatrix = IdentityMatrixIndex;
            if (polygon.NormalMatrix == -1)
                polygon.NormalMatrix = IdentityMatrixIndex;
            SetDefaultLight();
            if (polygon.LightModel == -1)
                polygon.LightModel = NoLightIndex;
        }
        finally
        {
            _vertexContext = null;
        }
    }

    internal static void AddPolygon(int listType, in ModifierVolumeParam parameter)
    {
        VerifyTa(CurrentContext is not null);
        VerifyTa(_vertexContext is null);
        _vertexContext = CurrentContext;
        try
        {
            StartList((uint)listType);
            switch (_currentList)
            {
                case ListType_Opaque_Modifier_Volume:
                    CurrentContext!.Rend.GlobalParamMvo.Add(parameter);
                    _naomiModifierList = CurrentContext.Rend.GlobalParamMvo;
                    _naomiModifier = _naomiModifierList.Count - 1;
                    break;
                case ListType_Translucent_Modifier_Volume:
                    CurrentContext!.Rend.GlobalParamMvoTr.Add(parameter);
                    _naomiModifierList = CurrentContext.Rend.GlobalParamMvoTr;
                    _naomiModifier = _naomiModifierList.Count - 1;
                    break;
                default:
                    Sh4HostRuntime.Die("wrong list type");
                    break;
            }
            ref ModifierVolumeParam modifier = ref CollectionsMarshal.AsSpan(_naomiModifierList!)[_naomiModifier];
            modifier.First = (uint)CurrentContext!.Rend.ModTrig.Count;
            modifier.Count = 0;
            SetDefaultMatrices();
            if (modifier.MvMatrix == -1)
                modifier.MvMatrix = IdentityMatrixIndex;
        }
        finally
        {
            _vertexContext = null;
        }
    }

    internal static void AddVertex(in TaVertex vertex)
    {
        CurrentContext!.Rend.Verts.Add(vertex);
        CollectionsMarshal.AsSpan(_naomiPolygonList!)[_naomiPolygon].Count++;
    }

    internal static void AddTriangle(in ModTriangle triangle)
    {
        CurrentContext!.Rend.ModTrig.Add(triangle);
        CollectionsMarshal.AsSpan(_naomiModifierList!)[_naomiModifier].Count++;
    }

    internal static int AddMatrix(float* matrix)
    {
        SetDefaultMatrices();
        CurrentContext!.Rend.Matrices.Add(*(N2Matrix*)matrix);
        return CurrentContext.Rend.Matrices.Count - 1;
    }

    internal static int AddLight(in N2LightModel light)
    {
        SetDefaultLight();
        CurrentContext!.Rend.LightModels.Add(light);
        return CurrentContext.Rend.LightModels.Count - 1;
    }

    internal static uint AddTaData(uint* data, uint size)
    {
        VerifyTa(_vertexContext is null);
        _vertexContext = CurrentContext;
        _fetchTextures = false;

        var command = (Ta_Dma*)data;
        var end = (Ta_Dma*)(data + size / 4);
        try
        {
            command = _taCommand(command, end);
        }
        finally
        {
            _vertexContext = null;
            _fetchTextures = true;
        }
        return (uint)((byte*)command - (byte*)data);
    }

    internal static uint GetTileClip() => _tileClipValue;

    internal static void SetTileClip(uint value) => _tileClipValue = value;

    internal static uint GetListType() => _currentList;

    internal static void SetListType(uint listType)
    {
        VerifyTa(_vertexContext is null);
        _vertexContext = CurrentContext;
        try
        {
            EndList();
            if (listType != ListType_None)
                StartList(listType);
        }
        finally
        {
            _vertexContext = null;
        }
    }

    private static bool IsDirectXRenderer() => PvrConfiguration.RendererType is
        PvrRenderType.DirectX9 or PvrRenderType.DirectX11 or PvrRenderType.DirectX11Oit;

    internal static void ParseReset()
    {
        if (IsDirectXRenderer())
            BgraParser.Reset();
        else
            RgbaParser.Reset();
    }

    private static void DecodePvrVertex(uint address, uint pointer, TaVertex* vertex, TaParser parser)
    {
        var isp = new ISP_TSP { full = PvrMemory.Read32<uint>(address) };
        vertex->X = PvrMemory.Read32<float>(pointer);
        pointer += 4;
        vertex->Y = PvrMemory.Read32<float>(pointer);
        pointer += 4;
        vertex->Z = PvrMemory.Read32<float>(pointer);
        pointer += 4;

        if (isp.Texture != 0)
        {
            if (isp.UV_16b != 0)
            {
                uint uv = PvrMemory.Read32<uint>(pointer);
                vertex->U = Float16((ushort)uv);
                vertex->V = Float16((ushort)(uv >> 16));
                pointer += 4;
            }
            else
            {
                vertex->U = PvrMemory.Read32<float>(pointer);
                pointer += 4;
                vertex->V = PvrMemory.Read32<float>(pointer);
                pointer += 4;
            }
        }

        uint color = PvrMemory.Read32<uint>(pointer);
        pointer += 4;
        parser.SetPackedColor(new Span<byte>(vertex->Col, 4), color);
        if (isp.Offset != 0)
        {
            uint offsetColor = PvrMemory.Read32<uint>(pointer);
            pointer += 4;
            parser.SetPackedColor(new Span<byte>(vertex->Spc, 4), offsetColor);
        }
    }

    private static byte FloatToSaturatedByteMath(float value) =>
        (byte)(float.IsNaN(value) ? 255f : Minimum(1f, Maximum(0f, value)) * 255f);

    private static byte[] InitializeFloatSatTable()
    {
        byte[] table = new byte[65536];
        for (uint i = 0; i < table.Length; i++)
        {
            uint bits = i << 16;
            table[i] = FloatToSaturatedByteMath(BitConverter.UInt32BitsToSingle(bits));
        }
        return table;
    }

    internal static void FillBgp(TaContext context)
    {
        ref PolyParam polygon = ref CollectionsMarshal.AsSpan(context.Rend.GlobalParamOp)[0];
        var vertices = CollectionsMarshal.AsSpan(context.Rend.Verts);

        uint parameterBase = PvrRegisters.PARAM_BASE & 0xf00000;
        uint stripBase = (parameterBase + PvrRegisters.ISP_BACKGND_T.tag_address * 4) & PvrMemory.VramMask;

        uint stripVertexSize = 3 + PvrRegisters.ISP_BACKGND_T.skip;
        if (PvrRegisters.FPU_SHAD_SCALE.intensity_shadow == 1 && PvrRegisters.ISP_BACKGND_T.shadow == 1)
            stripVertexSize += PvrRegisters.ISP_BACKGND_T.skip;
        stripVertexSize *= 4;

        uint stripVertexNumber = PvrRegisters.ISP_BACKGND_T.tag_offset;
        uint vertexPointer = stripVertexNumber * stripVertexSize + stripBase + 3 * 4;

        polygon.Isp.full = PvrMemory.Read32<uint>(stripBase);
        polygon.Tsp.full = PvrMemory.Read32<uint>(stripBase + 4);
        polygon.Tcw.full = PvrMemory.Read32<uint>(stripBase + 8);
        polygon.Count = 4;
        polygon.First = 0;
        polygon.TileClip = 0;

        polygon.Isp.DepthMode = 7;
        polygon.Isp.CullMode = 0;
        polygon.Pcw.UV_16bit = polygon.Isp.UV_16b;
        polygon.Pcw.Gouraud = polygon.Isp.Gouraud;
        polygon.Pcw.Offset = polygon.Isp.Offset;
        polygon.Pcw.Texture = polygon.Isp.Texture;
        polygon.Pcw.Shadow = PvrRegisters.ISP_BACKGND_T.shadow;

        fixed (TaVertex* vertex = vertices)
        {
            for (int i = 0; i < 3; i++)
            {
                if (IsDirectXRenderer())
                    DecodePvrVertex(stripBase, vertexPointer, &vertex[i], BgraParser);
                else
                    DecodePvrVertex(stripBase, vertexPointer, &vertex[i], RgbaParser);
                vertexPointer += stripVertexSize;
            }
        }

        float backgroundDepth = Maximum(PvrRegisters.ISP_BACKGND_D.f - 1e-6f, 1e-11f);
        vertices[0].Z = backgroundDepth;
        vertices[1].Z = backgroundDepth;
        vertices[2].Z = backgroundDepth;

        float scaleX = PvrRegisters.SCALER_CTL.hscale == 1 ? 2f : 1f;
        if (polygon.Pcw.Texture == 0)
        {
            vertices[0].X = -256f * scaleX;
            vertices[0].Y = 0f;
            vertices[1].X = 896f * scaleX;
            vertices[1].Y = 0f;
            vertices[2].X = vertices[0].X;
            vertices[2].Y = 480f;
            vertices[3] = vertices[2];
            vertices[3].X = vertices[1].X;
        }
        else
        {
            if (vertices[2].X == vertices[1].X)
            {
                vertices[2].X = vertices[0].X;
                vertices[2].U = vertices[0].U;
            }
            float deltaU = (vertices[1].U - vertices[0].U) * 0.4f;
            vertices[0].X -= 256f;
            vertices[0].U -= deltaU;
            vertices[1].X += 256f;
            vertices[1].U += deltaU;
            vertices[2].X -= 256f;
            vertices[2].U -= deltaU;

            vertices[0].X *= scaleX;
            vertices[1].X *= scaleX;
            vertices[2].X *= scaleX;

            vertices[3] = vertices[2];
            vertices[3].X = vertices[1].X;
            vertices[3].U = vertices[1].U;
        }
    }

    private static void GetRegionTileClipping(out uint xmin, out uint xmax, out uint ymin, out uint ymax)
    {
        xmin = 20;
        xmax = 0;
        ymin = 15;
        ymax = 0;

        GetRegionTileAddressAndSize(out uint address, out uint tileSize);

        RegionArrayTile tile;
        int maxTiles = 3000;
        do
        {
            tile.full = PvrMemory.Read32<uint>(address);
            xmin = Math.Min(xmin, tile.X);
            xmax = Math.Max(xmax, tile.X);
            ymin = Math.Min(ymin, tile.Y);
            ymax = Math.Max(ymax, tile.Y);
            address += tileSize;
        } while (tile.LastRegion == 0 && --maxTiles >= 0);
        if (maxTiles < 0)
            Trace.TraceWarning("getRegionTileClipping overflow");

        xmin *= 32;
        xmax *= 32;
        ymin *= 32;
        ymax *= 32;
    }

    internal static void GetRegionSettings(int passNumber, ref RenderPass pass)
    {
        GetRegionTileAddressAndSize(out uint address, out uint tileSize);
        address += (uint)passNumber * tileSize;
        var tile = new RegionArrayTile { full = PvrMemory.Read32<uint>(address) };

        if (((PvrRegisters.FPU_PARAM_CFG >> 21) & 1) == 0)
            pass.AutoSort = (PvrRegisters.ISP_FEED_CFG & 1) == 0;
        else
            pass.AutoSort = tile.PreSort == 0;
        pass.ZClear = tile.NoZClear == 0;
        pass.MvOpTrShared = PvrMemory.Read32<uint>(address + 8) == PvrMemory.Read32<uint>(address + 16);
    }
}

internal sealed partial class RenderContext
{
    internal void NewRenderPass()
    {
        var pass = new RenderPass
        {
            OpCount = (uint)GlobalParamOp.Count,
            TrCount = (uint)GlobalParamTr.Count,
            PtCount = (uint)GlobalParamPt.Count,
            MvoCount = (uint)GlobalParamMvo.Count,
            MvoTrCount = (uint)GlobalParamMvoTr.Count,
            SortedTrCount = 0
        };
        PowerVr2Ta.GetRegionSettings(RenderPasses.Count, ref pass);
        RenderPasses.Add(pass);
    }
}
