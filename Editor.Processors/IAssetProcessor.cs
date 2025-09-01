namespace Editor.Processors
{
    public interface IAssetProcessor
    {
        public bool Execute(object args); //boxing sucks ik
    }
}
