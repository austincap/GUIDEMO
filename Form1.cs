using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static GUIDEMO.Transaction;
using static GUIDEMO.BasicPeerNode;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

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
            this.checkBox4.Enabled = true;
            //on load disable connect to network button until your node is set up
            //this.button2.Enabled = false;
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
                GenesisNode.createGenesisBlock(MiningNode.Instance);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //connect to network button
            Console.WriteLine("CLIENT CONNECTING TO SELF SERVER NODE");

            this.SetLabel3Text = "CREATING CLIENT";
            //IDGSocketClient client = new IDGSocketClient();
            //CLIENT SHOULD ITERATE THOUGH HARDCODED DNS LIST FIRST
            Console.WriteLine("CHECK NETWORK FOR ACTIVE NODES");
            if(BasicPeerNode.storageFolderEndsIn1 == true)
            {
                IDGSocketClient.Singleton.Connect("127.0.0.1", 3000, this);
            }
            else
            {
                IDGSocketClient.Singleton.Connect("127.0.0.1", 3001, this);
            }
                
        }




        private void button3_Click(object sender, EventArgs e)
        {
            //view blockchain data button
            Form2 form2 = new Form2();
            form2.Show();
            Console.WriteLine("TEST");
            BasicPeerNode.LoadBinaryFile(form2);
            //check if local blockchain data available
            //make user select a block
            //show all transactions in block by timestamp
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //view constitution
            //check local drive for populated constitution
        }

        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return HexadecimalEncoding.ByteArrayToString(bytes);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //make transaction
            var txSubType = this.comboBox1.SelectedValue; //assume CITIZEN for now
            txSubType = "CITIZEN";
            Console.WriteLine(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            string txToAddress = GenHash(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            var txFromAddress = "Genesis Admin";
            var votecoinAmount = Decimal.ToDouble(this.numericUpDown1.Value);
            var txName = this.textBox1.Text;
            var txDesc = this.richTextBox1.Text;
            var txAction = this.comboBox2.GetItemText(this.comboBox2.SelectedItem);
            Transaction newTransaction = new Transaction(TransactionSubType.CITIZEN, txFromAddress, txToAddress, votecoinAmount, txName, txDesc, txAction);

            Hashtable addresses = null;
            string testingFolderString = ".\\blockchaindata" + BasicPeerNode.endingNumberOfFolder + "\\0.bin";
            string testingFolderString2 = ".\\blockchaindata" + BasicPeerNode.endingNumberOfFolder;
            // Open the file containing the data that you want to deserialize.
            FileStream fs = new FileStream(testingFolderString, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                addresses = (Hashtable)formatter.Deserialize(fs);
            }
            catch (SerializationException ex)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + ex.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

            //string pathString = "blockchaindata";
            string txdata = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", txSubType, txFromAddress, txToAddress, votecoinAmount, txName, txDesc, txAction);
            string txid = GenHash(txdata);
            addresses.Add(txid, txdata);
            
            string pathString = System.IO.Path.Combine(testingFolderString2, "0.bin");
            //Format the object as Binary  
            System.IO.Stream ms = File.OpenWrite(pathString);
            //It serialize the employee object  
            formatter.Serialize(ms, addresses);
            ms.Flush();
            ms.Close();
            ms.Dispose();

            //Transaction trx1 = new Transaction(TransactionSubType.CITIZEN, "00000000000000000", GenesisUserID, 0.0, "Genesis Admin", "The ID of the person who created the genesis block.", "CREATE");
            //theMiningNode.PendingTransactions.Add(trx1);
            //Transaction trx2 = new Transaction(GenesisUserID, "666453343", 0.0, "LAW", "NOUN", "a law created using this software", "CREATE");
            //Transaction trx1 = new Transaction("0000000000000000", GenesisUserID, );
            //var jsonString = JsonConvert.SerializeObject(trx1);
            //Console.WriteLine(jsonString);

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }


    }
}
