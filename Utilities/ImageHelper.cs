using System.Drawing.Imaging;
using Emgu.CV.CvEnum;
using Emgu.CV;

namespace TFTController.Utilities
{
    public static class ImageHelper
    {
        // Helper: Convert a Bitmap to a Mat using a MemoryStream.
        public static Mat BitmapToMat(Bitmap bmp)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                byte[] data = ms.ToArray();
                Mat dst = new Mat();
                CvInvoke.Imdecode(data, ImreadModes.Color, dst);
                return dst;
            }
        }
    }
}
