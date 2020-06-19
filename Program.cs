using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace ocr
{
    class Program
    {
        static void Main(string[] args)
        {
            string imagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "assets", "123.png");
            Bitmap image = (Bitmap)Image.FromFile(imagePath);
            //Console.WriteLine(image.Width);

            Util.Gray(image);
            image.Save(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "new.png"));

            string ocrPath = "./Properties/tessdata";
            //string language = "chi_sim";
            string language = "eng";

            Tesseract ocr = new Tesseract(ocrPath, language, OcrEngineMode.Default);

            ocr.SetImage(image.ToImage<Bgr, byte>());
            ocr.Recognize();

            Tesseract.Character[] characters = ocr.GetCharacters();


            string strRsult = string.Empty;
            try
            {
                strRsult = ocr.GetUTF8Text();

                Console.WriteLine(strRsult);
            }
            catch (Exception)
            {
                Console.WriteLine("Error");
                return;
            }
        }
    }
}
