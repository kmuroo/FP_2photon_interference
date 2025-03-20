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
        int n = 0; 
        int dt=1000;  //パラメーター初期値1000 ms毎
        int job_number = 0;
        string[] recieved_str = new string[0];// 可変長配列
        int[] ch1 = new int[0];
        int[] ch2 = new int[0];
        bool cancel = false;
        string default_portname = "COM5"; // デフォルトCOMポート（FP班のノートPCではこのポートになっている）
        string default_portname2 = "COM6"; // CH2用デフォルトCOMポート
        string[] current_portname = new string[16];
        string[] current_portname2 = new string[16];
        bool[] ch_enable = new bool[] { false, false, false };
        string[] ports;
        int number_of_coms = 0;
        int number_of_ch = 0;
        SerialPort[] serialPort = new SerialPort[3];
        System.Windows.Forms.Button[] button_open = new System.Windows.Forms.Button[3];
        System.Windows.Forms.Button[] button_active = new System.Windows.Forms.Button[3];
        System.Windows.Forms.Label[] label_ch = new System.Windows.Forms.Label[3];

        public Form1()
        {
            InitializeComponent();
            add_serial_portname();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort[1] = serialPort1;
            serialPort[2] = serialPort2;
            button_open[1] = button1;
            button_open[2] = button7;
            button_active[1] = button8;
            button_active[2] = button9;
            label_ch[1] = label2;
            label_ch[2] = label12;

            textBox2.AppendText("測定間隔：");
            textBox2.AppendText(dt.ToString() + " ms\r\n\r\n");
            //textBox2.AppendText("接続されているカウンターを探索しています\r\n\r\n");

            if (ports.Length >= 2)
            {
                comopen_general(1);
                comopen_general(2);
            }
            else
            {
                if (ports.Length >= 1)
                {
                    comopen_general(1);
                }
            }

            textBox2.AppendText("\r\nカウンター数/ポート数: " + number_of_ch + "/" + number_of_coms + "\r\n\r\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            comopen_general(1);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            comopen_general(2);
        }

        private void comopen_general(int ch_id)
        {
            if (serialPort[ch_id].IsOpen == false)
            {
                if (ch_id == 1)
                {
                    serialPort[ch_id].PortName = comboBox1.SelectedItem.ToString();// comboBox1.SelectedText;
                }
                else
                {
                    serialPort[ch_id].PortName = comboBox2.SelectedItem.ToString();
                }
                try
                {
                    serialPort[ch_id].Open();
                    while (serialPort[ch_id].IsOpen == false)
                    {
                        // ポートがオープンするまで待つ
                    }
                    serialPort[ch_id].BaudRate = 115200; // ArduinoソースのSerial　ボーレートと合わせる
                    serialPort[ch_id].ReadTimeout = 5000; // エコーバックを5秒まつ
                    serialPort[ch_id].ReadExisting(); //バッファを空に
                    string a = "C";
                    string c = arduino_send_recv_general(ch_id,"c");
                    if (a[0] == c[0]) //接続成功すれば 'C' がArduinoから返ってくる
                    {
                        textBox2.AppendText("COMポートCH" + ch_id + " (Arduino Uno)を接続しました\r\n");
                        ch_enable[ch_id] = true;
                        number_of_ch++;
                        button_active[ch_id].BackColor = Color.DarkGray;
                    }
                    else
                    {
                        throw new Exception("COMポートからが反応がない、または誤った反応をしています。もう一度「Open COM」ボタンを押して、反応がおかしいようであれば他のCOMポートを試してください。\r\n\r\n");
                    }
                }
                catch (Exception ex)
                {
                    textBox2.AppendText(ex.Message);
                    serialPort[ch_id].Close();
                    return;
                }

                button_open[ch_id].Text = "Connected";
                label_ch[ch_id].Text = serialPort[ch_id].PortName;
                button_open[ch_id].BackColor = Color.DarkGray;
                
                if (ch_enable[1] || ch_enable[2])
                {
                    label4.Text = dt.ToString();
                    label6.Text = "STATUS: Ready to RUN";
                    label6.BackColor = Color.LightCyan;
                }
            }
            else
            {
                comclose_general(ch_id);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)//パラメーター設定
        {
            if(ch_enable[1] || ch_enable[2])
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
                    textBox2.AppendText("周期(ms)：");
                    textBox2.AppendText(dt.ToString() + "\r\n\r\n");

                    if (ch_enable[1] || ch_enable[2])
                    {
                        label4.Text = dt.ToString();
                        label6.Text = "STATUS: Ready to RUN";
                        label6.BackColor = Color.LightCyan;
                    }

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


        private void add_serial_portname()
        {
            ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
                comboBox2.Items.Add(port);
            }
            comboBox1.SelectedIndex = comboBox1.Items.Count-1;
            comboBox2.SelectedIndex = comboBox2.Items.Count-2;

            if (comboBox1.FindString(default_portname)>0)
            {
                comboBox1.SelectedIndex = comboBox1.FindString(default_portname);
            }
            if (comboBox2.FindString(default_portname2) > 0)
            {
                comboBox2.SelectedIndex = comboBox2.FindString(default_portname);
            }
            number_of_coms = ports.Length;
        }

        private void comclose_general(int ch_id)
        {
            if (serialPort[ch_id].IsOpen == true)
            {
                arduino_send_recv_general(ch_id,"s");

                serialPort[ch_id].Close();
                textBox2.AppendText("COMポートCH" + ch_id + "を切断しました\r\n\r\n");
                if (ch_enable[ch_id])
                {
                    ch_enable[ch_id] = false;
                    button_active[ch_id].BackColor = SystemColors.Control;
                }
                button_open[ch_id].Text = "Open COM";
                button_open[ch_id].BackColor = SystemColors.Control;
                if (!(ch_enable[1] || ch_enable[2]))
                {
                    label6.Text = "STATUS: Not Ready";
                    label6.BackColor = Color.Yellow;
                }
                number_of_ch--;
            }

        }
 
        private void button3_Click(object sender, EventArgs e)
        {
            if (ch_enable[1] || ch_enable[2])
            {
                Task<int> task = Task.Run(() => {
                    return arduino_run();
                });
            }
            else
            {
                textBox2.AppendText("チャンネルが選択されていません。先にCH1ボタン, CH2ボタンを押して,どちらかまたは両方を選択してください。\r\n\r\n");
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
                button_open[1].Enabled = false;    // Open COM (CH1) ボタンを無効に
                button_open[2].Enabled = false;    // Open COM (CH2) ボタンを無効に
                button2.Enabled = false;    // 設定 ボタンを無効に
                button3.Enabled = false;    // Run ボタンを無効に
                button4.Enabled = false;    // Save DATA ボタンを無効に
                button5.Enabled = true;     // 中断 ボタンを有効に
                button_active[1].Enabled = false;     // CH1アクティベートボタンを有効に
                button_active[2].Enabled = false;     // CH2アクティベートボタンを有効に

                if (ch_enable[1]) { arduino_send_recv_general(1,"t" + dt.ToString()); }
                if (ch_enable[2]) { arduino_send_recv_general(2,"t" + dt.ToString()); }
                button3.Text = "Sampling...";
                button3.BackColor = Color.LightCyan;
                string data;
                job_number++;

                textBox2.AppendText("サンプリング開始 Job " + job_number + "\r\n");
                string header = "Index";
                if (ch_enable[1]) { header += "\tCH1"; }
                if (ch_enable[2]) { header += "\tCH2"; }
                header += "\r\n";
                textBox2.AppendText(header);

                for (i = 0; i < 2147483647; i++)
                    {
                    if(cancel == true)
                    {
                        break;
                    }
                    Array.Resize(ref recieved_str, i + 1);
                    if (ch_enable[1])
                    {
                        recieved_str[i] = serialPort1.ReadLine();
                    }
                    if (ch_enable[2])
                    {
                        recieved_str[i] += serialPort2.ReadLine();
                    }
                    textBox2.AppendText(i+"\t"+recieved_str[i]+"\r\n");
                }
                n = i;

                if(cancel == true)
                {
                    if (ch_enable[1]) { arduino_send_recv_general(1,"s"); }
                    if (ch_enable[2]) { arduino_send_recv_general(2,"s"); }
                    cancel = false;
                    if (ch_enable[1]) { serialPort[1].ReadExisting(); } //バッファを空に
                    if (ch_enable[2]) { serialPort[2].ReadExisting(); }
                    textBox2.AppendText("サンプリング終了 ");
                    textBox2.AppendText("(サンプル数 = " + n + ")\r\n\r\n");
                }

                Array.Resize(ref ch1, 100);
                Array.Resize(ref ch2, 100);
                
                for (i = 0; i < n; i++)
                {
                    data = recieved_str[i];
                    int.TryParse(data.Substring(0, data.IndexOf("\t")), out ch1[i]); //TryPerse() はヌル文字列のときfalse(bool値)を返す 値は0が変数に書き込まれる
                    int.TryParse(data.Substring(data.IndexOf("\t") + 1), out ch2[i]);
                }
              

                button3.Text = "Run";
                button3.BackColor = SystemColors.Control;
                label6.Text = "STATUS: Ready to RUN";
                label6.BackColor = Color.LightCyan;


                
                button_open[1].Enabled = true;    // Open COM (CH1) ボタンを有効に
                button_open[2].Enabled = true;    // Open COM (CH2) ボタンを有効に
                button2.Enabled = true;    // 設定 ボタンを有効に
                button3.Enabled = true;    // Run ボタンを有効に
                button4.Enabled = true;    // Save DATA ボタンを有効に
                button5.Enabled = false;   // 中断ボタンを無効に
                button_active[1].Enabled = true;   // CH1アクティベートボタンを無効に
                button_active[2].Enabled = true;   // CH2アクティベートボタンを無効に

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
            string header = "# Index";
            string temp_str;

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
            if (ch_enable[1]) { header += ", CH1"; }
            if (ch_enable[2]) { header += ", CH2"; }
            sw.WriteLine(header);
            for (i=0; i < n; i++)
            {
                temp_str = i.ToString();
                if (ch_enable[1]){ temp_str += ",\t" + ch1[i]; }
                if (ch_enable[2]) { temp_str += ",\t" + ch2[i]; }
                sw.WriteLine(temp_str);
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
                //COMポートを閉じて終了
                if (serialPort[1].IsOpen)
                {
                    comclose_general(1);
                }
                if (serialPort[2].IsOpen)
                {
                    comclose_general(2);
                }
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

        private string arduino_send_recv_general(int ch_id, string send_message)//Arduinoにメッセージ送信、コールバックあり
        {
            string recv_message;

            serialPort[ch_id].ReadExisting(); //バッファを空に

            serialPort[ch_id].Write(send_message); //メッセージ送信
            serialPort[ch_id].ReadTimeout = 5000;
            recv_message = serialPort[ch_id].ReadLine();//メッセージ受信

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

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (ch_enable[1])
            {
                ch_enable[1] = false;
                button8.BackColor = SystemColors.Control;
                if (!ch_enable[2])
                {
                    label6.Text = "STATUS: Not Ready";
                    label6.BackColor = Color.Yellow;
                }
            }
            else
            {
                if (serialPort[1].IsOpen)
                {
                    ch_enable[1] = true;
                    button8.BackColor = Color.DarkGray;
                    label6.Text = "STATUS: Ready to RUN";
                    label6.BackColor = Color.LightCyan;
                }
            }
            
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (ch_enable[2])
            {
                ch_enable[2] = false;
                button9.BackColor = SystemColors.Control;
                if (!ch_enable[2])
                {
                    label6.Text = "STATUS: Not Ready";
                    label6.BackColor = Color.Yellow;
                }
            }
            else
            {
                if (serialPort[2].IsOpen)
                {
                    ch_enable[2] = true;
                    button9.BackColor = Color.DarkGray;
                    label6.Text = "STATUS: Ready to RUN";
                    label6.BackColor = Color.LightCyan;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }
    }
}
