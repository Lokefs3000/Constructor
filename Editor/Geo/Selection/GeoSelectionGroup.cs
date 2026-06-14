using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Interaction;
using Primary.Common;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Geo.Selection
{
    internal sealed class GeoSelectionGroup
    {
        private Dictionary<Brush, BrushSelectionData> _selectionData;
        private AllowedSelections _allowedSelections;

        internal GeoSelectionGroup()
        {
            _selectionData = new Dictionary<Brush, BrushSelectionData>();
        }

        private ref BrushSelectionData GetOrAddSelectionData(Brush brush, out bool exists)
        {
            ref BrushSelectionData sd = ref CollectionsMarshal.GetValueRefOrAddDefault(_selectionData, brush, out exists);
            if (!exists)
            {
                sd = new BrushSelectionData(false, 0, 0, 0, 0);
                SelectionManager.Select(brush, SelectMode.Append);
            }

            return ref sd;
        }

        private ref BrushSelectionData GetOrNullSelectionData(Brush brush, out bool exists)
        {
            ref BrushSelectionData sd = ref CollectionsMarshal.GetValueRefOrNullRef(_selectionData, brush);
            exists = !Unsafe.IsNullRef(in sd);

            return ref sd;
        }

        private void RemoveSelectionData(Brush brush)
        {
            bool didRemove = _selectionData.Remove(brush, out BrushSelectionData sd);
            SelectionManager.Deselect(brush);

            if (didRemove)
                OnDeselected?.Invoke(brush, sd);
        }

        internal void DeselectAll()
        {
            using RentedArray<Brush> brushes = RentedArray<Brush>.Rent(_selectionData.Count);
            _selectionData.Keys.CopyTo(brushes.BackingArray, 0);

            foreach (Brush brush in brushes)
            {
                SelectionUpdated?.Invoke(brush, default);
                RemoveSelectionData(brush);
            }
        }

        internal void Select(Brush brush)
        {
            ref BrushSelectionData sd = ref GetOrAddSelectionData(brush, out bool exists);

            sd.IsBrushActive = true;
            sd.CommitData(_allowedSelections);

            if (!exists)
                OnSelected?.Invoke(brush, sd);
            SelectionUpdated?.Invoke(brush, sd);
        }

        internal void Select(Brush brush, BrushFaceIndex faceIndex)
        {
            ref BrushSelectionData sd = ref GetOrAddSelectionData(brush, out bool exists);

            sd.ActiveData.FaceVertexData |= s_faceVertexMasks[(int)faceIndex];
            sd.ActiveData.FaceFaceData |= (byte)(1 << (int)faceIndex);

            sd.CommitData(_allowedSelections);

            if (!exists)
                OnSelected?.Invoke(brush, sd);
            SelectionUpdated?.Invoke(brush, sd);
        }

        internal void Select(Brush brush, BrushVertexIndex vertexIndex)
        {
            ref BrushSelectionData sd = ref GetOrAddSelectionData(brush, out bool exists);

            sd.ActiveData.VertexVertexData |= (byte)(1 << (int)vertexIndex);

            foreach (BrushFaceIndex faceIndex in s_vertexFaceMasks[(int)vertexIndex].Item1)
            {
                if (Flags.HasFlag(sd.ActiveVertices, s_faceVertexMasks[(int)faceIndex]))
                {
                    sd.ActiveData.VertexFaceData |= (byte)(1 << (int)faceIndex);
                }
            }

            sd.CommitData(_allowedSelections);

            if (!exists)
                OnSelected?.Invoke(brush, sd);
            SelectionUpdated?.Invoke(brush, sd);
        }

        internal void Deselect(Brush brush)
        {
            ref BrushSelectionData sd = ref GetOrNullSelectionData(brush, out bool exists);

            if (exists && sd.IsBrushActive)
            {
                sd.IsBrushActive = false;
                sd.CommitData(_allowedSelections);
                SelectionUpdated?.Invoke(brush, sd);

                if (sd.IsEmpty)
                    RemoveSelectionData(brush);
            }
        }

        internal void Deselect(Brush brush, BrushFaceIndex faceIndex)
        {
            ref BrushSelectionData sd = ref GetOrNullSelectionData(brush, out bool exists);

            if (exists && sd.IsFaceSelected(faceIndex))
            {
                sd.ActiveData.FaceFaceData &= (byte)(~(1 << (int)faceIndex) & BrushSelectionData.AllFaces);

                byte verts = (byte)~s_faceVertexMasks[(int)faceIndex];
                for (int i = 0; i < 6; i++)
                {
                    if (Flags.HasFlag(sd.ActiveData.FaceFaceData, 1 << i))
                        verts |= s_faceVertexMasks[i];
                }

                sd.ActiveData.FaceVertexData &= (byte)(verts & BrushSelectionData.AllVertices);

                sd.CommitData(_allowedSelections);
                SelectionUpdated?.Invoke(brush, sd);

                if (sd.IsEmpty)
                    RemoveSelectionData(brush);
            }
        }

        internal void Deselect(Brush brush, BrushVertexIndex vertexIndex)
        {
            ref BrushSelectionData sd = ref GetOrNullSelectionData(brush, out bool exists);

            if (exists && sd.IsVertexSelected(vertexIndex))
            {
                sd.ActiveData.VertexVertexData &= (byte)(~(1 << (int)vertexIndex) & BrushSelectionData.AllVertices);

                byte faces = (byte)~s_vertexFaceMasks[(int)vertexIndex].Mask;
                for (int i = 0; i < 8; i++)
                {
                    if (Flags.HasFlag(sd.ActiveData.VertexVertexData, 1 << i))
                        faces |= s_vertexFaceMasks[i].Mask;
                }

                sd.ActiveData.VertexFaceData &= (byte)(faces & BrushSelectionData.AllFaces);

                sd.CommitData(_allowedSelections);
                SelectionUpdated?.Invoke(brush, sd);

                if (sd.IsEmpty)
                    RemoveSelectionData(brush);
            }
        }

        internal bool TryGetSelectionData(Brush brush, out BrushSelectionData selectionData)
        {
            return _selectionData.TryGetValue(brush, out selectionData);
        }

        internal void UpdateAllowed(AllowedSelections allowed)
        {
            if (_allowedSelections == allowed)
                return;

            foreach (var (brush, _) in _selectionData)
            {
                ref BrushSelectionData selectionData = ref CollectionsMarshal.GetValueRefOrNullRef(_selectionData, brush);
                Debug.Assert(!Unsafe.IsNullRef(in selectionData));

                byte prevFaceData = selectionData.FaceData;
                byte prevVertexData = selectionData.VertexData;

                selectionData.CommitData(allowed);
                if (prevFaceData != selectionData.FaceData || prevVertexData != selectionData.VertexData)
                {
                    SelectionUpdated?.Invoke(brush, selectionData);
                }
            }

            _allowedSelections = allowed;
        }

        internal Dictionary<Brush, BrushSelectionData> Selection => _selectionData;
        internal AllowedSelections AllowedSelections => _allowedSelections;

        internal bool IsEmpty => _selectionData.Count == 0;

        internal event Action<Brush, BrushSelectionData>? SelectionUpdated;

        internal event Action<Brush, BrushSelectionData>? OnSelected;
        internal event Action<Brush, BrushSelectionData>? OnDeselected;

        private static readonly byte[] s_faceVertexMasks =
             [.. BrushMeshGenerator.BrushFaceVertices.Select(static (x) =>
             {
                 int mask = 0;
                 for (int i = 0; i < x.Length; i++)
                 {
                     mask |= 1 << x[i];
                 }

                 return (byte)mask;
             })];

        private static readonly (BrushFaceIndex[] Faces, byte Mask)[] s_vertexFaceMasks =
            [.. Enum.GetValues<BrushVertexIndex>().Select(static (x) =>
            {
                BrushFaceIndex[] arr = new BrushFaceIndex[6];

                int j = 0;
                int mask = 0;

                for (int i = 0; i < BrushMeshGenerator.BrushFaceVertices.Length; i++)
                {
                    if (BrushMeshGenerator.BrushFaceVertices[i].Contains((int)x))
                    {
                        arr[j++] = (BrushFaceIndex)i;
                        mask |= s_faceVertexMasks[i];
                    }
                }

                return (arr[..j], (byte)mask);
            })];
    }

    internal enum AllowedSelections : byte
    {
        None = 0,

        Brush = 1 << 0,
        Face = 1 << 1,
        Vertex = 1 << 2
    }
}
