using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Primary.Common;

namespace EditorUI.Common
{
    public record struct LayoutValue<T> : IEquatable<LayoutValue<T>>, IEquatable<Nullable<T>>, IEquatable<T> where T : struct
    {
        private readonly T _value;
        private byte _state;

        public LayoutValue()
        {
            _value = default;
            _state = IsNullMask | HasChangedMask;
        }

        public LayoutValue(T value)
        {
            _value = value;
            _state = HasChangedMask;
        }

        public void ClearChanged()
        {
            _state &= unchecked((byte)~HasChangedMask);
        }

        public readonly T GetValueOrDefault() => _value;
        public readonly T GetValueOrDefault(T defaultValue) => HasValue ? _value : defaultValue;

        public readonly bool Equals(T? other) => HasValue ? (other.HasValue && Equals(other.Value)) : !other.HasValue;

        public readonly bool Equals(T other)
        {
            if (!HasValue)
                return false;
            return other is IEquatable<T> equatable ? equatable.Equals(_value) : other.Equals(_value);
        }

        public override int GetHashCode() => HasValue ? _value.GetHashCode() : 0;
        public override string? ToString() => HasValue ? _value.ToString() : string.Empty;

        public readonly T Value => Flags.HasFlag(_state, IsNullMask) ? throw new NullReferenceException() : _value;
        
        public readonly bool HasValue => !Flags.HasFlag(_state, IsNullMask);
        public readonly bool HasChanged => Flags.HasFlag(_state, HasChangedMask);

        public static bool operator ==(LayoutValue<T> lhs, Nullable<T> rhs) => lhs.Equals(rhs);
        public static bool operator !=(LayoutValue<T> lhs, Nullable<T> rhs) => !(lhs == rhs);

        public static bool operator ==(LayoutValue<T> lhs, T rhs) => lhs.Equals(rhs);
        public static bool operator !=(LayoutValue<T> lhs, T rhs) => !(lhs == rhs);

        public static implicit operator LayoutValue<T>(Nullable<T> value) => value.HasValue ? new LayoutValue<T>(value.Value) : new LayoutValue<T>();
        public static implicit operator LayoutValue<T>(T value) => new LayoutValue<T>(value);

        public static implicit operator Nullable<T>(LayoutValue<T> value) => value.HasValue ? new Nullable<T>(value._value) : null;
        public static explicit operator T(LayoutValue<T> value) => value.Value;

        private const byte IsNullMask = 1 << 0;
        private const byte HasChangedMask = 1 << 1;
    }
}
