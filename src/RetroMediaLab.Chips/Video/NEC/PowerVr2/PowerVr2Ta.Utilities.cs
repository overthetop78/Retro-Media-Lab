using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    private static bool IsVertexInfinite(in TaVertex vertex) =>
        float.IsNaN(vertex.X) || MathF.Abs(vertex.X) > 1e25f
        || float.IsNaN(vertex.Y) || MathF.Abs(vertex.Y) > 1e25f
        || float.IsNaN(vertex.Z) || vertex.Z > 3.4e37f;

    private struct IndexTriangle(uint polygonId, uint vertex0, uint vertex1, uint vertex2)
    {
        internal uint V0 = vertex0;
        internal uint V1 = vertex1;
        internal uint V2 = vertex2;
        internal uint PolygonId = polygonId;
        internal float Z;
    }

    private static readonly List<IndexTriangle> TriangleList = [];
    private static readonly IComparer<float> SortDepthComparer =
        Comparer<float>.Create((left, right) => left < right ? -1 : right < left ? 1 : 0);

    private static float Minimum(float left, float right) => right < left ? right : left;

    private static float Maximum(float left, float right) => left < right ? right : left;

    private static float MinZ(ReadOnlySpan<TaVertex> vertices, in IndexTriangle triangle) =>
        Minimum(Minimum(vertices[(int)triangle.V0].Z, vertices[(int)triangle.V1].Z), vertices[(int)triangle.V2].Z);

    private static float GetProjectedZ(in TaVertex vertex, ref N2Matrix matrix)
    {
        fixed (float* values = matrix.Mat)
            return -1 / (values[2] * vertex.X + values[6] * vertex.Y + values[10] * vertex.Z + values[14]);
    }

    internal static void SortTriangles(RenderContext context, ref RenderPass pass, in RenderPass previousPass)
    {
        int first = (int)previousPass.TrCount;
        int count = (int)pass.TrCount - first;
        if (count == 0)
            return;

        var polygons = CollectionsMarshal.AsSpan(context.GlobalParamTr);
        var vertices = CollectionsMarshal.AsSpan(context.Verts);
        var matrices = CollectionsMarshal.AsSpan(context.Matrices);

        int vertexCount = context.Verts.Count - (int)polygons[first].First;
        TriangleList.EnsureCapacity(vertexCount);
        TriangleList.Clear();

        for (int polygonIndex = first; polygonIndex != first + count; polygonIndex++)
        {
            ref readonly PolyParam polygon = ref polygons[polygonIndex];
            if (polygon.Count < 3)
                continue;
            int v0 = (int)polygon.First;
            int v1 = v0 + 1;
            float z0 = 0, z1 = 0;
            if (polygon.IsNaomi2())
            {
                z0 = GetProjectedZ(vertices[v0], ref matrices[polygon.MvMatrix]);
                z1 = GetProjectedZ(vertices[v1], ref matrices[polygon.MvMatrix]);
            }
            else
            {
                if (IsVertexInfinite(vertices[v0]))
                    v0 = -1;
                if (IsVertexInfinite(vertices[v1]))
                    v1 = -1;
            }
            for (uint i = 2; i < polygon.Count; i++)
            {
                int v2 = (int)(polygon.First + i);
                if (!polygon.IsNaomi2() && IsVertexInfinite(vertices[v2]))
                    v2 = -1;
                if (v0 != -1 && v1 != -1 && v2 != -1)
                {
                    var triangle = new IndexTriangle((uint)(polygonIndex - first), (uint)v0, (uint)v1, (uint)v2);
                    if (polygon.IsNaomi2())
                    {
                        float z2 = GetProjectedZ(vertices[v2], ref matrices[polygon.MvMatrix]);
                        triangle.Z = Minimum(z0, Minimum(z1, z2));
                        z0 = z1;
                        z1 = z2;
                    }
                    else
                    {
                        triangle.Z = MinZ(vertices, triangle);
                    }
                    TriangleList.Add(triangle);
                }
                if ((i & 1) != 0)
                    v1 = v2;
                else
                    v0 = v2;
            }
        }

        IndexTriangle[] triangles = TriangleList.OrderBy(triangle => triangle.Z, SortDepthComparer).ToArray();
        for (int index = 1; index < triangles.Length; index++)
        {
            if (triangles[index].PolygonId != triangles[index - 1].PolygonId)
            {
                ref readonly PolyParam current = ref polygons[first + (int)triangles[index].PolygonId];
                ref readonly PolyParam previous = ref polygons[first + (int)triangles[index - 1].PolygonId];
                if (current.EquivalentIgnoreCullingDirection(previous)
                    && (current.Isp.CullMode < 2 || current.Isp.CullMode == previous.Isp.CullMode))
                    triangles[index].PolygonId = triangles[index - 1].PolygonId;
            }
        }

        int lastPolygonIndex = -1;
        int indexSize = context.Idx.Count;
        for (int index = 0; index < triangles.Length; index++)
        {
            ref readonly IndexTriangle triangle = ref triangles[index];
            int polygonId = (int)triangle.PolygonId;
            context.Idx.Add(triangle.V0);
            context.Idx.Add(triangle.V1);
            context.Idx.Add(triangle.V2);

            if (lastPolygonIndex != polygonId)
            {
                var current = new SortedTriangle
                {
                    PolyIndex = (uint)(first + polygonId),
                    First = (uint)(indexSize + index * 3),
                    Count = 0
                };
                if (lastPolygonIndex != -1)
                {
                    ref SortedTriangle last = ref CollectionsMarshal.AsSpan(context.SortedTriangles)[^1];
                    last.Count = current.First - last.First;
                }
                context.SortedTriangles.Add(current);
                lastPolygonIndex = polygonId;
            }
        }

        if (triangles.Length != 0)
        {
            ref SortedTriangle last = ref CollectionsMarshal.AsSpan(context.SortedTriangles)[^1];
            last.Count = (uint)(indexSize + triangles.Length * 3) - last.First;
        }
        else
        {
            context.SortedTriangles.Add(new SortedTriangle { PolyIndex = (uint)first, First = 0, Count = 0 });
        }
        pass.SortedTrCount = (uint)context.SortedTriangles.Count;

#if PRINT_SORT_STATS
        Console.WriteLine($"Reassembled into {context.SortedTriangles.Count} from {count}");
#endif
    }

    internal static void SortPolyParams(List<PolyParam> polygonList, int first, int end, RenderContext context)
    {
        if (end - first <= 1)
            return;

        var polygons = CollectionsMarshal.AsSpan(polygonList);
        var vertices = CollectionsMarshal.AsSpan(context.Verts);
        var matrices = CollectionsMarshal.AsSpan(context.Matrices);
        for (int polygonIndex = first; polygonIndex != end; polygonIndex++)
        {
            ref PolyParam polygon = ref polygons[polygonIndex];
            if (polygon.Count < 3)
            {
                polygon.ZvZ = 0;
            }
            else
            {
                var polygonVertices = vertices.Slice((int)polygon.First, (int)polygon.Count);
                if (polygon.IsNaomi2())
                {
                    Matrix4x4 matrix;
                    fixed (float* m = matrices[polygon.MvMatrix].Mat)
                        matrix = new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                            m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);

                    var min = new Vector3(1e38f);
                    var max = new Vector3(-1e38f);
                    foreach (ref readonly TaVertex vertex in polygonVertices)
                    {
                        min = new Vector3(Minimum(min.X, vertex.X), Minimum(min.Y, vertex.Y), Minimum(min.Z, vertex.Z));
                        max = new Vector3(Maximum(max.X, vertex.X), Maximum(max.Y, vertex.Y), Maximum(max.Z, vertex.Z));
                    }
                    var center = new Vector4((min + max) / 2f, 1);
                    Vector3 extents = max - new Vector3(center.X, center.Y, center.Z);

                    center = Vector4.Transform(center, matrix);
                    Vector4 extentX = Vector4.Transform(new Vector4(extents.X, 0, 0, 0), matrix);
                    Vector4 extentY = Vector4.Transform(new Vector4(0, extents.Y, 0, 0), matrix);
                    Vector4 extentZ = Vector4.Transform(new Vector4(0, 0, extents.Z, 0), matrix);
                    Vector3 newExtent = Vector3.Abs(new Vector3(extentX.X, extentX.Y, extentX.Z))
                        + Vector3.Abs(new Vector3(extentY.X, extentY.Y, extentY.Z))
                        + Vector3.Abs(new Vector3(extentZ.X, extentZ.Y, extentZ.Z));

                    var transformedCenter = new Vector3(center.X, center.Y, center.Z);
                    min = transformedCenter - newExtent;
                    max = transformedCenter + newExtent;
                    polygon.ZvZ = -1 / Minimum(min.Z, max.Z);
                }
                else
                {
                    uint zv = uint.MaxValue;
                    foreach (ref readonly TaVertex vertex in polygonVertices)
                        zv = Math.Min(zv, BitConverter.SingleToUInt32Bits(vertex.Z));
                    polygon.ZvZ = BitConverter.UInt32BitsToSingle(zv);
                }
            }
        }

        var sorted = polygons[first..end].ToArray().OrderBy(polygon => polygon.ZvZ, SortDepthComparer).ToArray();
        sorted.AsSpan().CopyTo(polygons[first..end]);
    }

    internal static void GetRegionTileAddressAndSize(out uint address, out uint size)
    {
        address = PvrRegisters.REGION_BASE;
        bool type1Tile = ((PvrRegisters.FPU_PARAM_CFG >> 21) & 1) == 0;
        size = (uint)(type1Tile ? 5 : 6) * 4;
        bool emptyFirstRegion = true;
        for (int index = type1Tile ? 4 : 5; index > 0; index--)
        {
            if ((PvrMemory.Read32<uint>(address + (uint)index * 4) & 0x80000000) == 0)
            {
                emptyFirstRegion = false;
                break;
            }
        }
        if (emptyFirstRegion)
            address += size;
        var tile = new RegionArrayTile { full = PvrMemory.Read32<uint>(address) };
        if (tile.PreSort != 0)
            size = 6 * 4;
    }

    internal static int GetTaContextAddresses(uint* addresses)
    {
        GetRegionTileAddressAndSize(out uint address, out uint tileSize);

        var tile = new RegionArrayTile { full = PvrMemory.Read32<uint>(address) };
        uint x = tile.X;
        uint y = tile.Y;
        uint count = 0;
        do
        {
            tile.full = PvrMemory.Read32<uint>(address);
            if (tile.X != x || tile.Y != y)
                break;

            uint opbAddress = PvrMemory.Read32<uint>(address + 4);
            if ((opbAddress & 0x80000000) != 0)
            {
                opbAddress = PvrMemory.Read32<uint>(address + 12);
                if ((opbAddress & 0x80000000) != 0)
                {
                    if (tileSize >= 24)
                        opbAddress = PvrMemory.Read32<uint>(address + 20);
                    if ((opbAddress & 0x80000000) != 0)
                    {
                        Trace.TraceInformation($"Can't find any non-null OPB for pass {count}");
                        break;
                    }
                }
            }
            addresses[count++] = PvrMemory.Read32<uint>(opbAddress);
            address += tileSize;
        } while (tile.LastRegion == 0 && count < MaxPasses);

        return (int)count;
    }

    internal static void FixTextureBleeding(List<PolyParam> polygonList, int first, int end, RenderContext context)
    {
        var polygons = CollectionsMarshal.AsSpan(polygonList);
        var vertices = CollectionsMarshal.AsSpan(context.Verts);

        for (int polygonIndex = first; polygonIndex != end; polygonIndex++)
        {
            ref readonly PolyParam polygon = ref polygons[polygonIndex];
            if (polygon.Pcw.Texture == 0 || polygon.Count < 3 || polygon.IsNaomi2())
                continue;

            uint last = polygon.First + polygon.Count;
            bool needsFixing = true;
            float z = 0f;
            for (uint index = polygon.First; index < last && needsFixing; index++)
            {
                ref TaVertex vertex = ref vertices[(int)index];
                if (vertex.U != 0f && (vertex.U <= 0.995f || vertex.U > 1f))
                    needsFixing = false;
                else if (vertex.V != 0f && (vertex.V <= 0.995f || vertex.V > 1f))
                    needsFixing = false;
                else if (index == polygon.First)
                    z = vertex.Z;
                else if (z != vertex.Z)
                    needsFixing = false;
            }
            if (!needsFixing)
                continue;
            uint width = 8u << (int)polygon.Tsp.TexU;
            uint height = 8u << (int)polygon.Tsp.TexV;
            for (uint index = polygon.First; index < last; index++)
            {
                ref TaVertex vertex = ref vertices[(int)index];
                if (vertex.U > 0.995f)
                    vertex.U = 1f;
                vertex.U = (0.5f + vertex.U * (width - 1)) / width;
                if (vertex.V > 0.995f)
                    vertex.V = 1f;
                vertex.V = (0.5f + vertex.V * (height - 1)) / height;
            }
        }
    }

    internal static void MakePrimRestartIndex(List<PolyParam> polygonList, int first, int end, bool merge, RenderContext context)
    {
        if (first >= polygonList.Count)
            return;
        var polygons = CollectionsMarshal.AsSpan(polygonList);
        var vertices = CollectionsMarshal.AsSpan(context.Verts);
        int lastPolygon = -1;
        for (int polygonIndex = first; polygonIndex != end; polygonIndex++)
        {
            ref PolyParam polygon = ref polygons[polygonIndex];
            int firstIndex;
            bool duplicateNextVertex = false;
            if (merge && lastPolygon != -1 && polygons[lastPolygon].Count != 0
                && polygon.EquivalentIgnoreCullingDirection(polygons[lastPolygon]))
            {
                context.Idx.Add(uint.MaxValue);
                duplicateNextVertex = polygon.Isp.CullMode >= 2 && polygon.Isp.CullMode != polygons[lastPolygon].Isp.CullMode;
                firstIndex = (int)polygons[lastPolygon].First;
            }
            else
            {
                lastPolygon = polygonIndex;
                firstIndex = context.Idx.Count;
            }
            int lastGoodVertex = -1;
            for (uint index = 0; index < polygon.Count; index++)
            {
                ref readonly TaVertex vertex = ref vertices[(int)(polygon.First + index)];
                if (!polygon.IsNaomi2() && IsVertexInfinite(vertex))
                {
                    bool odd = (index & 1) != 0;
                    while (index < polygon.Count - 1)
                    {
                        odd = !odd;
                        ref readonly TaVertex nextVertex = ref vertices[(int)(polygon.First + index + 1)];
                        if (!IsVertexInfinite(nextVertex))
                        {
                            if (polygon.Count - (index + 1) < 3)
                                index = polygon.Count - 1;
                            else
                            {
                                if (lastGoodVertex >= 0)
                                    context.Idx.Add(uint.MaxValue);
                                if (odd && polygon.Isp.CullMode >= 2)
                                    duplicateNextVertex = true;
                            }
                            break;
                        }
                        index++;
                    }
                }
                else
                {
                    lastGoodVertex = (int)(polygon.First + index);
                    if (duplicateNextVertex)
                    {
                        context.Idx.Add((uint)lastGoodVertex);
                        duplicateNextVertex = false;
                    }
                    context.Idx.Add((uint)lastGoodVertex);
                }
            }
            if (lastPolygon == polygonIndex)
            {
                polygon.First = (uint)firstIndex;
                polygon.Count = (uint)(context.Idx.Count - firstIndex);
            }
            else
            {
                polygons[lastPolygon].Count = (uint)context.Idx.Count - polygons[lastPolygon].First;
                polygon.Count = 0;
            }
        }
    }

    internal static void MakeIndex(List<PolyParam> polygonList, int first, int end, bool merge, RenderContext context)
    {
        if (first >= polygonList.Count)
            return;
        var polygons = CollectionsMarshal.AsSpan(polygonList);
        var vertices = CollectionsMarshal.AsSpan(context.Verts);
        int lastPolygon = -1;
        bool cullingReversed = false;
        for (int polygonIndex = first; polygonIndex != end; polygonIndex++)
        {
            ref PolyParam polygon = ref polygons[polygonIndex];
            int firstIndex;
            bool duplicateNextVertex = false;
            if (merge && lastPolygon != -1 && polygons[lastPolygon].Count != 0
                && polygon.EquivalentIgnoreCullingDirection(polygons[lastPolygon]))
            {
                ref readonly PolyParam last = ref polygons[lastPolygon];
                uint lastVertex = context.Idx[(int)(last.First + last.Count - 1)];
                context.Idx.Add(lastVertex);
                if (polygon.Isp.CullMode < 2 || polygon.Isp.CullMode == last.Isp.CullMode)
                {
                    if (cullingReversed)
                        context.Idx.Add(lastVertex);
                    cullingReversed = false;
                }
                else
                {
                    if (!cullingReversed)
                        context.Idx.Add(lastVertex);
                    cullingReversed = true;
                }
                duplicateNextVertex = true;
                firstIndex = (int)last.First;
            }
            else
            {
                lastPolygon = polygonIndex;
                firstIndex = context.Idx.Count;
                cullingReversed = false;
            }
            int lastGoodVertex = -1;
            for (uint index = 0; index < polygon.Count; index++)
            {
                ref readonly TaVertex vertex = ref vertices[(int)(polygon.First + index)];
                if (!polygon.IsNaomi2() && IsVertexInfinite(vertex))
                {
                    while (index < polygon.Count - 1)
                    {
                        ref readonly TaVertex nextVertex = ref vertices[(int)(polygon.First + index + 1)];
                        if (!IsVertexInfinite(nextVertex))
                        {
                            if (lastGoodVertex >= 0)
                            {
                                Sh4HostRuntime.Verify(!duplicateNextVertex);
                                context.Idx.Add((uint)lastGoodVertex);
                                duplicateNextVertex = true;
                            }
                            break;
                        }
                        index++;
                    }
                }
                else
                {
                    lastGoodVertex = (int)(polygon.First + index);
                    if (duplicateNextVertex)
                    {
                        context.Idx.Add((uint)lastGoodVertex);
                        duplicateNextVertex = false;
                    }
                    uint count = (uint)(context.Idx.Count - firstIndex);
                    if ((((index ^ count) & 1) != 0) ^ cullingReversed)
                        context.Idx.Add((uint)lastGoodVertex);
                    context.Idx.Add((uint)lastGoodVertex);
                }
            }
            if (lastPolygon == polygonIndex)
            {
                polygon.First = (uint)firstIndex;
                polygon.Count = (uint)(context.Idx.Count - firstIndex);
            }
            else
            {
                polygons[lastPolygon].Count = (uint)context.Idx.Count - polygons[lastPolygon].First;
                polygon.Count = 0;
            }
        }
    }
}
