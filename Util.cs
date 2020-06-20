using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ocr
{
    class Util
    {

        #region 灰阶
        public static Bitmap Gray(Bitmap image)
        {
            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int stride = bitmapData.Stride;
            System.IntPtr Scan0 = bitmapData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                int nOffset = stride - image.Width * 3;
                byte red, green, blue;
                for (int y = 0; y < image.Height; ++y)
                {
                    for (int x = 0; x < image.Width; ++x)
                    {
                        blue = p[0];
                        green = p[1];
                        red = p[2];
                        p[0] = p[1] = p[2] = (byte)(.299 * red + .587 * green + .114 * blue);
                        p += 3;
                    }
                    p += nOffset;
                }
            }
            image.UnlockBits(bitmapData);
            return image;
        }
        #endregion

        #region 固定阈值 二值化
        public static Bitmap Thresholding(Bitmap image, byte threshold)
        {
            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)bitmapData.Scan0;
                int offset = bitmapData.Stride - image.Width * 4;
                byte R, G, B, gray;
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        R = p[2];
                        G = p[1];
                        B = p[0];
                        gray = (byte)((R * 19595 + G * 38469 + B * 7472) >> 16);
                        if (gray >= threshold)
                        {
                            p[0] = p[1] = p[2] = 255;
                        }
                        else
                        {
                            p[0] = p[1] = p[2] = 0;
                        }
                        p += 4;
                    }
                    p += offset;
                }
            }
            image.UnlockBits(bitmapData);
            return image;
        }
        #endregion

        #region otsu阈值法 二值化
        public static byte Otsu(Bitmap image)
        {
            byte threshold = 0;
            int width = image.Width;
            int height = image.Height;
            int[] hist = new int[256];
            int AllPixelNumber = 0, PixelNumberSmall = 0, PixelNumberBig = 0;
            double MaxValue, AllSum = 0, SumSmall = 0, SumBig, ProbabilitySmall, ProbabilityBig, Probability;
            BitmapData data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)data.Scan0;
                int offset = data.Stride - width * 4;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        hist[p[0]]++;
                        p += 4;
                    }
                    p += offset;
                }
                image.UnlockBits(data);

                //计算灰度为I的像素出现的概率
                for (int i = 0; i < 256; i++)
                {
                    AllSum += i * hist[i];     //   质量矩
                    AllPixelNumber += hist[i];  //  质量
                }

                MaxValue = -1.0;
                for (int i = 0; i < 256; i++)
                {
                    PixelNumberSmall += hist[i];
                    PixelNumberBig = AllPixelNumber - PixelNumberSmall;
                    if (PixelNumberBig == 0)
                    {
                        break;
                    }
                    SumSmall += i * hist[i];
                    SumBig = AllSum - SumSmall;
                    ProbabilitySmall = SumSmall / PixelNumberSmall;
                    ProbabilityBig = SumBig / PixelNumberBig;
                    Probability = PixelNumberSmall * ProbabilitySmall * ProbabilitySmall + PixelNumberBig * ProbabilityBig * ProbabilityBig;
                    if (Probability > MaxValue)
                    {
                        MaxValue = Probability;
                        threshold = (byte)i;
                    }
                }
            }
            return threshold;
        }
        #endregion

        public static double getAngleTray(Point mac0, Point mac1, Point maxp0, Point maxp1)
        {
            var a = Math.Atan2(mac1.Y - mac0.Y, mac1.X - mac0.X);
            var b = Math.Atan2(maxp1.Y - maxp0.Y, maxp1.X - maxp0.X);
            double angle = (double)(180 * (b - a)) / (double)Math.PI;
            if (angle > 180)
                angle -= 360;
            if (angle < -180)
                angle += 360;
            return angle;
        }

        #region FindVertices
        public static Image<Bgr, byte> FindVertices(Image<Bgr, byte> image, ref Point left, ref Point right)
        {
            #region Canny and edge detection
            UMat cannyEdges = new UMat();

            CvInvoke.Canny(image, cannyEdges, 60, 180);
            #endregion

            List<RotatedRect> boxList = new List<RotatedRect>(); //旋转的矩形框
            Point center = new Point(); ;
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;

                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.08, true);
                        var area = CvInvoke.ContourArea(approxContour, false);
                        //仅考虑面积大于300000的轮廓
                        if (area > 100000)
                        {
                            if (approxContour.Size == 4) //轮廓有4个顶点
                            {

                                Point[] pts = approxContour.ToArray();
                                //求四边形中心点？坐标
                                int x_average = 0;
                                int y_average = 0;
                                int x_sum = 0;
                                int y_sum = 0;
                                for (int j = 0; j < 4; j++)
                                {
                                    x_sum += pts[j].X;
                                    y_sum += pts[j].Y;
                                }
                                x_average = x_sum / 4;
                                y_average = y_sum / 4;
                                center = new Point(x_average, y_average);
                                for (int j = 0; j < 4; j++)
                                {
                                    if (pts[j].X < center.X && pts[j].Y < center.Y)
                                    {
                                        left = pts[j];//左上角点
                                    }
                                    if (pts[j].X > center.X && pts[j].Y < center.Y)
                                    {
                                        right = pts[j];//右上角点
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            cannyEdges.Dispose();
            return image;
        }
        #endregion

        public static void displayImg(string winName, Image<Bgr, byte> image)
        {
            CvInvoke.NamedWindow(winName, Emgu.CV.CvEnum.WindowFlags.Normal);
            CvInvoke.Imshow(winName, image);

            CvInvoke.WaitKey(0);
            CvInvoke.DestroyWindow(winName);
        }
    }
}
