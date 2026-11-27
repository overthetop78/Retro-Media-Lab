using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static partial class PvrSorter
{
    internal static unsafe partial void SetFirstProvokingVertex(RenderContext renderContext)
    {
        void SetProvokingVertex(List<PolyParam> polygons)
        {
            fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(renderContext.Verts))
            {
                foreach (PolyParam polygon in polygons)
                {
                    if (polygon.Pcw.Gouraud != 0)
                        continue;
                    for (uint i = 0; i + 2 < polygon.Count; i++)
                    {
                        uint index = renderContext.Idx[(int)(polygon.First + i)];
                        if (index == uint.MaxValue)
                            continue;
                        TaVertex* vertex = &vertices[index];
                        index = renderContext.Idx[(int)(polygon.First + i + 1)];
                        if (index == uint.MaxValue)
                        {
                            i++;
                            continue;
                        }
                        index = renderContext.Idx[(int)(polygon.First + i + 2)];
                        if (index == uint.MaxValue)
                        {
                            i += 2;
                            continue;
                        }
                        TaVertex* lastVertex = &vertices[index];
                        NativeMemory.Copy(lastVertex->Col, vertex->Col, 4);
                        NativeMemory.Copy(lastVertex->Spc, vertex->Spc, 4);
                        NativeMemory.Copy(lastVertex->Col1, vertex->Col1, 4);
                        NativeMemory.Copy(lastVertex->Spc1, vertex->Spc1, 4);
                    }
                }
            }
        }

        SetProvokingVertex(renderContext.GlobalParamOp);
        SetProvokingVertex(renderContext.GlobalParamPt);
        if (renderContext.SortedTriangles.Count == 0)
            SetProvokingVertex(renderContext.GlobalParamTr);
        else
        {
            foreach (SortedTriangle triangle in renderContext.SortedTriangles)
            {
                if (renderContext.GlobalParamTr[(int)triangle.PolyIndex].Pcw.Gouraud != 0)
                    continue;
                for (uint i = 0; i + 2 < triangle.Count; i += 3)
                {
                    Span<uint> indices = CollectionsMarshal.AsSpan(renderContext.Idx)[(int)(triangle.First + i)..];
                    uint last = indices[2];
                    indices[2] = indices[1];
                    indices[1] = indices[0];
                    indices[0] = last;
                }
            }
        }
    }
}
