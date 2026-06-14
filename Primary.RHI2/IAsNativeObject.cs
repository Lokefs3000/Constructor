namespace Primary.RHI
{
    public unsafe interface IAsNativeObject<T> where T : unmanaged
    {
        public T* GetAsNative();
    }
}
