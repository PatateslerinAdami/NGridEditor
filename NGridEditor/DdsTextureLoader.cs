using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;

namespace LoLNGRIDConverter
{
    public static class DdsTextureLoader
    {
        public static ImageSource LoadDDS(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var image = Dds.Create(stream, new PfimConfig()))
                    {
                        PixelFormat format;

                        switch (image.Format)
                        {
                            case ImageFormat.Rgb24:
                                format = PixelFormats.Bgr24;
                                break;
                            case ImageFormat.Rgba32:
                                format = PixelFormats.Bgra32;
                                break;
                            case ImageFormat.Rgb8:
                                format = PixelFormats.Gray8;
                                break;
                            default:
                                throw new Exception($"Unsupported pixel format: {image.Format}");
                        }

                        var bitmap = BitmapSource.Create(
                            image.Width,
                            image.Height,
                            96.0, 96.0,
                            format,
                            null,
                            image.Data,
                            image.Stride);

                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}