using Tensorflow;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class TensorHelper
{
    public static Tensor ConvertImageToTensor(Bitmap bitmap)
    {
        var byteArray = ImageToByteArray(bitmap);
        var tensor = new Tensor(byteArray);
        return tensor;
    }

    private static byte[] ImageToByteArray(Bitmap image)
    {
        using (var ms = new MemoryStream())
        {
            image.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }
    }
}
