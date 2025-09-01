using CommandLine;
using Editor.Processors;

namespace Editor.Runners
{
    internal class TextureRunner
    {
        public void Execute(Span<string> args)
        {
            ParserResult<RunnerArguments> result = Parser.Default.ParseArguments<RunnerArguments>(args.ToArray());
            if (result.Errors.Any())
            {
                bool shouldReturnBad = false;

                foreach (Error error in result.Errors)
                {
                    Console.WriteLine(error.ToString());

                    if (error.StopsProcessing)
                    {
                        shouldReturnBad = true;
                    }
                }

                if (shouldReturnBad)
                {
                    return;
                }
            }

            RunnerArguments value = result.Value;

            TextureProcessor processor = new TextureProcessor();
            processor.Execute(new TextureProcessorArgs
            {
                AbsoluteFilepath = Path.GetFullPath(value.Input),
                AbsoluteOutputPath = Path.GetFullPath(value.Output),

                FlipVertical = value.FlipVertical,
                ImageFormat = value.ImageFormat,
                CutoutDither = value.CutoutDither,
                CutoutThreshold = value.CutoutThreshold,
                GammaCorrect = value.GammaCorrect,
                PremultipliedAlpha = value.PremultipliedAlpha,
                MipmapFilter = value.MipmapFilter,
                MaxMipmapCount = value.MaxMipmapCount,
                MinMipmapSize = value.MinMipmapSize,
                GenerateMipmaps = value.GenerateMipmaps,
                ImageType = value.ImageType,
                ScaleAlphaForMipmaps = value.ScaleAlphaForMipmaps
            });
        }

        private class RunnerArguments
        {
            [Option('i', "input")]
            public string Input { get; set; } = string.Empty;
            [Option('o', "output")]
            public string Output { get; set; } = string.Empty;

            [Option("flip")]
            public bool FlipVertical { get; set; } = false;
            [Option('f', "format")]
            public TextureImageFormat ImageFormat { get; set; } = TextureImageFormat.RGBA8;
            [Option("cutoutDither")]
            public bool CutoutDither { get; set; } = false;
            [Option("cutoutThreshold")]
            public byte CutoutThreshold { get; set; } = 127;
            [Option("gammaCorrect")]
            public bool GammaCorrect { get; set; } = false;
            [Option("premultipliedAlpha")]
            public bool PremultipliedAlpha { get; set; } = false;
            [Option("filter")]
            public TextureMipmapFilter MipmapFilter { get; set; } = TextureMipmapFilter.Box;
            [Option("maxMips")]
            public int MaxMipmapCount { get; set; } = int.MaxValue;
            [Option("minMips")]
            public int MinMipmapSize { get; set; } = 1;
            [Option("mipmaps")]
            public bool GenerateMipmaps { get; set; } = false;
            [Option('t', "type")]
            public TextureImageType ImageType { get; set; } = TextureImageType.Colormap;
            [Option("scaleMipAlpha")]
            public bool ScaleAlphaForMipmaps { get; set; } = false;
        }
    }
}
