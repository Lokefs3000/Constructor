struct DrawColor
{
    uint16_t Type;
    half4 _Internal0;

    float4 GetColor() { return _Internal0; }
    uint GetGradientIndex() { return asuint(_Internal0); }
};

struct RawPaintData
{
    DrawColor Color;

    float StrokeWidth;
    DrawColor StrokeColor;
};

struct GenerateInfo
{
    uint DataOffset;
    uint16_t ShapeType;

    uint VertexOffset;
    uint IndexOffset;
};

static const uint16_t ShapeType_Points = 1;
static const uint16_t ShapeType_Lines = 2;

struct PointsInfo
{
    uint PointCount;
    uint DataOffset;
    RawPaintData Paint;
};

struct LinesInfo
{
    uint LineCount;
    uint DataOffset;
    RawPaintData Paint;
    
    bool IsStrip() { return LineCount & (1 << 31); }
};

struct Vertex
{
    float2 Position;
    float2 UV;
    float4 Color;
    uint MetadataOffset;
};

ByteAddressBuffer baShapingInfo;

RWStructuredBuffer<Vertex> sbVertices;
RWStructuredBuffer<uint> sbIndices;

void GeneratePoints(GenerateInfo gen, PointsInfo info)
{
    
}

void GenerateLines(GenerateInfo gen, LinesInfo info)
{
    if (info.IsStrip())
    {
        float2 v0 = baShapingInfo.Load<float2>(info.DataOffset);
        for (uint i = 1, vtxIdx = gen.VertexOffset, idxIdx = gen.IndexOffset; i < info.LineCount; ++i, vtxIdx += 4, idxIdx += 6)
        {
            float2 v1 = baShapingInfo.Load<float2>(info.DataOffset + i * sizeof(float2));


            v0 = v1;
        }
    }
    else
    {

    }
}

[kernel]
[numthreads(32, 1, 1)]
void CSGenerateShapes(uint3 dispatchId : SV_DispatchThreadID)
{
    GenerateInfo info = baShapingInfo.Load<GenerateInfo>(dispatchId.x * sizeof(GenerateInfo));
    switch (info.ShapeType)
    {
        case ShapeType_Points: GeneratePoints(info, baShapingInfo.Load<PointsInfo>(info.DataOffset)); break;
        case ShapeType_Lines: GenerateLines(info, baShapingInfo.Load<LinesData>(info.DataOffset)); break;
    }
}