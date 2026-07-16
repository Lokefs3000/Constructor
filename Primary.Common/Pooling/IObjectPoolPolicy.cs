namespace Primary.Pooling
{
    public interface IObjectPoolPolicy<T>
    {
        public T Create();
        public bool Return(ref T obj);
    }

    public sealed record class DefaultObjectPolicy<T> : IObjectPoolPolicy<T> where T : new()
    {
        public T Create() => new T();
        public bool Return(ref T _) => true;
    }
}
