namespace ConfigUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string targetDir = args[0];

            string[] filesInDir = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);
            foreach (string file in filesInDir)
            {
                if (file.EndsWith(".toml"))
                    continue;

                string configFile = Path.ChangeExtension(file, ".toml");
                if (File.Exists(configFile))
                {
                    string source = File.ReadAllText(configFile);

                    File.WriteAllText(file + ".assetdat", $"asset_id = \"{Guid.CreateVersion7():n}\"\r\nmeta_version = 1\r\n\r\n{source}");
                    File.Delete(configFile);

                    Console.WriteLine(configFile);
                }
            }
        }
    }
}
