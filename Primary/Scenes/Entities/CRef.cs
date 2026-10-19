using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Arch.Core;
using Primary.Components;

namespace Primary.Scenes.Entities
{
    public sealed class CRef<T> : ICRef, IEquatable<CRef<T>>, IEquatable<T>, IEqualityOperators<CRef<T>, CRef<T>, bool>
        where T : struct, IComponent
    {
        private SceneEntity _entity;
        private bool _isNull;

        internal CRef()
        {
            _entity = SceneEntity.Null;
            _isNull = true;
        }

        internal CRef(SceneEntity entity)
        {
            _entity = entity;
            _isNull = entity.HasComponent<T>();
        }

        void ICRef.DestroyInternal()
        {
            _isNull = true;
        }

        public override string? ToString() => ComponentRef.ToString();
        public override int GetHashCode() => ComponentRef.GetHashCode();

        public override bool Equals(object? obj) => obj is CRef<T> comp && Equals(comp);

        public bool Equals(CRef<T>? other)
        {
            if (other == null || other.IsNull)
            {
                return false;
            }
            else if (this == other)
            {
                return true;
            }
            else
            {
                ref T comp = ref ComponentRef;
                ref T otherComp = ref other.ComponentRef;
                if (comp is IEquatable<T> equatable)
                    return equatable.Equals(otherComp);
                else
                    return comp.Equals(otherComp);
            }
        }

        public bool Equals(T other)
        {
            ref T comp = ref ComponentRef;
            if (comp is IEquatable<T> equatable)
                return equatable.Equals(other);
            else
                return comp.Equals(other);
        }

        public ref T ComponentRef
        {
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return ref _entity.GetComponent<T>();
            }
        }

        public bool IsNull => _isNull || _entity.IsNull;

        public static bool operator ==(CRef<T>? left, CRef<T>? right) => left?.Equals(right) ?? right == null;
        public static bool operator !=(CRef<T>? left, CRef<T>? right) => !(left == right);

        public static implicit operator T(CRef<T> cRef) => cRef.ComponentRef;
    }

    internal interface ICRef
    {
        public void DestroyInternal();
    }
}
