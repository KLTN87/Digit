using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.CvEnum;
using System.IO;
namespace Digit
{
    public partial class Form1 : Form
    {
        string inputfile;
        Image<Bgr, byte> imgInput;
        List<String> vectorList = new List<string>();
        public Form1()
        {
            InitializeComponent();
        }
        
        private void predictToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image<Bgr, byte> tempimg = new Image<Bgr, byte>(inputfile);

            int tempimgwsize = int.Parse(tempimg.Size.Width.ToString());

            //if (tempimgwsize < 32) // khi ảnh quá nhỏ
            //{
            //    tempimg = IResize(tempimg, 32, 32);
            //}
            tempimg = IResize(tempimg, 128, 128);

            var imggrabcut = GrabcutImg(tempimg);
            var tempimg2 = IResize(imggrabcut, 128, 128);

            //var featureSIFT = getSIFTFeature(tempimg2, SIFTchoose);
            var featureSIFT = getSIFTGray(tempimg2);
            var featureHOG = getHOGFeature(tempimg2, 2304);

            var gopdactrung = concatDoubleArray(featureSIFT, featureHOG);
            var chuanhoadactrung = normalizeDoubleArray(gopdactrung);

            List<String> stringdactrung = convertDoubleArrayToStringArray(chuanhoadactrung);


            string textDT = string.Join(" ", stringdactrung);




            //string textDT = string.Join(" ", featureSIFT);// test sift

            vectorList.Add(textDT);
            print10FirstVector(vectorList);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            opf.Title = "Select multiply images";
            opf.Filter = "Image Files | *.jpg; *.jpeg; *.png";

            if (opf.ShowDialog() == DialogResult.OK)
            {
                inputfile = opf.FileName;   
                imgInput = new Image<Bgr, byte>(inputfile);
            }
            pictureBox1.Image = imgInput.ToBitmap();
        }
        private void print10FirstVector(List<String> dsVectoc)
        {
            int dem = 0;
            richTextBox1.Clear();
            foreach (String vec in vectorList)
            {

                dem++;
                richTextBox1.Text = richTextBox1.Text +  "<vector>" + vec + "</vector>";

                if (dem >= 10)
                    break;
            }

        }
        private double normalizeDouble(double value, double min, double max)
        {

            //value = Math.Round(value, 15);
            //min = Math.Round(min, 15);
            //max = Math.Round(max, 15);


            if (value == 0 && min == 0 && max == 0)
            {
                return 0;
            }

            double temp = (value - min) / (max - min);

            //Math.Round(temp, 15);

            return temp;
        }
        private double[] normalizeDoubleArray(double[] arr)
        {
            double[] terms = new double[arr.Length];

            for (int runs = 0; runs < arr.Length; runs++)
            {

                terms[runs] = normalizeDouble(arr[runs], arr.Min(), arr.Max());
            }


            return terms;
        }
        private double[] concatDoubleArray(double[] first, double[] second)
        {
            double[] result = first.Concat(second).ToArray();

            return result;
        }

        private List<String> convertDoubleArrayToStringArray(double[] arr) //double sang string khi số quá nhỏ dính ký tự thập phân E
        {
            List<String> temp = new List<String>();
            foreach (double ichuan in arr)
            {

                String tempsdt;
                if (ichuan == 0 || ichuan == 1)
                {
                    tempsdt = ichuan.ToString();
                }
                else
                {
                    tempsdt = String.Format("{0:0.00000000000000000}", ichuan);
                }
                temp.Add(tempsdt);
            }
            return temp;
        }
        //Hàm của Grabcut
        private Image<Bgr, Byte> GrabcutImg(Image<Bgr, Byte> img)
        {
            try
            {
                //Image<Gray, byte> tempimg = new Image<Gray, byte>(img.Size);
                //img = new Image<Bgr, Byte>(tempimg.Size);



                //test//
                Matrix<double> bg = new Matrix<double>(1, 65);
                bg.SetZero();
                Matrix<double> fg = new Matrix<double>(1, 65);
                fg.SetZero();
                Image<Gray, byte> mask = new Image<Gray, byte>(img.Size);
                Rectangle rect = new Rectangle(img.Cols / 10, 2, (int)((double)img.Width / (0.75)), img.Height);
                CvInvoke.GrabCut(img, mask, rect,
                   bg, fg, 5, Emgu.CV.CvEnum.GrabcutInitType.InitWithRect);
                Image<Gray, byte> mask2 = new Image<Gray, byte>(img.Size);
                ////here i set the only white pixels (foreground object ) to 1 and 0 for else
                for (int x = 0; x < mask.Cols; x++)
                {
                    for (int y = 0; y < mask.Rows; y++)
                    {
                        if (mask2[y, x].Intensity > new Gray(200).Intensity)
                        {
                            mask[y, x] = new Gray(1);
                        }
                        else
                        {

                        }
                    }
                }
                CvInvoke.GrabCut(img, mask, rect,
                     bg, fg, 5, Emgu.CV.CvEnum.GrabcutInitType.InitWithMask);
                for (int x = 0; x < mask.Cols; x++)
                {
                    for (int y = 0; y < mask.Rows; y++)
                    {
                        if (mask[y, x].Intensity == new Gray(1).Intensity || mask[y, x].Intensity == new Gray(3).Intensity)
                        {
                            mask[y, x] = new Gray(1);
                        }
                        else
                        {
                            mask[y, x] = new Gray(0);
                        }
                    }
                }
                img = img.Mul(mask.Convert<Bgr, byte>());

                img = cropNoUseBlackAreaImg(img);

                //if (showimg == true)
                //{
                //    //hiện ảnh sau khi grabcut
                //    CvInvoke.Imshow("image grabcut", img);

                //}
                //CvInvoke.WaitKey(0);


                return img;
            }

            catch
            {

            }
            return img;

        }

        public Image<Bgr, Byte> cropNoUseBlackAreaImg(Image<Bgr, Byte> img)
        {
            List<int> listNonBlack = new List<int>();
            listNonBlack = getNonBlackArea(img);
            Mat ROI = new Mat(img.Mat, new Range(listNonBlack[2], listNonBlack[3]), new Range(listNonBlack[0], listNonBlack[1])).Clone();
            Image<Bgr, Byte> temp = ROI.ToImage<Bgr, Byte>();
            return temp;
        }



        public List<int> getNonBlackArea(Image<Bgr, Byte> imgBlack)
        {
            Bitmap img = imgBlack.AsBitmap();
            Color pixelColor;
            List<int> listX = new List<int>();
            List<int> listY = new List<int>();
            List<int> listMinMaxXY = new List<int>();
            try
            {

                for (int y = 0; y < img.Height; y++) //tìm tọa độ không phải là màu đen - màu nền sau grabcut
                {
                    for (int x = 0; x < img.Width; x++)
                    {
                        pixelColor = img.GetPixel(x, y);
                        if (pixelColor.R != 0 && pixelColor.G != 0 && pixelColor.B != 0)
                        {
                            listX.Add(x);
                            listY.Add(y);
                        }
                    }
                }

                listMinMaxXY.Add(listX.Min());
                listMinMaxXY.Add(listX.Max());
                listMinMaxXY.Add(listY.Min());
                listMinMaxXY.Add(listY.Max());
                //listMinMaxXY(minx, maxx, miny, maxy);
            }

            catch
            {
                listMinMaxXY.Add(0);
                listMinMaxXY.Add(0);
                listMinMaxXY.Add(0);
                listMinMaxXY.Add(0);

            }
            return listMinMaxXY;

        }
        //hàmm của HOG
        private Image<Bgr, Byte> IResize(Image<Bgr, Byte> im, int w, int h)
        {
            return im.Resize(w, h, Emgu.CV.CvEnum.Inter.Linear);
        }
        private double[] GetVector(Image<Bgr, Byte> im, HOGDescriptor hog)
        {
            float[] temp = hog.Compute(im, Size.Empty, Size.Empty, null);
            double[] doubleArray = Array.ConvertAll(temp, x => (double)x);
            return doubleArray;

        }
        private double[] getHOGFeature(Image<Bgr, Byte> im, int numberValues)
        {
            HOGDescriptor des;

            if (numberValues == 36)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(128, 128),
                new Size(16, 16), new Size(64, 64), 9);

            }
            else if (numberValues == 144)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(128, 128),
                     new Size(16, 16), new Size(32, 32), 9);

            }

            else if (numberValues == 576)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(128, 128),
                     new Size(16, 16), new Size(8, 32), 9);

            }

            else if (numberValues == 2304)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(32, 32),
                    new Size(32, 32), new Size(8, 8), 9);

            }

            else if (numberValues == 5184)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(64, 64),
                    new Size(32, 32), new Size(8, 8), 9);

            }
            else if (numberValues == 9360)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(32, 64),
                    new Size(8, 16), new Size(8, 16), 9);

            }


            else if (numberValues == 18720)
            {
                des = new HOGDescriptor(new Size(128, 128), new Size(32, 64),
                    new Size(8, 16), new Size(8, 8), 9);

            }
            else
            {
                //des = new HOGDescriptor();
                des = new HOGDescriptor(new Size(128, 128), new Size(128, 128),
                new Size(16, 16), new Size(64, 64), 9); //36
            }

            double[] hog = GetVector(im, des);

            return hog;
        }
       
        private double[] getSIFTGray(Image<Bgr, Byte> im)
        {

            Bitmap a = convertGrayScale(im.ToBitmap());

            Mat src1 = a.ToMat();

            SIFT sift = new SIFT();
            MKeyPoint[] mKeyPoints = sift.Detect(src1, null);

            Mat sift_feature = new Mat();

            VectorOfKeyPoint vkPoint = new VectorOfKeyPoint(mKeyPoints);

            //pictureBox2.Image = sift_feature.ToBitmap();


            //hiên ảnh sift
            //if (showimg == true)
            //{
            //    Features2DToolbox.DrawKeypoints(src1, vkPoint, sift_feature, new Bgr(0, 255, 0), Features2DToolbox.KeypointDrawType.Default);
            //    CvInvoke.Imshow("image sift gray", sift_feature);

            //}

            MKeyPoint[] keyPoints = vkPoint.ToArray();

            keypoint key;
            List<keypoint> keypointsList = new List<keypoint>();
            double maxx = 0, maxy = 0;
            foreach (MKeyPoint keyPoint in keyPoints)
            {
                key = new keypoint(keyPoint.Point.X, keyPoint.Point.Y, keyPoint.Size);
                keypointsList.Add(key);
                maxx++;
                maxy++;
            }


            double[] b = tinhHistogramGray(a, keyPoints, maxx, maxy);

            double mx = b[0];
            double mn = b[0];

            for (int i = 1; i < b.Length; i++)
            {
                if (b[i] > mx)
                {
                    mx = b[i];
                }


                if (b[i] < mn)
                {
                    mn = b[i];
                }
            }
            double[] c = balanceHistogram(b, mx, mn);
            return c;
        }
        public Bitmap convertGrayScale(Bitmap img)
        {
            Bitmap gimg = new Bitmap(img.Width, img.Height);
            for (int x = 0; x < gimg.Width; x++)
            {
                for (int y = 0; y < gimg.Height; y++)
                {
                    Color pixel = img.GetPixel(x, y);
                    //byte red = pixel.R;
                    //byte green = pixel.G;
                    //byte blue = pixel.B;
                    byte a = pixel.A;
                    int grayScale = (int)((pixel.R * 0.3) + (pixel.G * 0.59) + (pixel.B * 0.11));
                    //byte gray = (byte)((red + green + blue) / 3);
                    gimg.SetPixel(x, y, Color.FromArgb(a, grayScale, grayScale, grayScale));
                }
            }
            return gimg;
        }
        public double[] balanceHistogram(double[] b, double max, double min)
        {
            double[] histogram = new double[256];
            for (int i = 0; i < b.Length; i++)
            {
                histogram[i] = ((b[i] - min) / (max - min));
            }
            return histogram;
        }
        public double[] tinhHistogramGray(Bitmap anhxam, MKeyPoint[] key, double maxx, double maxy)
        {
            double[] histogram = new double[256];
            for (int i = 0; i < maxx; i++)
            {
                for (int j = 0; j < maxy; j++)
                {
                    //if (i == j)
                    //{
                    Color color = anhxam.GetPixel((int)key[i].Point.X, (int)key[j].Point.Y);
                    byte gray = color.R; // chuyen sang anh don sac thi xam = r = g = b

                    histogram[gray]++;
                    //}
                }
            }
            return histogram;
        }
    }
}
