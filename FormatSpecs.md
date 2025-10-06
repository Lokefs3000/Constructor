## Primary Model Format (.pmf)

```csharp
/*

File root:
	Header Header;
	Mesh Meshes[Header.MeshCount]
	Node Nodes[Header.NodeCount]
	Vertex Vertices[Header.TotalVertices]
	Index Indices[Header.TotalIndices]
*/

struct Header
{
	uint Header;			//'PMF ' / 0x20504d46
	uint Version;			//1

	HeaderFlags Flags;		//byte/1b

	uint TotalVertices;
	uint TotalIndices;

	ushort MeshCount;
	ushort NodeCount;
}

enum HeaderFlags : byte
{
	None = 0,

	IsCompressed = 1 << 0		//File is compressed (LZ4)
	LargeIndices = 1 << 1		//Support 32bit indices
	HighNodePrecision = 1 << 2	//Nodes have 32 bit precision
	HighVertexPrecision = 1 << 3	//Mesh vertices have 32 bit precision
}

struct Mesh
{
	string Name;

	uint VertexOffset
	uint IndexCount
	//IndexOffset = Sum(Previous Meshes.IndexCount)
}

struct Node
{
	string Name;
	Transform Transform;

	ushort ChildCount;
	//Children[ChildCount] = Followed right after this node

	ushort MeshIndex;		//File mesh index
}

using NVector3 = (Header.Flags.Has(HeaderFlags.HighNodePrecision)) ? Vector3 : HalfVector3
using NQuaternion = (Header.Flags.Has(HeaderFlags.HighNodePrecision)) ? Vector3 : HalfVector3

struct Transform
{
	TransformFeatures Features;	//byte/1b

	NVector3 Position;		//Only if "Flags.Has(Features.Position)"
	NQuaternion Rotation;		//Only if "Flags.Has(Features.Rotation)"
	NVector3 Scale;			//Only if "Flags.Has(Features.Scale)"
}

enum TransformFeatures : byte
{
	None = 0,

	Position = 1 << 0,	//Transform has position
	Rotation = 1 << 1,	//Transform has rotation
	UniformScale = 1 << 2,	//Transform has uniform scale
	Scale = 1 << 3		//Transform has scale
}

struct Vertex
{
	
}
```

---