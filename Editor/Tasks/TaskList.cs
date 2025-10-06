namespace Editor.Tasks
{
    public sealed class TaskList
    {
        private static WeakReference s_instance = new WeakReference(null);

        internal TaskList()
        {
            s_instance.Target = this;
        }

        private record struct RunningTask(Task Task, string Title, float Progress);
    }
}
