using Editor.Geometry.Diagnostics;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Geometry.Mesh
{
    public sealed class MeshGenerator
    {
        private GeneratorTimings _timings;

        private BrushMeshCache _meshCache;
        private HashSet<BrushMeshKey> _invalidFaces;

        internal MeshGenerator()
        {
            _timings = new GeneratorTimings();

            _meshCache = new BrushMeshCache();
            _invalidFaces = new HashSet<BrushMeshKey>();
        }

        /// <summary>Not thread-safe</summary>
        internal void Generate(GeoScene scene, MeshContainer container, MaterialAsset? defaultMaterial = null)
        {
            using (new TimingScope(ref _timings.TotalTime))
            {
                container.ClearPreviousData();

                FindInvalidFaces(scene, container);
                ResolveFacePositions(scene, container, defaultMaterial);
                BuildFaceGeometry(scene, container);
            }
        }

        private void FindInvalidFaces(GeoScene scene, MeshContainer container)
        {
            using (new TimingScope(ref _timings.FindInvalid))
            {
                foreach (BrushGroup group in scene.Groups)
                {
                    foreach (Brush brush in group.Brushes)
                    {
                        BrushUpdateFlags updateFlags = brush.UpdateFlags;
                        if (updateFlags != BrushUpdateFlags.None)
                        {
                            brush.RecalculateBoundingBox();
                            for (int i = 0; i < 6; i++)
                            {
                                _invalidFaces.Add(new BrushMeshKey(brush, (BrushFaceIndex)i));
                            }
                        }
                    }
                }
            }
        }

        private void ResolveFacePositions(GeoScene scene, MeshContainer container, MaterialAsset? defaultMaterial)
        {
            using (new TimingScope(ref _timings.ResolveFaces))
            {
                foreach (Brush deletedBrush in scene.DeletedBrushes)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        BrushFace face = deletedBrush.Faces[i];
                        BrushMeshKey key = new BrushMeshKey(deletedBrush, (BrushFaceIndex)i);

                        if (container.TryGetBrushSliceIndex(key, out int sliceIndex))
                        {
                            container.RemoveBrushFace(key);
                        }
                    }
                }

                scene.ClearDeletedBrushes();

                foreach (BrushMeshKey key in _invalidFaces)
                {
                    BrushFace face = key.Brush.Faces[(int)key.FaceIndex];
                    MaterialAsset? material = (face.Material != null && face.Material.Status != ResourceStatus.Error) ? face.Material : defaultMaterial;

                    if (container.TryGetBrushSliceIndex(key, out int sliceIndex))
                    {
                        ref MeshSlice slice = ref container.Slices[sliceIndex];

                        bool isInvisible = Flags.HasFlag(face.Flags, BrushFaceFlags.Invisible);
                        if (isInvisible || material == null)
                            container.RemoveBrushFace(key);
                        else if (slice.Material != material)
                            container.MoveBrushFace(key, material);
                        else
                            container.AddFaceUpdate(key, sliceIndex);
                    }
                    else if (!Flags.HasFlag(face.Flags, BrushFaceFlags.Invisible) && material != null)
                    {
                        container.AddBrushFace(key, material);
                    }
                }

                _invalidFaces.Clear();
            }
        }

        private void BuildFaceGeometry(GeoScene scene, MeshContainer container)
        {
            using (new TimingScope(ref _timings.BuildGeometry))
            {
                container.UpdateCurrentData();

                Span<MeshSlice> slices = container.Slices;
                foreach (MeshFaceUpdate update in container.FaceUpdates)
                {
                    MeshSlice slice = slices[update.SliceIndex];
                    BrushMesh mesh = _meshCache.GetMesh(update.Key.Brush);

                    int faceIdx = mesh.GetFaceIndex((int)update.Key.FaceIndex);
                    if (faceIdx == 7)
                        GeoManager.Logger?.Error("Failed to update face because it does not exist: {id}-{face}", update.Key.Brush.Id, update.Key.FaceIndex);
                    else
                    {
                        Span<BrushVertex> vertices = container.Vertices.Slice(update.VertexOffset + slice.VtxOffset, 6);
                        MeshFace face = mesh.Faces[faceIdx];

                        vertices[0] = mesh.Vertices[face.I0];
                        vertices[1] = mesh.Vertices[face.I1];
                        vertices[2] = mesh.Vertices[face.I2];
                        vertices[3] = mesh.Vertices[face.I3];
                        vertices[4] = mesh.Vertices[face.I4];
                        vertices[5] = mesh.Vertices[face.I5];
                    }
                }
            }
        }

        public GeneratorTimings Timings => _timings;

        public BrushMeshCache MeshCache => _meshCache;

        private static readonly BrushUpdateFlags[] s_faceUpdateFlags = [
            BrushUpdateFlags.XPlus,
            BrushUpdateFlags.XNegative,
            BrushUpdateFlags.YPlus,
            BrushUpdateFlags.YNegative,
            BrushUpdateFlags.ZPlus,
            BrushUpdateFlags.ZNegative,
            ];
    }
}
