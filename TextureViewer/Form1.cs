using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Loaders;
using Primary.RHI2;
using System.Drawing.Imaging;

namespace TextureViewer
{
    public partial class Form1 : Form
    {
        private Bitmap[] _bitmaps;

        public Form1()
        {
            _bitmaps = Array.Empty<Bitmap>();

            InitializeComponent();

            Application.SetColorMode(SystemColorMode.Dark);
        }

        private unsafe void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            using Stream? stream = File.OpenRead(openFileDialog1.FileName);

            TextureHeader header = stream.Read<TextureHeader>();
            TextureSampler sampler = stream.Read<TextureSampler>();

            if (header.FileHeader != TextureHeader.Header || header.FileVersion != TextureHeader.Version)
                return;

            textBox1.Text = $@"
TextureHeader:
    Header: {header.FileHeader}
    Version: {header.FileVersion}

    Width: {header.Width}
    Height: {header.Height}
    Depth: {header.Depth}

    Format: {header.Format}
    Flags: {header.Flags}

    Mip levels: {header.MipLevels}
    Array size: {header.ArraySize}

Sampler:
    Swizzle: [{sampler.Swizzle.R}, {sampler.Swizzle.G}, {sampler.Swizzle.B}, {sampler.Swizzle.A}]
    
    Reduction filter: {sampler.ReductionType}
    Min filter: {sampler.MinFilter}
    Mag filter: {sampler.MagFilter}
    Mip filter: {sampler.MipFilter}

    Address mode U: {sampler.AddressModeU}
    Address mode V: {sampler.AddressModeV}
    Address mode W: {sampler.AddressModeW}

    Comparison function: {sampler.ComparisonFunction}

    Border color: {sampler.BorderColor}

    Mip LOD bias: {sampler.MipLODBias}
    Min LOD: {sampler.MinLOD}
    Max LOD: {sampler.MaxLOD}

    Max anisotropy: {sampler.MaxAnisotropy}";

            if (_bitmaps.Length > 0)
            {
                foreach (Bitmap bitmap in _bitmaps)
                {
                    bitmap?.Dispose();
                }
            }

            RHIFormatInfo fi = default;
            switch (header.Format)
            {
                case TextureFormat.BC7: fi = RHIFormatInfo.Query(RHIFormat.BC7_UNorm); break;
                case TextureFormat.BC6s: fi = RHIFormatInfo.Query(RHIFormat.BC6H_SFloat16); break;
                case TextureFormat.BC6u: fi = RHIFormatInfo.Query(RHIFormat.BC6H_UFloat16); break;
                case TextureFormat.BC5u: fi = RHIFormatInfo.Query(RHIFormat.BC5_UNorm); break;
                case TextureFormat.BC4u: fi = RHIFormatInfo.Query(RHIFormat.BC4_UNorm); break;
                case TextureFormat.BC3: fi = RHIFormatInfo.Query(RHIFormat.BC3_UNorm); break;
                case TextureFormat.BC3n: fi = RHIFormatInfo.Query(RHIFormat.BC3_UNorm); break;
                case TextureFormat.BC2: fi = RHIFormatInfo.Query(RHIFormat.BC2_UNorm); break;
                case TextureFormat.BC1a: fi = RHIFormatInfo.Query(RHIFormat.BC1_Typeless); break;
                case TextureFormat.BC1: fi = RHIFormatInfo.Query(RHIFormat.BC1_UNorm); break;
                case TextureFormat.R8a: fi = RHIFormatInfo.Query(RHIFormat.R8_UNorm); break;
                case TextureFormat.RG8: fi = RHIFormatInfo.Query(RHIFormat.RG8_UNorm); break;
                case TextureFormat.RGB8: fi = new RHIFormatInfo(3, 3); break;
                case TextureFormat.RGBA8: fi = RHIFormatInfo.Query(RHIFormat.RGBA8_UNorm); break;
                case TextureFormat.R16: fi = RHIFormatInfo.Query(RHIFormat.R16_Float); break;
                case TextureFormat.RG16: fi = RHIFormatInfo.Query(RHIFormat.RG16_Float); break;
                case TextureFormat.RGBA16: fi = RHIFormatInfo.Query(RHIFormat.RGBA16_Float); break;
                case TextureFormat.R32: fi = RHIFormatInfo.Query(RHIFormat.R32_Float); break;
                case TextureFormat.RG32: fi = RHIFormatInfo.Query(RHIFormat.RG32_Float); break;
                case TextureFormat.RGBA32: fi = RHIFormatInfo.Query(RHIFormat.RGBA32_Float); break;
            }

            comboBox1.Items.Clear();

            _bitmaps = new Bitmap[header.MipLevels * header.ArraySize];
            for (int i = 0; i < header.ArraySize; i++)
            {
                Span<Bitmap> span = _bitmaps.AsSpan(i * header.MipLevels, header.MipLevels);
                for (int j = 0; j < span.Length; j++)
                {
                    int width = header.Width >> j;
                    int height = header.Height >> j;

                    comboBox1.Items.Add($"{i}: {width}x{height} ({j})");

                    byte[] rawData = new byte[fi.CalculateSize(width, height)];
                    stream.ReadExactly(rawData);

                    Bitmap bm = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    {
                        BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                        int count = width * height * 4;
                        Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                        BcDecoder decoder = new BcDecoder();
                        decoder.Options.IsParallel = true;

                        ColorRgba32[] colors = decoder.DecodeRaw(rawData, width, height, header.Format switch
                        {
                            TextureFormat.BC7 => CompressionFormat.Bc7,
                            TextureFormat.BC5u => CompressionFormat.Bc5,
                            TextureFormat.BC4u => CompressionFormat.Bc4,
                            TextureFormat.BC3 => CompressionFormat.Bc3,
                            TextureFormat.BC1 => CompressionFormat.Bc1,
                            TextureFormat.R8a => CompressionFormat.R,
                            TextureFormat.RG8 => CompressionFormat.Rg,
                            TextureFormat.RGB8 => CompressionFormat.Rgb,
                            TextureFormat.RGBA8 => CompressionFormat.Rgba,
                            _ => throw new NotImplementedException(),
                        });

                        TextureSwizzleChannel r = sampler.Swizzle.R;
                        TextureSwizzleChannel g = sampler.Swizzle.G;
                        TextureSwizzleChannel b = sampler.Swizzle.B;
                        TextureSwizzleChannel a = sampler.Swizzle.A;

                        Span<TextureSwizzleChannel> channels = [r, g, b, a];

                        for (int k = 0, l = 0; k < count; k += 4, ++l)
                        {
                            ColorRgba32 color = colors[l];

                            for (int m = 0; m < 4; m++)
                            {
                                switch (channels[m])
                                {
                                    case TextureSwizzleChannel.R: bmPixels[k + m] = color.r; break;
                                    case TextureSwizzleChannel.G: bmPixels[k + m] = color.g; break;
                                    case TextureSwizzleChannel.B: bmPixels[k + m] = color.b; break;
                                    case TextureSwizzleChannel.A: bmPixels[k + m] = color.a; break;
                                    case TextureSwizzleChannel.Zero: bmPixels[k + m] = 0; break;
                                    case TextureSwizzleChannel.One: bmPixels[k + m] = 255; break;
                                }
                            }
                        }

                        bm.UnlockBits(data);
                    }

                    _bitmaps[i * header.MipLevels + j] = bm;
#if false
switch (header.Format)
                    {
                        case TextureFormat.BC7:
                            break;
                        case TextureFormat.BC5u:
                            break;
                        case TextureFormat.BC4u:
                            break;
                        case TextureFormat.BC3:
                            break;
                        case TextureFormat.BC1:
                            {
                                Bitmap bm = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                                {
                                    BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                                    int count = width * height * 3;
                                    Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                                    BcDecoder decoder = new BcDecoder();
                                    decoder.DecodeRaw(rawData, width, height, CompressionFormat.Bc1);

                                    for (int k = 0, l = 0; k < count; k += 3, ++l)
                                    {
                                        bmPixels[k] = rawData[l];
                                        bmPixels[k + 1] = 255;
                                        bmPixels[k + 2] = 255;
                                    }

                                    bm.UnlockBits(data);
                                }

                                break;
                            }
                        case TextureFormat.R8a:
                            {
                                Bitmap bm = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                                {
                                    BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                    int count = width * height * 3;
                                    Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                                    for (int k = 0, l = 0; k < count; k += 3, ++l)
                                    {
                                        bmPixels[k] = rawData[l];
                                        bmPixels[k + 1] = 255;
                                        bmPixels[k + 2] = 255;
                                    }

                                    bm.UnlockBits(data);
                                }

                                break;
                            }
                        case TextureFormat.RG8:
                            {
                                Bitmap bm = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                                {
                                    BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                    int count = width * height * 3;
                                    Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                                    for (int k = 0, l = 0; k < count; k += 3, l += 2)
                                    {
                                        bmPixels[k] = rawData[l];
                                        bmPixels[k + 1] = rawData[l + 1];
                                        bmPixels[k + 2] = 255;
                                    }

                                    bm.UnlockBits(data);
                                }

                                break;
                            }
                        case TextureFormat.RGB8:
                            {
                                Bitmap bm = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                                {
                                    BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                    int count = width * height * 3;
                                    Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                                    rawData.CopyTo(bmPixels);

                                    bm.UnlockBits(data);
                                }

                                break;
                            }
                        case TextureFormat.RGBA8:
                            {
                                Bitmap bm = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                                {
                                    BitmapData data = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                                    int count = width * height * 4;
                                    Span<byte> bmPixels = new Span<byte>(data.Scan0.ToPointer(), count);

                                    for (int k = 0; k < count; k += 4)
                                    {
                                        bmPixels[k] = rawData[k + 3];
                                        bmPixels[k + 1] = rawData[k];
                                        bmPixels[k + 2] = rawData[k + 1];
                                        bmPixels[k + 3] = rawData[k + 2];
                                    }

                                    bm.UnlockBits(data);
                                }

                                break;
                            }
                    }
#endif
                }
            }

            pictureBox1.Image = _bitmaps[0];
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex < _bitmaps.Length)
                pictureBox1.Image = _bitmaps[comboBox1.SelectedIndex];
        }
    }
}
