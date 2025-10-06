using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Editor.LegacyGui.Data
{
    public record struct DecoratedValue<T> : DecoratedValue_ImplAdd<T>, IEqualityComparer<T>
    {
        private T _value;
        private T _decorated;

        public DecoratedValue(T value)
        {
            _value = value;
            _decorated = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decorate(T value)
        {
            _decorated = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Undecorate()
        {
            _decorated = _value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T? x, T? y)
        {
            return x?.Equals(y) ?? false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode([DisallowNull] T obj)
        {
            return obj.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _decorated?.GetHashCode() ?? -1;
        }

        [UnscopedRef]
        public ref T Value => ref _value;
        [UnscopedRef]
        public ref readonly T Decorated => ref _decorated;

        T DecoratedValue_ImplAdd<T>.Value { set => _value = value; }
        //T DecoratedValue_ImplAdd<T>.Decorated { set => _decorated = value; }

        public static implicit operator DecoratedValue<T>(T value) => new DecoratedValue<T>(value);
        public static explicit operator T(DecoratedValue<T> value) => value.Value;
    }

    public interface DecoratedValue_ImplAdd<T>
    {
        public T Value { set; }
        //public T Decorated { set; }
    }
}
