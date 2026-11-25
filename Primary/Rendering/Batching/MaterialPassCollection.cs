using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Assets;
using Primary.Collections;
using Primary.Rendering.Assets;
using Primary.Services;
using Primary.Threading;
using Primary.Utility;

namespace Primary.Rendering.Batching
{
    public sealed class MaterialPassCollection : IService
    {
        private readonly Dictionary<MaterialAsset, MaterialPass?[]> _passes;
        private readonly Dictionary<ulong, MaterialPass> _idToPass;

        private readonly ConcurrentBag<MaterialPassUpdate> _passUpdates;

        private readonly IdCounter _passIds;

        internal MaterialPassCollection()
        {
            _passes = new Dictionary<MaterialAsset, MaterialPass?[]>();
            _idToPass = new Dictionary<ulong, MaterialPass>();

            _passUpdates = new ConcurrentBag<MaterialPassUpdate>();

            _passIds = new IdCounter();
        }

        private void HandlePassUpdates()
        {
            using RentedList<MaterialPassUpdate> passUpdates = new RentedList<MaterialPassUpdate>();
            while (_passUpdates.TryTake(out MaterialPassUpdate passUpdate))
            {
                passUpdates.Add(passUpdate);
            }

            passUpdates.AsSpan().Sort();

            foreach (ref readonly MaterialPassUpdate passUpdate in passUpdates.AsSpan())
            {
                MaterialAsset material = passUpdate.Material;
                switch (passUpdate.Type)
                {
                    case UpdateType.Register:
                        {
                            ref MaterialPass?[] passes = ref CollectionsMarshal.GetValueRefOrAddDefault(_passes, passUpdate.Material, out bool exists)!;
                            if (!exists)
                            {
                                passes = new MaterialPass?[Enum.GetNames<MaterialShaderType>().Length];
                            }

                            for (int i = 0; i < passes.Length; ++i)
                            {
                                MaterialShaderType shaderType = (MaterialShaderType)i;

                                ShaderAsset? shader = material.GetShaderOrNull(shaderType);
                                ROPropertyBlock properties = material.GetPropertyBlockOrNull(shaderType);

                                if (shader != null && !properties.IsNull)
                                {
                                    passes[i] = new MaterialPass(
                                        ((ulong)shaderType << 48) | ((ulong)shader.UniqueIndex << 24) | (uint)material.UniqueIndex,
                                        material,
                                        shader,
                                        properties);

                                    // Whoa! This is ass
                                    _idToPass.Add(passes[i].GetValueOrDefault().PassId, passes[i].GetValueOrDefault());
                                }
                                else
                                {
                                    passes[i] = null;
                                }
                            }

                            break;
                        }
                    case UpdateType.Unregister:
                        {
                            if (_passes.Remove(material, out MaterialPass?[]? values))
                            {
                                foreach (MaterialPass? passNullable in values)
                                {
                                    if (passNullable.HasValue)
                                    {
                                        _idToPass.Remove(passNullable.GetValueOrDefault().PassId);
                                    }
                                }
                            }

                            break;
                        }
                    case UpdateType.Update:
                        {
                            if (_passes.TryGetValue(material, out MaterialPass?[]? passes))
                            {
                                for (int i = 0; i < passes.Length; ++i)
                                {
                                    MaterialShaderType shaderType = (MaterialShaderType)i;

                                    ShaderAsset? shader = material.GetShaderOrNull(shaderType);
                                    ROPropertyBlock properties = material.GetPropertyBlockOrNull(shaderType);

                                    if (shader != null && !properties.IsNull)
                                    {
                                        passes[i] = new MaterialPass(
                                            ((ulong)shaderType << 48) | ((ulong)shader.UniqueIndex << 24) | (uint)material.UniqueIndex,
                                            material,
                                            shader,
                                            properties);

                                        // Whoa! It's repeated and still looks ass
                                        _idToPass[passes[i].GetValueOrDefault().PassId] = passes[i].GetValueOrDefault();
                                    }
                                    else
                                    {
                                        passes[i] = null;
                                    }
                                }
                            }

                            break;
                        }
                }
            }
        }

        internal JobHandle ScheduleUpdates()
        {
            if (!_passUpdates.IsEmpty)
            {
                return JobScheduler.Schedule(HandlePassUpdates, JobPriority.Realtime, true);
            }

            return JobHandle.Completed;
        }

        internal void RegisterMaterial(MaterialAsset material)
        {
            if (!_passes.ContainsKey(material))
            {
                _passUpdates.Add(new MaterialPassUpdate(UpdateType.Register, material));
            }
        }

        internal void UnregisterMaterial(MaterialAsset material)
        {
            if (_passes.ContainsKey(material))
            {
                _passUpdates.Add(new MaterialPassUpdate(UpdateType.Unregister, material));
            }
        }

        internal void UpdateMaterial(MaterialAsset material)
        {
            if (_passes.ContainsKey(material))
            {
                _passUpdates.Add(new MaterialPassUpdate(UpdateType.Update, material));
            }
        }

        internal bool TryGetMaterial(MaterialAsset material, out ReadOnlySpan<MaterialPass?> passes)
        {
            if (_passes.TryGetValue(material, out MaterialPass?[]? array))
            {
                passes = array;
                return true;
            }

            passes = null;
            return false;
        }

        internal bool TryGetPass(ulong passId, [NotNullWhen(true)] out MaterialPass materialPass)
        {
            return _idToPass.TryGetValue(passId, out materialPass);
        }

        private readonly record struct MaterialPassUpdate(UpdateType Type, MaterialAsset Material) : IEquatable<MaterialPassUpdate>, IComparable<MaterialPassUpdate>
        {
            public int CompareTo(MaterialPassUpdate other) => ((byte)Type).CompareTo((byte)other.Type);
        }

        internal enum UpdateType : byte
        {
            Register = 0,
            Unregister,
            Update
        }
    }
}
