using DlibDotNet;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PatientSystem.Models
{
    public class FaceLandMarkDetection
    {
        private static string modelPath = @"C:\Users\dell\source\repos\PatientSystem\shape_predictor_68_face_landmarks.dat"; // Path to the Dlib model
        private static FrontalFaceDetector detector = Dlib.GetFrontalFaceDetector();
        private static ShapePredictor sp = ShapePredictor.Deserialize(modelPath);

        public static void DetectFacialLandmarks(Bitmap image)
        {
            try
            {
                // Convert Bitmap to Dlib Matrix
                var dlibImage = ConvertBitmapToDlibMatrix(image);

                // Detect faces
                var faces = detector.Operator(dlibImage);

                if (faces.Length == 0)
                {
                    Console.WriteLine("No faces detected.");
                    return;
                }

                // Loop over all detected faces
                foreach (var face in faces)
                {
                    // Get landmarks for each detected face
                    var shape = sp.Detect(dlibImage, face);

                    // Draw landmarks on the face
                    DrawLandmarks(image, shape); // Use FullObjectDetection here
                }

                Console.WriteLine("Landmarks detected and drawn.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during landmark detection: {ex.Message}");
            }
        }

        private static Matrix<byte> ConvertBitmapToDlibMatrix(Bitmap bitmap)
        {
            // Lock the bitmap's bits
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            // Create a byte array to store the pixel data
            int byteCount = bitmapData.Stride * bitmap.Height;
            byte[] pixelData = new byte[byteCount];
            Marshal.Copy(bitmapData.Scan0, pixelData, 0, byteCount);

            // Unlock the bitmap's bits
            bitmap.UnlockBits(bitmapData);

            // Convert to Dlib Matrix
            var matrix = new Matrix<byte>(bitmap.Height, bitmap.Width);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int pixelIndex = (y * bitmapData.Stride) + (x * 3); // 3 bytes per pixel (RGB)
                    matrix[y, x] = pixelData[pixelIndex]; // Use the R channel as the grayscale value
                }
            }

            return matrix;
        }

        private static void DrawLandmarks(Bitmap image, FullObjectDetection shape)
        {
            using (var g = Graphics.FromImage(image))
            {
                for (int i = 0; i < shape.Parts; i++)
                {
                    var point = shape.GetPart((uint)i); // Cast i to uint
                    g.FillEllipse(Brushes.Red, point.X - 2, point.Y - 2, 4, 4); // Draw a small red circle at each landmark
                }
            }

            // Save the result with landmarks drawn
            image.Save(@"C:\Users\dell\source\repos\PatientSystem\FacelandMark");
        }
    }
}
