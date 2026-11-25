namespace Primary.Console
{
    internal interface IGenericConsoleVar
    {
        public Type VariableType { get; }

        public object GetValue();
        public void SetValue(object value);
    }

    public sealed class ConsoleVar<T> : IGenericConsoleVar, IEquatable<ConsoleVar<T>>, IEquatable<T> where T : notnull
    {
        public T Value;

        public ConsoleVar(T value)
        {
            Value = value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string? ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is ConsoleVar<T> conVar ? conVar.Equals(this) : (obj is T t ? t.Equals(Value) : false);
        }

        public bool Equals(ConsoleVar<T>? rhs)
        {
            return rhs != null && Value.Equals(rhs.Value);
        }

        public bool Equals(T? rhs)
        {
            return Value.Equals(rhs);
        }

        public object GetValue() => Value;

        public void SetValue(object value)
        {
            if (value is T t)
                Value = t;
        }

        public Type VariableType => typeof(T);

        public static implicit operator T(ConsoleVar<T> var) => var.Value;
        public static implicit operator ConsoleVar<T>(T value) => new ConsoleVar<T>(value);
    }
}
