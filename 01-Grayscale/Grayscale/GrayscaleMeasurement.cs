using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Grayscale
{
    [MedianColumn, MinColumn, MaxColumn]
    public class GrayscaleMeasurement
    {
        private Image<Rgb24> _image;
        private ParallelOptions _parallelOptions;

        public GrayscaleMeasurement()
        {
            var path = Environment.GetEnvironmentVariable("IMAGEFILEPATH", EnvironmentVariableTarget.User);
            _image = Image.Load<Rgb24>(path);

            _parallelOptions = new()
            {
                MaxDegreeOfParallelism = 8,
            };
        }

        [Benchmark]
        public Image<Rgb24> ImageSharpGrayscale()
        {
            var gray = _image.Clone(ctx => ctx.Grayscale());
            return gray;
        }

        [Benchmark]
        public Image<L8> OriginalGrayscale()
        {
            Rgb24[] colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            L8[] gray = new L8[_image.Width * _image.Height];

            for(int i = 0; i < _image.Width * _image.Height; i++)
            {
                gray[i].PackedValue = GetGray(colorBytes[i].R, colorBytes[i].G, colorBytes[i].B);
            }

            return Image.LoadPixelData(gray, _image.Width, _image.Height);
        }

        [Benchmark]
        public Image<L8> ParallelOriginalGrayscale()
        {
            Rgb24[] colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            L8[] gray = new L8[_image.Width * _image.Height];

            Parallel.For(0, _image.Width * _image.Height, _parallelOptions, (i) =>
            {
                gray[i].PackedValue = GetGray(colorBytes[i].R, colorBytes[i].G, colorBytes[i].B);
            });

            return Image.LoadPixelData(gray, _image.Width, _image.Height);
        }

        [Benchmark]
        public unsafe Image<L8> UnsafeOriginalGrayscale()
        {
            Span<Rgb24> colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            Span<L8> gray = new L8[_image.Width * _image.Height];

            fixed (Rgb24* colorPtr = colorBytes)
            fixed (L8* grayPtr = gray)
            {
                Rgb24* colorEvil = colorPtr;
                L8* grayEvil = grayPtr;

                Parallel.For(0, _image.Width * _image.Height, _parallelOptions, (i) =>
                {
                    grayEvil[i].PackedValue = GetGray(colorEvil[i].R, colorEvil[i].G, colorEvil[i].B);
                });
            }

            return Image.LoadPixelData<L8>(gray, _image.Width, _image.Height);
        }

        private byte GetGray(byte R, byte G, byte B)
        {
            return (byte)(R * 0.2126 + G * 0.7152 + B * 0.0722);
        }
    }
}
