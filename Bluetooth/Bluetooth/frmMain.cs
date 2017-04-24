﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;

namespace Bluetooth
{
    public partial class frmMain : Form
    {
        SerialPort _serialPort = new SerialPort();
        string inputData = String.Empty; // Chuỗi rỗng.
        delegate void SetTextData(string text);
        private string _path = @"E:\data";

        private DateTime time;

        private string tembuf = null;
        private string humbuf = null;

        TextBox tb = new TextBox();

        public frmMain()
        {
            InitializeComponent();

            timer.Start();
            timer.Interval = 1000; // 1 giây      

            _serialPort.DataReceived += _serialPort_DataReceived;
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;

            // Đọc dữ liệu từ cổng đang hoạt động
            inputData = port.ReadExisting();

            if (inputData != String.Empty)
                SetText(inputData);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Quét các cổng đang kết nối với PC.
            cbName.DataSource = SerialPort.GetPortNames();
        }

        private void btConnectControl_Click(object sender, EventArgs e)
        {
            // Nếu chưa kết nối với bất kì Port nào thì thoát.
            if (cbName.Text == "")
                return;

            if (!_serialPort.IsOpen)
            {
                _serialPort.PortName = cbName.Text;

                try
                {
                    // Mở kết nối nếu đã ngắt
                    _serialPort.Open();

                    lbState.ForeColor = Color.Green;
                    lbState.Text = "Đang kết nối";

                    btConnectControl.Text = "Ngắt kết nối";

                    time = DateTime.Now;
                    tbDataReading.Text += time + Environment.NewLine;
                }
                catch(Exception)
                {
                    lbState.ForeColor = Color.Red;
                    lbState.Text = "Không thể kết nối";
                }                
            }
            else if (_serialPort.IsOpen)
            {
                // Đóng kết nối nếu đang kết nối
                _serialPort.Close();

                lbState.ForeColor = Color.Red;
                lbState.Text = "Đã ngắt kết nối";
                btConnectControl.Text = "Kết nối";

                tbDataReading.Text += DateTime.Now + Environment.NewLine;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!_serialPort.IsOpen)
            {
                lbState.ForeColor = Color.Red;
                lbState.Text = "Đã mất kết nối";
            }
            else if (_serialPort.IsOpen)
            {
                lbState.ForeColor = Color.Green;
                lbState.Text = "Đang kết nối";
            }
        }

        private void cbName_TextChanged(object sender, EventArgs e)
        {
            // Hiển thị ra các thông số của Port.
            tbBaudRate.Text = _serialPort.BaudRate.ToString();
            tbDataBits.Text = _serialPort.DataBits.ToString();
            tbParity.Text = _serialPort.Parity.ToString();
            tbHandShake.Text = _serialPort.Handshake.ToString();
        }

        private void SetText(string data)
        {
            if (tbDataReading.InvokeRequired)
            {
                SetTextData std = new SetTextData(SetText);

                this.Invoke(std, new object[] { data });
            }
            else
                tbDataReading.Text += data + "\n";
        }

        private void btShow_Click(object sender, EventArgs e)
        {
            DirectoryInfo dic = new DirectoryInfo(@"E:\data");

            // Duyet qua tung file txt de lay ra du lieu
            foreach (FileInfo file in dic.GetFiles())
            {
                // Doc du lieu ra tu file txt
                getSourceData(File.ReadAllLines(file.FullName));
            }

            tb.Text = tembuf;
            string[] tem = tb.Lines;
            tb.Text = humbuf;
            string[] hum = tb.Lines;

            // Chuyen doi du lieu trong file txt sang ma tran
            // de co the xu li trong matlab
            string x = createMatrixMatlab(tem);
            string y = createMatrixMatlab(hum);

            MLApp.MLApp matlab = new MLApp.MLApp();

            // Chay cac lenh command Matlab
            // de ve do thi nhiet do, do am
            matlab.Execute(@"cd E:\data");            
            matlab.Execute("x = " + x);
            matlab.Execute("y = " + y);
            matlab.Execute("subplot(2,1,1)");
            matlab.Execute("plot(x)");
            matlab.Execute("xlabel('Temperature')");
            matlab.Execute("grid on");
            matlab.Execute("subplot(2,1,2)");
            matlab.Execute("plot(y)");
            matlab.Execute("xlabel('Humidity')");
            matlab.Execute("grid on");
        }

        /* Ham tao ra ma tran dung de chay tren command cua Matlab:
           - Tham so truyen vao la mang da xu li (da qua ham loc ra nhiet do hoac do am)
           - Moi phan tu cua mang la 1 phan tu cau ma tran.
           - Ket qua tra ra la chuoi co dang:
             [a1 a2 a3 ... an]
         */
        private string createMatrixMatlab(string[] data)
        {
            // Tao ma tran de dung trong Matlab command
            string matrix = "[";

            for (int i = 0; i < data.Length - 1; i++)
            {
                matrix += data[i] + " ";
            }

            matrix += data[data.Length - 1] + "]";

            return matrix;
        }

        /* Ham loc ra chi so nhiet do va do am tu du leu duoc gui ve
           - Ham loc don gian chi lay cac chu so,
             dau cham cua so thap phan(bo qua dau cham cuoi cau).
           - Quy dinh:
             - Luu Nhiet do vo bien tembuf
             - Luu Do am vao bien humbuf
         */
        private void getSourceData(string[] data)
        {
            bool isTemp = true;
            int i, j;

            for (i = 0; i < data.Length; i++)
            {
                for (j = 0; j < data[i].Length; j++)
                {
                    if (data[i][j] == '/')
                    {
                        // This is time line
                        // go to next line
                        i++;
                        break;
                    }
                }

                for (j = 0; j < data[i].Length; j++)
                {
                    // Neu la so
                    if ((data[i][j] >= 48) && (data[i][j] <= 57))
                    {
                        if (isTemp)
                            tembuf += data[i][j];
                        else
                            humbuf += data[i][j];
                    }
                    else if (data[i][j] == '.')
                    {
                        // Neu la dau cham va ko phai dau cham cau
                        if ((j + 1 != data[i].Length))
                        {
                            if (isTemp)
                                tembuf += data[i][j];
                            else
                                humbuf += data[i][j];
                        }
                        else
                        {
                            // This is hum
                            isTemp = true;
                            tembuf += Environment.NewLine;
                        }
                        
                    }
                    else if (data[i][j] == ',')
                    {
                        // This is temp
                        isTemp = false;
                        humbuf += Environment.NewLine;
                    }
                }                    
            }
        }

        /* Nut Save:
           Khi nhan nut Save:
           - Lay data tu tren textbox hien thi xuong
           - Xu li chuoi time la thoi gian dau tien luc vua ket noi
             (hoac lan luu truoc do) la ten file luu tru
           - Tao moi file voi ten vua xu li
           - Luu data vao file vua tao
           - Xoa textboc data, set lai time la hien tai
         */
        private void btSave_Click(object sender, EventArgs e)
        {
            //Xu li chuoi de tao ten file luu du lieu
            string temp = time.ToString().Replace(":", "_").Replace("/", "_");
            string pathFileNow = Path.Combine(_path, temp + ".txt");

            if (File.Exists(pathFileNow))
            {
                // Tao file
                using (FileStream fs = File.Create(pathFileNow)) { }
            }

            // Luu du lieu vao file vua tao
            tbDataReading.Lines[0] = null;
            string data = tbDataReading.Text;
            File.WriteAllText(pathFileNow, data);

            // Clear man hinh hien thi, chuan bi nhan du lieu moi
            tbDataReading.Clear();
            time = DateTime.Now;
            tbDataReading.Text = time.ToString();
        }

        // Mo folder chua cac file nguon luu tru nhiet do va do am thu duoc
        private void btOpenFolderSource_Click(object sender, EventArgs e)
        {
            Process.Start(@"E:\data");
        }
    }
}
