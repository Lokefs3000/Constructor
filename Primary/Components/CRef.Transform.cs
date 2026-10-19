using System.Numerics;
using Primary.Scenes.Entities;

namespace Primary.Components
{
    public static partial class CRefExtensions
    {
        extension (CRef<Transform> cref)
        {
            public Vector3 Position { get => cref.ComponentRef.Position; set => cref.ComponentRef.Position = value; }
            public Quaternion Rotation { get => cref.ComponentRef.Rotation; set => cref.ComponentRef.Rotation = value; }
            public Vector3 Scale { get => cref.ComponentRef.Scale; set => cref.ComponentRef.Scale = value; }
        }
    }
}
