using System.Buffers;
using System.Collections;
using System.Numerics;

namespace Editor.GeoEdit
{
    internal static class BrushFaceEnumerator
    {
        //public static FaceEnumerable Get(IBrush brush)
        //{
        //    if (brush is BoxBrush boxBrush)
        //    {
        //        BrushTriangle[] triangles = ArrayPool<BrushTriangle>.Shared.Rent(12);
        //
        //        Vector3 halfExtents = boxBrush.Extents * 0.5f;
        //
        //        Vector3 tbl = boxBrush.Transform.Position + new Vector3(-halfExtents.X, halfExtents.Y, -halfExtents.Z);
        //        Vector3 tbr = boxBrush.Transform.Position + new Vector3(halfExtents.X, halfExtents.Y, -halfExtents.Z);
        //        Vector3 tfl = boxBrush.Transform.Position + new Vector3(-halfExtents.X, halfExtents.Y, halfExtents.Z);
        //        Vector3 tfr = boxBrush.Transform.Position + new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z);
        //
        //        Vector3 bbl = boxBrush.Transform.Position + new Vector3(-halfExtents.X, -halfExtents.Y, -halfExtents.Z);
        //        Vector3 bbr = boxBrush.Transform.Position + new Vector3(halfExtents.X, -halfExtents.Y, -halfExtents.Z);
        //        Vector3 bfl = boxBrush.Transform.Position + new Vector3(-halfExtents.X, -halfExtents.Y, halfExtents.Z);
        //        Vector3 bfr = boxBrush.Transform.Position + new Vector3(halfExtents.X, -halfExtents.Y, halfExtents.Z);
        //
        //        if (boxBrush.Transform.Rotation != Quaternion.Identity)
        //        {
        //            tbl = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            tbr = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            tfl = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            tfr = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //
        //            bbl = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            bbr = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            bfl = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //            bfr = Vector3.Transform(tbl, boxBrush.Transform.Rotation);
        //        }
        //
        //        TriangulateQuad(triangles.AsSpan(0), )
        //    }
        //
        //    throw new NotImplementedException();
        //}

        internal struct FaceEnumerable : IEnumerable<BrushTriangle>
        {
            public IEnumerator<BrushTriangle> GetEnumerator() => new FaceEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal struct FaceEnumerator : IEnumerator<BrushTriangle>
        {
            private BrushTriangle[] _triangles;
            private int _length;

            private int _index;

            public void Dispose()
            {
                ArrayPool<BrushTriangle>.Shared.Return(_triangles);

                _triangles = Array.Empty<BrushTriangle>();
                _length = -1;
                _index = -1;
            }

            public void Reset()
            {
                if (_index == -1)
                    throw new ObjectDisposedException(GetType().Name);

                _index = 0;
            }

            public bool MoveNext()
            {
                if (_index == -1)
                    throw new ObjectDisposedException(GetType().Name);
                if (_index >= _triangles.Length)
                    return false;

                _index++;
                return true;
            }

            public BrushTriangle Current
            {
                get
                {
                    if (_index == -1)
                        throw new ObjectDisposedException(GetType().Name);

                    return _triangles[_index];
                }
            }
            object IEnumerator.Current => Current;
        }
    }

    internal readonly record struct BrushTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 Normal, int FaceIndex);
}
