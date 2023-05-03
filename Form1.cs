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
            set { wait(1000); Console.WriteLine(value); label3.Text = value; }
        }

        public Boolean GetGenesisNodeCheckedStatus
        {
            get { return checkBox1.Checked; }
        }
        public Boolean GetMiningNodeCheckedStatus
        {
            get { return checkBox2.Checked; }
        }
        public Boolean GetDNSseedNodeCheckedStatus
        {
            get { return checkBox3.Checked; }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //you always need to have a basic peer node to participate in network
            this.checkBox4.Checked = true;
            this.checkBox4.Enabled = false;
            //on load disable connect to network button until your node is set up
            this.button2.Enabled = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //gray out and check all other checkboxes if you check genesis node
            if (this.checkBox1.Checked == true)
            {
                this.checkBox2.Checked = true;
                this.checkBox2.Enabled = false;
                this.checkBox3.Checked = true;
                this.checkBox3.Enabled = false;
                
            }
            else
            {
                this.checkBox2.Enabled = true;
                this.checkBox3.Enabled = true;
                
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //set up node button clicked
            if (this.checkBox1.Checked == true)
            {
                //set up genesis node
                //empty blockchaindata folder
                Blockchain myblockchain = new Blockchain();
                //int difficultyTest = myblockchain.Difficulty;

                Console.WriteLine("SETTING UP GENESIS NODE");
                Console.WriteLine(myblockchain.Difficulty);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //connect to network button
            Console.WriteLine("CLIENT CONNECTING TO SELF SERVER NODE");
            // Client thisClient = new Client("127.0.0.1");
            //IDGSocketClient client = new IDGSocketClient();
            //client.Connect("localhost", 3000);

        }


        private void button3_Click(object sender, EventArgs e)
        {
            //view blockchain data button
            //check if local blockchain data available
            //make user select a block
            //show all transactions in block by timestamp
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //view constitution
            //check local drive for populated constitution
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //make transaction
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
