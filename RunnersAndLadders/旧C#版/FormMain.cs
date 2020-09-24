using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunnersAndLadders
{
    partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();          
        }
        private void GameBegin(object sender, EventArgs e)
        {
            //
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Bitmap bigBmp = new Bitmap("outside.bmp");
            Bitmap smallBmp = new Bitmap("inside.bmp");

            Graphics g = Graphics.FromImage(bigBmp);
            basePicture.CreateGraphics().DrawImage(bigBmp, 0, 0);


            Bitmap cover = new Bitmap(smallBmp.Width, smallBmp.Height);
            Bitmap sma = new Bitmap(smallBmp);
            Color c = Color.FromArgb(255, 255, 255);

            for (int movement = 66; movement < 300; movement += 33)
            {
                for (int i = 0; i < smallBmp.Width; ++i)
                    for (int j = 0; j < smallBmp.Height; ++j)
                        cover.SetPixel(i, j, bigBmp.GetPixel(i + movement - 33, j + movement - 33));
                basePicture.CreateGraphics().DrawImage(cover, movement - 33, movement - 33);

                for (int i = 0; i < smallBmp.Width; ++i)
                    for (int j = 0; j < smallBmp.Height; ++j)
                        if (smallBmp.GetPixel(i, j) == c)
                            sma.SetPixel(i, j, bigBmp.GetPixel(i + movement, j + movement));
                basePicture.CreateGraphics().DrawImage(sma, movement, movement);
                //Task.Delay(20).Wait();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox2.Parent = basePicture;
            pictureBox2.BackColor = Color.Transparent;

            Bitmap bigBmp = new Bitmap("outside.bmp");
            
            Bitmap smallBmp = new Bitmap("inside.bmp");
            smallBmp.MakeTransparent();
            Color c = Color.FromArgb(255, 255, 255);

            for (int i = 0; i < smallBmp.Width; ++i)
                for (int j = 0; j < smallBmp.Height; ++j)
                    if (smallBmp.GetPixel(i, j) == c)
                        smallBmp.SetPixel(i, j, Color.Transparent);

            basePicture.Image = bigBmp;
            pictureBox2.Image = smallBmp;
            
            for (int movement = 20; movement < 300; movement += 10)
            {
                pictureBox2.Top = movement + basePicture.Top;
                //pictureBox2.Left = movement + pictureBox1.Left;
                Task.Delay(20).Wait();
                
            }
        }

        Game aa = null;
        private void button3_Click(object sender, EventArgs e)
        {
            if (aa != null)
            {
                aa.GameEnd = true;
                aa.RoundEnd = true;
                aa.WaitForMainProcess();
            }
            aa = new Game("1",basePicture);
            basePicture.Focus();
        }
        
        //bool isPressing = false;
        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (aa.PlayerTask==null)//!isPressing)
            {
                //isPressing = true;
                aa.PlayerTask= Task.Factory.StartNew(() => aa.PlayerTryAct(e.KeyCode));
                await aa.PlayerTask;
                aa.PlayerTask = null;
                //isPressing = false;
            }
        }
    }
}
