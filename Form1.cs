using System.Data;
using System.IO.Ports;
using System.Text;

namespace EM02_E_HalfTester
{
    public partial class fmMain : Form
    {
        private SerialPort? _barcodePort;
        private bool Barcode_receiving = false;
        delegate void GoGetData(string buffer);
        private Thread t;
        private string[] chanelNames ;
        private Image[] imgDigiNormal;
        private Image[] imgDigiNormalDot;
        private Image[] imgDigiError;
        private Image[] imgDigiErrorDot;
        public fmMain()
        {
            InitializeComponent();
        }

        private void initComBarcode(string sPort)
        {
            _barcodePort = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            if (_barcodePort.IsOpen == false)
            {
                try
                {
                    _barcodePort.Open();

                    //開啟 Serial Port

                    Barcode_receiving = true;
                    //開啟執行續做接收動作
                    t = new Thread(DoReceive);
                    t.IsBackground = true;
                    t.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟Barcode Reader!");
                    Application.Exit();
                }
            }

        }

        private void DoReceive()
        {
            Byte[] buffer = new Byte[1024];

            try
            {
                while (Barcode_receiving)
                {
                    if (_barcodePort.BytesToRead > 14 && _barcodePort.BytesToWrite == 0)
                    {
                        Int32 length = _barcodePort.Read(buffer, 0, buffer.Length);

                        string buf = Encoding.ASCII.GetString(buffer);
                        Array.Resize(ref buffer, length);
                        GoGetData d = new GoGetData(BarcodeShow);
                        this.Invoke(d, new Object[] { buf });
                        Array.Resize(ref buffer, 1024);
                    }

                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void BarcodeShow(string buffer)
        {
           
           MessageBox.Show(buffer);
        }

        private void fmMain_Load(object sender, EventArgs e)
        {
            chanelNames = new string[24];
            imgDigiNormal = new Image[10];
            imgDigiNormalDot = new Image[10];
            imgDigiError = new Image[10];
            imgDigiErrorDot = new Image[10];

            chanelNames[0] = EM02_E_HalfTester.Resource1.Ch1;
            chanelNames[1] = EM02_E_HalfTester.Resource1.Ch2;
            chanelNames[2] = EM02_E_HalfTester.Resource1.Ch3;
            chanelNames[3] = EM02_E_HalfTester.Resource1.Ch4;
            chanelNames[4] = EM02_E_HalfTester.Resource1.Ch5;
            chanelNames[5] = EM02_E_HalfTester.Resource1.Ch6;
            chanelNames[6] = EM02_E_HalfTester.Resource1.Ch7;
            chanelNames[7] = EM02_E_HalfTester.Resource1.Ch8;
            chanelNames[8] = EM02_E_HalfTester.Resource1.Ch9;
            chanelNames[9] = EM02_E_HalfTester.Resource1.Ch10;    
            chanelNames[10] = EM02_E_HalfTester.Resource1.Ch11;
            chanelNames[11] = EM02_E_HalfTester.Resource1.Ch12;
            chanelNames[12] = EM02_E_HalfTester.Resource1.Ch13;
            chanelNames[13] = EM02_E_HalfTester.Resource1.Ch14;
            chanelNames[14] = EM02_E_HalfTester.Resource1.Ch15;
            chanelNames[15] = EM02_E_HalfTester.Resource1.Ch16;
            chanelNames[16] = EM02_E_HalfTester.Resource1.Ch17;
            chanelNames[17] = EM02_E_HalfTester.Resource1.Ch18;
            chanelNames[18] = EM02_E_HalfTester.Resource1.Ch19;
            chanelNames[19] = EM02_E_HalfTester.Resource1.Ch20;
            chanelNames[20] = EM02_E_HalfTester.Resource1.Ch21;
            chanelNames[21] = EM02_E_HalfTester.Resource1.Ch22;
            chanelNames[22] = EM02_E_HalfTester.Resource1.Ch23;
            chanelNames[23] = EM02_E_HalfTester.Resource1.Ch24;

            int idx = 0;
            foreach (Control ctrl in this.Controls)
            {
                   
                if (ctrl is GroupBox)
                {
                        ctrl.Text = chanelNames[idx++];
                }
            }

            imgDigiNormal[0] = EM02_E_HalfTester.Resource1._0;
            imgDigiNormal[1] = EM02_E_HalfTester.Resource1._1;
            imgDigiNormal[2] = EM02_E_HalfTester.Resource1._2;
            imgDigiNormal[3] = EM02_E_HalfTester.Resource1._3;
            imgDigiNormal[4] = EM02_E_HalfTester.Resource1._4;
            imgDigiNormal[5] = EM02_E_HalfTester.Resource1._5;
            imgDigiNormal[6] = EM02_E_HalfTester.Resource1._6;
            imgDigiNormal[7] = EM02_E_HalfTester.Resource1._7;
            imgDigiNormal[8] = EM02_E_HalfTester.Resource1._8;
            imgDigiNormal[9] = EM02_E_HalfTester.Resource1._9;

            imgDigiNormalDot[0] = EM02_E_HalfTester.Resource1._0d;
            imgDigiNormalDot[1] = EM02_E_HalfTester.Resource1._1d;
            imgDigiNormalDot[2] = EM02_E_HalfTester.Resource1._2d;
            imgDigiNormalDot[3] = EM02_E_HalfTester.Resource1._3d;
            imgDigiNormalDot[4] = EM02_E_HalfTester.Resource1._4d;
            imgDigiNormalDot[5] = EM02_E_HalfTester.Resource1._5d;
            imgDigiNormalDot[6] = EM02_E_HalfTester.Resource1._6d;
            imgDigiNormalDot[7] = EM02_E_HalfTester.Resource1._7d;
            imgDigiNormalDot[8] = EM02_E_HalfTester.Resource1._8d;
            imgDigiNormalDot[9] = EM02_E_HalfTester.Resource1._9d;

            imgDigiError[0] = EM02_E_HalfTester.Resource1._0r;
            imgDigiError[1] = EM02_E_HalfTester.Resource1._1r;
            imgDigiError[2] = EM02_E_HalfTester.Resource1._2r;
            imgDigiError[3] = EM02_E_HalfTester.Resource1._3r;
            imgDigiError[4] = EM02_E_HalfTester.Resource1._4r;
            imgDigiError[5] = EM02_E_HalfTester.Resource1._5r;
            imgDigiError[6] = EM02_E_HalfTester.Resource1._6r;
            imgDigiError[7] = EM02_E_HalfTester.Resource1._7r;
            imgDigiError[8] = EM02_E_HalfTester.Resource1._8r;
            imgDigiError[9] = EM02_E_HalfTester.Resource1._9r;

            
            imgDigiErrorDot[0] = EM02_E_HalfTester.Resource1._0rd;
            imgDigiErrorDot[1] = EM02_E_HalfTester.Resource1._1rd;
            imgDigiErrorDot[2] = EM02_E_HalfTester.Resource1._2rd;
            imgDigiErrorDot[3] = EM02_E_HalfTester.Resource1._3rd;
            imgDigiErrorDot[4] = EM02_E_HalfTester.Resource1._4rd;
            imgDigiErrorDot[5] = EM02_E_HalfTester.Resource1._5rd;
            imgDigiErrorDot[6] = EM02_E_HalfTester.Resource1._6rd;
            imgDigiErrorDot[7] = EM02_E_HalfTester.Resource1._7rd;
            imgDigiErrorDot[8] = EM02_E_HalfTester.Resource1._8rd;
            imgDigiErrorDot[9] = EM02_E_HalfTester.Resource1._9rd;

            string[] ports = SerialPort.GetPortNames();
            cbComPorts.Items.AddRange(ports);

            initComBarcode("COM3");

        }

        private void displayPictureBox ( PictureBox pb , int iData , bool bErr)
        {
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            pb.Image = bErr ? imgDigiError[iData] : imgDigiNormal[iData];
            pb.SizeMode = PictureBoxSizeMode.StretchImage;

        }
        private void displayPictureBoxDot(PictureBox pb, int iData, bool bErr)
        {
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            pb.Image = bErr ? imgDigiErrorDot[iData] : imgDigiNormalDot[iData];
        }

        private void displayGroup( GroupBox gb , int iData , bool bErr)
        {
            // find 3 picture box in GroupBox and check their left right place

            PictureBox pbL = new PictureBox();
            PictureBox pbR = new PictureBox();
            PictureBox pbM = new PictureBox();
            int gbWidth = gb.Width;
    
            foreach (Control ctrl in gb.Controls)
            {
                if ( ctrl is PictureBox)
                {
                    Point l = ctrl.Location;
                    if(l.X > gbWidth/2)
                    {
                        pbR = (PictureBox)ctrl;
                    } else if(l.X > gbWidth/4)
                    {
                        pbM = (PictureBox)ctrl;
                    } else
                    {
                        pbL = (PictureBox)ctrl;
                    }
                }
            }

            int iLeft = iData / 100;
            int iMid = (iData - (iLeft * 100)) / 10;
            int iRight = iData % 10;

            displayPictureBox(pbL, iLeft , bErr);
            displayPictureBoxDot(pbM , iMid , bErr);
            displayPictureBox(pbR , iRight , bErr);

        }

        private void btnTest_Click(object sender, EventArgs e)
        {
       

            foreach (Control ctrl in this.Controls)
            {

                if (ctrl is GroupBox)
                {
                    displayGroup((GroupBox)ctrl, 123 , true);
                }
            }
        }

        private void btnTest2_Click(object sender, EventArgs e)
        {
            foreach (Control ctrl in this.Controls)
            {

                if (ctrl is GroupBox)
                {
                    displayGroup((GroupBox)ctrl, 321, false);
                }
            }
        }
    }
}