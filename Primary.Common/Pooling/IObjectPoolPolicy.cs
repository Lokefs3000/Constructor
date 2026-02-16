namespace Primary.Pooling
{
    public interface IObjectPoolPolicy<T>
    {
        public T Create();
        public bool Return(ref T obj);
    }

    public struct DefaultObjectPolicy<T> : IObjectPoolPolicy<T> where T : new()
    {
        public T Create() => new T();
        public bool Return(ref T _) => true;
    }
}
