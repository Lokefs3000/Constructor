namespace Primary.Utility.Scopes
{
    public readonly record struct SemaphoreScope : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public SemaphoreScope(SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
