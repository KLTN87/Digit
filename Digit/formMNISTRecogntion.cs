using Emgu.CV;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EmgucvDemo
{
    public partial class formMNISTRecogntion : Form
    {
        //Point StartPosition = Point.Empty;
        private bool isMouseDown = false;

        private Net model = null;

        public formMNISTRecogntion()
        {
            InitializeComponent();
        }

        private void loadONNXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                //dialog.Filter = "ONNX Files (*.onnx;)|*.onnx;";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    model = DnnInvoke.ReadNetFromTensorflow(dialog.FileName);
                    MessageBox.Show("Model loaded");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //StartPosition = e.Location;
            isMouseDown = true;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            //StartPosition = Point.Empty;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (isMouseDown == true)
                {
                    if (pictureBox1.Image == null)
                    {
                        Bitmap bm = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        pictureBox1.Image = bm;
                    }

                    using (Graphics g = Graphics.FromImage(pictureBox1.Image))
                    {
                        g.FillEllipse(Brushes.Black, e.X, e.Y, 15, 15);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    }
                    pictureBox1.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image = null;
                pictureBox1.Invalidate();
                lblDigit.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (pictureBox1.Image == null)
                {
                    MessageBox.Show("Draw a digit or open images");
                }

                if (model == null)
                {
                    MessageBox.Show("Please load model");
                }

                Bitmap bm = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
                pictureBox1.DrawToBitmap(bm, pictureBox1.ClientRectangle);



                var img = bm.ToImage<Gray, byte>()
                    .Not()
                    .SmoothGaussian(3)
                    .Resize(28, 28, Emgu.CV.CvEnum.Inter.Cubic)
                    .Mul(1 / 255.0f);

                var input = DnnInvoke.BlobFromImage(img);
                model.SetInput(input);
                var output = model.Forward();

                float[] array = new float[10];
                output.CopyTo(array);

                var prob = SoftMax(array);
                int index = Array.IndexOf(prob, prob.Max());
                lblDigit.Text = index.ToString();

                chart1.Series.Clear();
                chart1.Titles.Clear();

                chart1.Series.Add("Hist");
                chart1.Titles.Add("Probabilities");

                for (int i = 0; i < prob.Length; i++)
                {
                    chart1.Series["Hist"].Points.AddXY(i, prob[i]);
                }

            }
            catch (Exception ex)
            {
                lblMessage.Text = ex.Message;
            }
        }

        private float[] SoftMax(float[] arr)
        {
            var exp = (from a in arr
                       select (float)Math.Exp(a))
                      .ToArray();
            var sum = exp.Sum();

            return exp.Select(x => x / sum).ToArray();
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            opf.Title = "Select images";
            opf.Filter = "Image Files | *.jpg; *.jpeg; *.png";

            if (opf.ShowDialog() == DialogResult.OK)
            {
                string filename = opf.FileName;
                var img = Image.FromFile(filename);
                img = invertImageColors(img);
                pictureBox1.Image = resizeImage(img, new Size(180, 180));


            }
        }


        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }



        public static Image invertImageColors(Image img)
        {

            Bitmap pic = new Bitmap(img);


            for (int y = 0; (y <= (pic.Height - 1)); y++)
            {
                for (int x = 0; (x <= (pic.Width - 1)); x++)
                {
                    Color inv = pic.GetPixel(x, y);
                    inv = Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    pic.SetPixel(x, y, inv);
                }
            }

            return pic;
        }

    }
}