using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace ocr
{
    class Program
    {
        static void Main(string[] args)
        {
            string winName = "test-win";
            string imagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "assets", "1234L.jpg");
            Bitmap image = new Bitmap(imagePath);

            Util.Gray(image);
            byte threshold = Util.Otsu(image);
            //Console.WriteLine("threshold is: " + threshold);
            Util.Thresholding(image, threshold);

            // 这一步调用 ToImage 之后 图片会自动逆时针转90度
            Image<Bgr, byte> image1 = image.ToImage<Bgr, byte>();
            //image1 = image1.Rotate(90, new Bgr(0, 0, 0));
            Util.displayImg(winName + "1", image1);

            //使用高斯滤波去除噪声
            // 这里new Size(3, 3) 的3  很关键，好像太大 太小 都找不对顶点
            CvInvoke.GaussianBlur(image1, image1, new Size(3, 3), 3);
            Util.displayImg(winName + "2", image1);

            Point left = new Point();
            Point right = new Point();
            image1 = Util.FindVertices(image1, ref left, ref right);
            Util.displayImg(winName + "3", image1);

            // 画线 看找到的顶点对不对
            //Point[] points = new Point[2];
            //points[0] = left;
            //points[1] = right;
            //image1.DrawPolyline(points, true, new Bgr(Color.Red), 10);
            //image1.Save(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "new.png"));

            Console.WriteLine("1 left: " + left.X + "  " + left.Y);
            Console.WriteLine("1 right: " + right.X + "  " + right.Y);

            double angle = Util.getAngleTray(left, right, left, new Point(right.X, left.Y));
            Console.WriteLine("angle is: " + angle);

            // 旋转
            image1 = image1.Rotate(90 + angle, new Bgr(0, 0, 0));
            Util.displayImg(winName + "4", image1);

            // 旋转后的顶点
            image1 = Util.FindVertices(image1, ref left, ref right);
            Util.displayImg(winName + "5", image1);

            int cutWidth = 700;
            int cutHeight = 800;
            Bitmap MasterMap = new Bitmap(cutWidth, cutHeight, PixelFormat.Format32bppRgb);
            Graphics g = Graphics.FromImage(MasterMap);

            Rectangle dest = new Rectangle(0, 0, cutWidth, cutHeight);
            Rectangle source = new Rectangle(left.X + 300, left.Y + 200, cutWidth, cutHeight);
            g.DrawImage(image1.AsBitmap(), dest, source, GraphicsUnit.Pixel);

            // 这里调用后 图片没有旋转  不知道为什么
            image1 = MasterMap.ToImage<Bgr, byte>();
            Util.displayImg(winName + "6", image1);

            #region 开操作 没什么效果    https://www.cnblogs.com/ssyfj/p/9277688.html
            //var image1 = image.ToImage<Bgr, byte>();
            //Mat kernel1 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(10, 10), new Point(0, 0));
            //image1.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Gradient, kernel1, new Point(0, 0), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            //image1.Save(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "new.png"));
            #endregion

            string ocrPath = "./Properties/tessdata";
            //string language = "chi_sim";
            string language = "eng";

            Tesseract ocr = new Tesseract(ocrPath, language, OcrEngineMode.Default);

            ocr.SetImage(image1);
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
