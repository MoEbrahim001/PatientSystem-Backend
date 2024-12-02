using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Drawing;

public class ImagePreprocessing
{
    public static Bitmap DetectAndAlignFace(string imagePath)
    {
        // Load image using Emgu CV
        var image = new Image<Bgr, byte>(imagePath);

        // Use a cascade classifier for face detection
        var faceCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_frontalface_default.xml");
        var faces = faceCascade.DetectMultiScale(image, 1.1, 10, Size.Empty);

        if (faces.Length > 0)
        {
            // Crop and resize the first detected face
            var faceRegion = faces[0];
            var face = image.GetSubRect(faceRegion);
            var resizedFace = face.Resize(160, 160, Inter.Linear);

            // Convert to Bitmap
            return resizedFace.ToBitmap();
        }

        return null;
    }
}
