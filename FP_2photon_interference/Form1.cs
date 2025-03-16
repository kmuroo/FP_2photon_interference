using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FP_2photon_interference
{
    public partial class Form1 : Form
    {
        //private void add_serial_portnames();
        int n = 0; 
        int dt=1000;  //パラメーター初期値1000 ms毎
        int job_number = 0;
        string[] recieved_str = new string[0];// 可変長配列
        int[] ch1 = new int[0];
        int[] ch2 = new int[0];
        bool cancel = false;
        string default_portname = "COM5"; // デフォルトCOMポート（FP班のノートPCではこのポートになっている）
        string[] current_portname = new string[16];

        public Form1()
        {
            InitializeComponent();
            add_serial_portname();
            comopen();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            comopen();
        }

        private void comopen()
        {
            if (serialPort1.IsOpen == false)
            {
                serialPort1.PortName = comboBox1.SelectedItem.ToString();// comboBox1.SelectedText;
                serialPort1.Open();
                while (serialPort1.IsOpen == false)
                {
                    // ポートがオープンするまで待つ
                }
                serialPort1.BaudRate = 115200; // ArduinoソースのSerial　ボーレートと合わせる
                serialPort1.ReadTimeout = 5000;

                try
                {

                    serialPort1.ReadExisting(); //バッファを空に
                    
                    string a = "C";
                    string c = arduino_send_recv("c");
                    if (a[0] == c[0]) //接続成功すれば 'C' がArduinoから返ってくる
                    {
                        textBox2.AppendText("COMポート(Arduino Uno)を接続しました\r\n\r\n");
                    }
                    else
                    {
                        throw new Exception("COMポートからが反応がない、または誤った反応をしています。もう一度「Open COM」ボタンを押して、反応がおかしいようであれば他のCOMポートを試してください。\r\n\r\n");
                    }
                }
                catch (Exception ex)
                {
                    textBox2.AppendText(ex.Message);
                    serialPort1.Close();
                    return;
                }

                button1.Text = "Connected";
                label2.Text = serialPort1.PortName;
                button1.BackColor = Color.DarkGray;
                //arduino_send_recv("c"); //念のためArduinoのTime割り込み停止指示

                //パラメーター初期値設定
                //arduino_send_recv("t" + dt.ToString());
                textBox2.AppendText("周期(ms)：");
                textBox2.AppendText(dt.ToString() + "\r\n\r\n");

                label4.Text = dt.ToString();
                label6.Text = "STATUS: Ready to RUN";
                label6.BackColor = Color.LightCyan;
            }
            else
            {
                comclose();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)//パラメーター設定
        {
            if(serialPort1.IsOpen == true)
            {
                label6.Text = "STATUS: Not Ready";
                label6.BackColor = Color.Yellow;
                try
                {   
                    //現在の設定を設定ダイアログに格納
                    //Form2_Tex1は、Publicのように、外部からアクセスできるModifiersである必要がある
                    Form2.Instance.Form2_Text1 = dt.ToString();
                    //Form2を表示する
                    Form2.Instance.ShowDialog();
                    
                    //設定ダイアログでの設定値について
                    //パラメーター範囲外トリミング処理 1～50000
                    if ( int.Parse(Form2.Instance.Form2_Text1) > 50000)
                    {
                        Form2.Instance.Form2_Text1 = "50000";
                    }
                    else if( int.Parse(Form2.Instance.Form2_Text1) < 1)
                    {
                        Form2.Instance.Form2_Text1 = "1";
                    }
                   
                    //ダイアログでの設定値を現在の設定値にする
                    dt = int.Parse(Form2.Instance.Form2_Text1);
                    //新設定値をArduinoに書き込み
                    //arduino_send_recv("t" + dt.ToString());
                    textBox2.AppendText("周期(ms)：");
                    textBox2.AppendText(dt.ToString() + "\r\n\r\n");

                    label4.Text = dt.ToString();
                    label6.Text = "STATUS: Ready to RUN";
                    label6.BackColor = Color.LightCyan;

                }
                catch (Exception err)
                {
                    MessageBox.Show("設定: " + err.Message);
                }

            }
            else
            {
                textBox2.AppendText("接続されていません。先にOpen COMボタンを押して接続してください。\r\n");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void add_serial_portname()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                 comboBox1.Items.Add(port);
            }
            comboBox1.SelectedIndex = comboBox1.Items.Count-1;
           
            if(comboBox1.FindString(default_portname)>0)
            {
                    comboBox1.SelectedIndex = comboBox1.FindString(default_portname);
            }               
        }

        private void comclose()
        {
            if(serialPort1.IsOpen == true) 
            {
                arduino_send_recv("s");

                serialPort1.Close();
                textBox2.AppendText("COMポートを切断しました\r\n\r\n");
                button1.Text = "Open COM";
                button1.BackColor = SystemColors.Control;
                label6.Text = "STATUS: Not Ready";
                label6.BackColor = Color.Yellow;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == true)
            {
                Task<int> task = Task.Run(() => {
                    return arduino_run();
                });
            }
            else
            {
                textBox2.AppendText("接続されていません。先にOpen COMボタンを押して接続してください。\r\n\r\n");
            }
        }

        private int arduino_run()
        {
            int i;
            Array.Resize(ref recieved_str, 0); // 配列サイズ初期化=0
            Array.Resize(ref ch1, 0);
            Array.Resize(ref ch2, 0);
            try
            {
                button1.Enabled = false;    // Open COM ボタンを無効に
                button2.Enabled = false;    // 設定 ボタンを無効に
                button3.Enabled = false;    // Run ボタンを無効に
                button4.Enabled = false;    // Save DATA ボタンを無効に
                button5.Enabled = true;     // 中断 ボタンを有効に

                //arduino_send_recv("r");// →obsolate
                arduino_send_recv("t" + dt.ToString());
                button3.Text = "Sampling...";
                button3.BackColor = Color.LightCyan;
                string data;
                job_number++;


                textBox2.AppendText("サンプリング開始 Job " + job_number + "\r\n");
                textBox2.AppendText("Index\tCH1\tCH2\r\n");
               
                for (i = 0; i < 2147483647; i++)
                    {
                    if(cancel == true)
                    {
                        break;
                    }
                    Array.Resize(ref recieved_str, i + 1);
                    recieved_str[i] = serialPort1.ReadLine();
                    textBox2.AppendText(i+"\t"+recieved_str[i]+"\r\n");
                }
                n = i;

                if(cancel == true)
                {
                    arduino_send_recv("s"); 
                    cancel = false;
                    serialPort1.ReadExisting(); //バッファを空に
                    textBox2.AppendText("サンプリング終了\r\n\r\n");
                }

                Array.Resize(ref ch1, n);
                Array.Resize(ref ch2, n);
                for (i = 0; i < n; i++)
                {
                    data = recieved_str[i];
                    ch1[i] = int.Parse(data.Substring(0, data.IndexOf("\t")));
                    ch2[i] = int.Parse(data.Substring(data.IndexOf("\t") + 1));
                }
              

                button3.Text = "Run";
                button3.BackColor = SystemColors.Control;
                label6.Text = "STATUS: Ready to RUN";
                label6.BackColor = Color.LightCyan;


                
                button1.Enabled = true;    // Open COM ボタンを有効に
                button2.Enabled = true;    // 設定 ボタンを有効に
                button3.Enabled = true;    // Run ボタンを有効に
                button4.Enabled = true;    // Save DATA ボタンを有効に
                button5.Enabled = false;   // 中断ボタンを無効に

            }
            catch (Exception err)
            {
                MessageBox.Show("Run エラー:" + err.Message);
            }
            return 1;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            int i;
            //int t;
            string data;

            saveFileDialog1.FileName = "data" + job_number.ToString() +".csv";
            saveFileDialog1.Filter = "csv型式ファイル(*.csv)|*.csv";
            saveFileDialog1.Title = "Save an DATA File";
            saveFileDialog1.ShowDialog();

            System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();
            System.Text.Encoding.GetEncoding("shift_jis");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(fs);
            
            DateTime date = DateTime.Now;
            sw.WriteLine("# " + date.ToString("yyyy/MM/dd") + " Job " + job_number.ToString());
            sw.WriteLine("# Duration: " + dt.ToString() + " ms");
            sw.WriteLine("# Index, CH1, CH2");
            for (i=0; i < n; i++)
            {
                data = recieved_str[i];
                sw.WriteLine(i + ",\t" + ch1[i] + ",\t" + ch2[i]);
            }
           
            sw.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 質問ダイアログを表示する
            DialogResult result = MessageBox.Show("終了しますか？", "質問", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(result == DialogResult.No)
            {
                // はいボタンをクリックしたときはウィンドウを閉じる
                e.Cancel = true;
            }
            else
            {
                comclose(); //COMポートを閉じて終了
            }
        }
 
        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            cancel = true;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void arduino_send(string send_message) //Arduinoにメッセージ送信、コールバックなし
        {
            serialPort1.ReadExisting(); //バッファを空に
            serialPort1.Write(send_message); //メッセージ送信
            serialPort1.ReadExisting(); //バッファを空に
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private string arduino_send_recv(string send_message)//Arduinoにメッセージ送信、コールバックあり
        {
            string recv_message;

            serialPort1.ReadExisting(); //バッファを空に

            serialPort1.Write(send_message); //メッセージ送信
            while (serialPort1.BytesToRead == 0)
            {
                // バッファにデータが溜まるまでまつ（エコーバック用）
            }
            recv_message = serialPort1.ReadLine();//メッセージ受信

            return recv_message;        
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
