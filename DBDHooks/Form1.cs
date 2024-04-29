using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace DBDHooks
{
    public partial class Form1 : Form
    {
        private Bitmap targetImage;
        private int part = 1;
        private System.Windows.Forms.Timer timer;
        private const int maxParts = 6; 
        private bool processing = false; 

        public Form1()
        {
            InitializeComponent();
            this.Resize += Form1_Resize;
            notifyIcon1.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            button1.Click += Button1_Click;

            targetImage = new Bitmap("C:\\Users\\Cezar\\Desktop\\ada.png");

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000; 
            timer.Tick += Timer_Tick;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!processing && part <= maxParts)
            {
                processing = true; 

                CaptureScreenRegions();
                part++;

                processing = false; 
            }
            else if (part > maxParts)
            {
                timer.Stop();
            }
        }

        private void CaptureScreenRegions()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string screenshotsFolder = Path.Combine(folderPath, "dbdhooks_screenshots");

            if (!Directory.Exists(screenshotsFolder))
            {
                Directory.CreateDirectory(screenshotsFolder);
            }

            Rectangle[] regions = new Rectangle[4];
            regions[0] = new Rectangle(80, 410, 175 - 80, (770 - 410) / 4);
            regions[1] = new Rectangle(80, 410 + regions[0].Height, 175 - 80, (770 - 410) / 4);
            regions[2] = new Rectangle(80, 410 + 2 * regions[0].Height, 175 - 80, (770 - 410) / 4);
            regions[3] = new Rectangle(80, 410 + 3 * regions[0].Height, 175 - 80, (770 - 410) / 4);

            for (int i = 0; i < regions.Length; i++)
            {
                string fileName = $"survivor{i + 1}_{part}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(screenshotsFolder, fileName);
                Rectangle region = regions[i];
                Bitmap screenCapture = new Bitmap(region.Width, region.Height);
                using (Graphics g = Graphics.FromImage(screenCapture))
                {
                    g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size);
                }

                screenCapture.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                if (ImageContains(screenCapture, targetImage))
                {
                    MessageBox.Show("Target image detected!");
                }
            }
        }

        private bool ImageContains(Bitmap sourceImage, Bitmap target)
        {
            using (Image<Bgr, byte> source = sourceImage.ToImage<Bgr, byte>())
            using (Image<Bgr, byte> template = target.ToImage<Bgr, byte>())
            {
                using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    double threshold = 0.5;
                    return maxValues[0] > threshold;
                }
            }
        }
    }
}
