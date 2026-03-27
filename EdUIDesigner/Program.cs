namespace EdUIDesigner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (Designer designer = new Designer(args))
            {
                designer.Run();
            }
        }
    }
}
