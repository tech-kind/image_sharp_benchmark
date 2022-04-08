using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Threshold
{
    [MedianColumn, MinColumn, MaxColumn]
    public class ThresholdMeasurements
    {
        private Image<Rgb24> _image;
        private ParallelOptions _parallelOptions;

        public ThresholdMeasurements()
        {
            var path = Environment.GetEnvironmentVariable("IMAGEFILEPATH", EnvironmentVariableTarget.User);
            _image = Image.Load<Rgb24>(path);

            _parallelOptions = new()
            {
                MaxDegreeOfParallelism = 8,
            };
        }

        [Benchmark]
        public Image<Rgb24> ImageSharpThreshold()
        {
            var th = _image.Clone(ctx => ctx.Grayscale().BinaryThreshold((float)0.5));
            return th;
        }

        [Benchmark]
        public Image<L8> OriginalThreshold()
        {
            byte thresh = 127;
            Rgb24[] colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            L8[] gray = new L8[_image.Width * _image.Height];

            for (int i = 0; i < _image.Width * _image.Height; i++)
            {
                var value = GetGray(colorBytes[i].R, colorBytes[i].G, colorBytes[i].B);
                gray[i].PackedValue = GetThreshValue(value, thresh);
            }

            return Image.LoadPixelData(gray, _image.Width, _image.Height);
        }

        [Benchmark]
        public Image<L8> ParallelOriginalThreshold()
        {
            byte thresh = 127;
            Rgb24[] colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            L8[] gray = new L8[_image.Width * _image.Height];

            Parallel.For(0, _image.Width * _image.Height, _parallelOptions, (i) =>
            {
                var value = GetGray(colorBytes[i].R, colorBytes[i].G, colorBytes[i].B);
                gray[i].PackedValue = GetThreshValue(value, thresh);
            });

            return Image.LoadPixelData(gray, _image.Width, _image.Height);
        }

        [Benchmark]
        public unsafe Image<L8> UnsafeOriginalThreshold()
        {
            byte thresh = 127;
            Span<Rgb24> colorBytes = new Rgb24[_image.Width * _image.Height];
            _image.CopyPixelDataTo(colorBytes);

            Span<L8> th = new L8[_image.Width * _image.Height];

            fixed (Rgb24* colorPtr = colorBytes)
            fixed (L8* thPtr = th)
            {
                Rgb24* colorEvil = colorPtr;
                L8* thEvil = thPtr;

                Parallel.For(0, _image.Width * _image.Height, _parallelOptions, (i) =>
                {
                    var value = GetGray(colorEvil[i].R, colorEvil[i].G, colorEvil[i].B);
                    thEvil[i].PackedValue = GetThreshValue(value, thresh);
                });
            }

            return Image.LoadPixelData<L8>(th, _image.Width, _image.Height);
        }

        private byte GetGray(byte R, byte G, byte B)
        {
            return (byte)(R * 0.2126 + G * 0.7152 + B * 0.0722);
        }

        private byte GetThreshValue(byte value, int threshold)
        {
            byte th = value switch
            {
                byte i when i < threshold => 0,
                _ => 255,
            };
            return th;
        }
    }
}
