namespace Primary.RHI2
{
    public unsafe interface IAsNativeObject<T> where T : unmanaged
    {
        public T* GetAsNative();
    }
}
