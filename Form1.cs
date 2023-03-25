using System;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GUIDEMO
{

    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        public void wait(int milliseconds)
        {
            var timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0) return;

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
                // Console.WriteLine("stop wait timer");
            };

            while (timer1.Enabled)
            {
                Application.DoEvents();
            }
        }

        public string SetLabel3Text
        {
            get { return label3.Text; }
            set { wait(1000);  label3.Text = value; }
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //set up mother node
            //empty blockchaindata folder
            Blockchain myblockchain = new Blockchain();
            //int difficultyTest = myblockchain.Difficulty;

            Console.WriteLine("TEST");
            Console.WriteLine(myblockchain.Difficulty);
        }

    }
}
