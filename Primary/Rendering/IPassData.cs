namespace Primary.Rendering
{
    public interface IPassData
    {
        public void Clear();
    }

    public class GenericPassData : IPassData
    {
        public void Clear() { }
    }
}
