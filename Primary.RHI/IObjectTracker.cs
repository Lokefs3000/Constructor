namespace Primary.RHI
{
    public interface IObjectTracker
    {
        public void ObjectCreated(Resource resource);
        public void ObjectDestroyed(Resource resource);

        public void ObjectRenamed(Resource resource, string newName);
    }
}
