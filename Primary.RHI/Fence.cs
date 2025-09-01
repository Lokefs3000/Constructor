namespace Primary.RHI
{
    public abstract class Fence : IDisposable
    {
        public abstract string Name { set; }

        public abstract ulong CompletedValue { get; }

        public abstract void Dispose();

        public abstract void Signal(ulong value);

        /// <summary>
        /// Waits on the fence if <paramref name="value"/> compared to <seealso cref="CompletedValue"/> by <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="value">The value to compare</param>
        /// <param name="condition">The condition to use</param>
        public abstract void Wait(ulong value, FenceCondition condition, int timeout = -1);
    }

    //Is "value" (Condition) "CompletedValue"
    public enum FenceCondition : byte
    {
        Always = 0,
        Equals,
        LessThan,
        LessThanOrEquals,
        GreaterThan,
        GreaterThanOrEquals,
    }
}
