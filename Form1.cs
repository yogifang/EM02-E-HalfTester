using System;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;




namespace EM02_E_HalfTester
{

   
    public partial class fmMain : Form
    {
        private SerialPort? _barcodePort;
        private bool Barcode_receiving = false;
        delegate void GoGetBarcodeData(byte[] buffer);
        private Thread threadBarcode;
        private SerialPort? _UT5526Port;
        private bool UT5526_receiving = false;
        delegate void GoGetUT5526Data(byte[] buffer);
        private Thread threadUT5526;

        private const uint lenBufUT5526 = 32;
        private byte[] ringBufferUT5526;
        private int ringCountUT5526 = 0;
        private int ringOutputUT5526 = 0;
        private int ringInputUT5526 = 0;
        private int iIdxReadUT5526 = 0;  // current read channel 
        private int iCntReadUT5526 = 0;  // read how many channels
        private int iCurrentReadUT5526 = 0;
        private int iCntWaitUT5526 = 0;

        private int iState = 0;


        private const uint lenBufBarCode = 32;
        private byte[] ringBufferBarCode;
        private int ringCountBarCode = 0;
        private int ringOutputBarCode = 0;
        private int ringInputBarCode = 0;
        private int iIdxReadBarCode = 0;  // current read channel 
        private int iCntReadBarCode = 0;  // read how many channels
        private int iCurrentReadBarCode = 0;
        private int iCntWaitBarCode = 0;
        private int iStateBarCode = 0;



        private string[] chanelNames;
        private Image[] imgDigiNormal;
        private Image[] imgDigiNormalDot;
        private Image[] imgDigiError;
        private Image[] imgDigiErrorDot;
        private byte[] leadChar = new byte[1];
        private byte[] endChar = new byte[1];

        private bool bCmdReadSend = false;

        class ChannelData
        {
            public string ChannelName { get; set; }
            public string CmdSelected { get; set; }
            public int PreviousData { get; set; }
            public int CurrentData { get; set; }
            public int StandardData { get; set; }
        }


        private List<ChannelData> collectData = new List<ChannelData>();


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
                    threadBarcode = new Thread(DoReceiveBarcode);
                    threadBarcode.IsBackground = true;
                    threadBarcode.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟Barcode Reader!");
                    Application.Exit();
                }
            }
        }
        private void initComUT5526(string sPort)
        {
            _UT5526Port = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            if (_UT5526Port.IsOpen == false)
            {
                try
                {
                    _UT5526Port.Open();
                    //開啟 Serial Port
                    UT5526_receiving = true;
                    //開啟執行續做接收動作
                    threadUT5526 = new Thread(DoReceiveUT5526);
                    threadUT5526.IsBackground = true;
                    threadUT5526.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟UT5526 Reader!");
                    Application.Exit();
                }
            }
        }
        private void DoReceiveBarcode()
        {
            Byte[] buffer = new Byte[256];

            try
            {
                while (Barcode_receiving)
                {
                    if (_barcodePort?.BytesToRead > 14 && _barcodePort.BytesToWrite == 0)
                    {
                        Int32 length = _barcodePort.Read(buffer, 0, buffer.Length);

                        string buf = Encoding.ASCII.GetString(buffer);
                        Array.Resize(ref buffer, length);
                        GoGetBarcodeData d = new GoGetBarcodeData(BarcodeShow);
                        this.Invoke(d, new Object[] { buffer });
                      
                        Array.Resize(ref buffer, 1024);
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DoReceiveUT5526()
        {
            Byte[] buffer = new Byte[256];
          
            try
            {
                while (UT5526_receiving)
                {
                    if (_UT5526Port?.BytesToRead >= 1 && _UT5526Port.BytesToWrite == 0)
                    {
                        Int32 length = _UT5526Port.Read(buffer, 0, buffer.Length);

                        string buf = Encoding.ASCII.GetString(buffer);
                        Array.Resize(ref buffer, length);
                        GoGetUT5526Data d = new GoGetUT5526Data(UT5526Show);
                        this.Invoke(d, new Object[] { buffer });
                        Array.Resize(ref buffer, length);
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void setRingBarCode(byte byData)
        {
            ringBufferBarCode[ringInputBarCode] = byData;
            ringCountBarCode++;
            ringInputBarCode = (ringInputBarCode + 1) & (int)(lenBufBarCode - 1);
        }
        private byte getRingBarCode()
        {
            byte byData = ringBufferBarCode[ringOutputBarCode];
            ringCountBarCode--;
            if (ringCountBarCode < 0) ringCountBarCode = 0;
            ringOutputBarCode = (ringOutputBarCode + 1) & (int)(lenBufBarCode - 1);
            return byData;
        }

        private void setRingUT5526(byte byData)
        {
            ringBufferUT5526[ringInputUT5526] = byData;
            ringCountUT5526++;
            ringInputUT5526 = (ringInputUT5526 + 1) & 0x001f;
        }
        private byte getRingUT5526()
        {
            byte byData = ringBufferUT5526[ringOutputUT5526];
            ringCountUT5526--;
            if (ringCountUT5526 < 0) ringCountUT5526 = 0;
            ringOutputUT5526 = (ringOutputUT5526 + 1) & 0x001f;
            return byData;
        }
        public void BarcodeShow(byte[] buffer)
        {

            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                setRingBarCode(buf[i]);
            }
        }

        public void UT5526Show(byte[] buffer)
        {
            //  byte[] buf = Encoding.ASCII.GetBytes(buffer);
            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                setRingUT5526(buf[i]);
            }

        }



        private void InitializeTimer()
        {
            //   timer1 = new System.Timers.Timer();
            timer1.Interval = 100;
            this.timer1.Tick += new EventHandler(Timer1_Tick);
         //  this.timer1.Tick += new System.EventHandler(Timer1_Tick);
            // Enable timer.  
            timer1.Enabled = false;

        }
        private void Timer1_Tick(object Sender, EventArgs e)
        {
            const byte SOH = 0x01;
            const byte EOT = 0x04;
            if (iCntWaitUT5526 > 0) iCntWaitUT5526--;

            switch (iState)
            {
                case 0:
                    if (iCntReadUT5526 > 0 && iCntWaitUT5526 == 0)
                    {
                      //  iCntRead--;
                        string strComData = collectData[iIdxReadUT5526].CmdSelected;   // send channel select comand
                        byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                        byte[] byBCC = new byte[1];
                        byBCC[0] = UTBus_LRC(cmdStr, 8);
                        _UT5526Port?.Write(leadChar, 0, 1);
                        _UT5526Port?.Write(strComData);
                        _UT5526Port?.Write(byBCC, 0, 1);
                        _UT5526Port?.Write(endChar, 0, 1);
                        iCntWaitUT5526 = 7;
                        iState = 1;
                    }
                    break;
                case 1:                                             // wait 1ER0
                    if (ringCountUT5526 >= 4)
                    {
                        byte byTemp = getRingUT5526(); ;
                        byTemp = getRingUT5526();  // range code
                        byTemp = getRingUT5526(); // address
                        byTemp = getRingUT5526(); // V code 
                        iState = 2;
                     
                    }
                    break;
                case 2:
                    if (iCntReadUT5526 > 0 && iCntReadUT5526 != iCurrentReadUT5526 && iCntWaitUT5526 == 0)
                    {
                        iCurrentReadUT5526 = iCntReadUT5526;
                        string strComData = "01MORDVO";  // read current channel data
                        byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                        byte[] byBCC = new byte[1];
                        byBCC[0] = UTBus_LRC(cmdStr, 8);
                        _UT5526Port?.Write(leadChar, 0, 1);
                        _UT5526Port?.Write(strComData);
                        _UT5526Port?.Write(byBCC, 0, 1);
                        _UT5526Port?.Write(endChar, 0, 1);
                        timer1.Enabled = true;
                        bCmdReadSend = false;
                        iState = 3;
                    }
                    break;
                case 3:
                    if (ringCountUT5526 >= 11)
                    {
                        do
                        {
                            byte byTemp = getRingUT5526(); ;
                            if (byTemp == SOH)
                            {
                                iCntReadUT5526--;
                                byTemp = getRingUT5526();  // range code
                                byTemp = getRingUT5526(); // address
                                byTemp = getRingUT5526(); // V code 
                                int iInt = getRingUT5526() * 10 + getRingUT5526();
                                int iDot = getRingUT5526() * 100 + getRingUT5526() * 10 + getRingUT5526();
                                byTemp = getRingUT5526(); // bcc code
                                byTemp = getRingUT5526(); // end code
                                iState = 0;
                                collectData[iIdxReadUT5526].PreviousData = collectData[iIdxReadUT5526].CurrentData;
                                collectData[iIdxReadUT5526].CurrentData = iInt * 100 + iDot/10;
                              

                                foreach (Control ctrl in this.Controls)
                                {
                                    if (ctrl is GroupBox)
                                    {
                                        if (ctrl.Text == collectData[iIdxReadUT5526].ChannelName)
                                        {
                                            int iOffset = collectData[iIdxReadUT5526].CurrentData - collectData[iIdxReadUT5526].StandardData;
                                            if (iOffset < 0) iOffset = 0 - (iOffset); 
                                            bool bErr = (iOffset > (collectData[iIdxReadUT5526].StandardData/10))? true : false;

                                            displayGroup((GroupBox)ctrl, collectData[iIdxReadUT5526].CurrentData, bErr);
                                        }
                                    }
                                       
                                       
                                }
                                iIdxReadUT5526++;  // move to next

                            }
                        } while (ringCountUT5526 >= 10);
                    }

                    break;
                default:
                    iState = 0;
                    break;
            }

         
        }

        private void fmMain_Load(object sender, EventArgs e)
        {
     
            imgDigiNormal = new Image[10];
            imgDigiNormalDot = new Image[10];
            imgDigiError = new Image[10];
            imgDigiErrorDot = new Image[10];
            ringBufferUT5526 = new byte[32];
         
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is GroupBox)
                {
                  if(ctrl.Tag == null)
                    {
                        MessageBox.Show("缺通道設定!");
                        Application.Exit();
                    }
                  string strChannel = ctrl.Tag?.ToString();
                  int channelInt = int.Parse(strChannel);
                 if(channelInt > 32 || channelInt < 1)
                    {
                        MessageBox.Show("通道設定錯誤!");
                        Application.Exit();
                    }
                 GroupBox grp = (GroupBox)ctrl;
                    int iLeft = 0;
                    int iMid = 0;
                    int iRight = 0;
                    int gbWidth = grp.Width;
                    foreach (Control cctrl in grp.Controls)
                    {
                        if (cctrl is PictureBox)
                        {
                            Point l = cctrl.Location;
                            if (l.X > gbWidth / 2)
                            {
                                iRight = int.Parse((string)cctrl.Tag);
                            }
                            else if (l.X > gbWidth / 4)
                            {
                                iMid = int.Parse((string)cctrl.Tag);
                            }
                            else
                            {
                                iLeft = int.Parse((string)cctrl.Tag);
                            }
                        }
                    }
                    int iStandard = iLeft * 100 + iMid * 10 + iRight;
                    collectData.Add(new ChannelData() { ChannelName = ctrl.Text , CmdSelected = "01MOCH" + channelInt.ToString().PadLeft(2, '0'), PreviousData = 0, CurrentData = 0 , StandardData=iStandard});
                }
            }

            imgDigiNormal[0] = Resource1._0;
            imgDigiNormal[1] = Resource1._1;
            imgDigiNormal[2] = Resource1._2;
            imgDigiNormal[3] = Resource1._3;
            imgDigiNormal[4] = Resource1._4;
            imgDigiNormal[5] = Resource1._5;
            imgDigiNormal[6] = Resource1._6;
            imgDigiNormal[7] = Resource1._7;
            imgDigiNormal[8] = Resource1._8;
            imgDigiNormal[9] = Resource1._9;

            imgDigiNormalDot[0] = Resource1._0d;
            imgDigiNormalDot[1] = Resource1._1d;
            imgDigiNormalDot[2] = Resource1._2d;
            imgDigiNormalDot[3] = Resource1._3d;
            imgDigiNormalDot[4] = Resource1._4d;
            imgDigiNormalDot[5] = Resource1._5d;
            imgDigiNormalDot[6] = Resource1._6d;
            imgDigiNormalDot[7] = Resource1._7d;
            imgDigiNormalDot[8] = Resource1._8d;
            imgDigiNormalDot[9] = Resource1._9d;

            imgDigiError[0] = Resource1._0r;
            imgDigiError[1] = Resource1._1r;
            imgDigiError[2] = Resource1._2r;
            imgDigiError[3] = Resource1._3r;
            imgDigiError[4] = Resource1._4r;
            imgDigiError[5] = Resource1._5r;
            imgDigiError[6] = Resource1._6r;
            imgDigiError[7] = Resource1._7r;
            imgDigiError[8] = Resource1._8r;
            imgDigiError[9] = Resource1._9r;

            imgDigiErrorDot[0] = Resource1._0rd;
            imgDigiErrorDot[1] = Resource1._1rd;
            imgDigiErrorDot[2] = Resource1._2rd;
            imgDigiErrorDot[3] = Resource1._3rd;
            imgDigiErrorDot[4] = Resource1._4rd;
            imgDigiErrorDot[5] = Resource1._5rd;
            imgDigiErrorDot[6] = Resource1._6rd;
            imgDigiErrorDot[7] = Resource1._7rd;
            imgDigiErrorDot[8] = Resource1._8rd;
            imgDigiErrorDot[9] = Resource1._9rd;

            string[] ports = SerialPort.GetPortNames();
            cbComPorts.Items.AddRange(ports);

            leadChar[0] = 0x01;
            endChar[0] = 0x04;

            InitializeTimer();
            initComBarcode("COM3");
            initComUT5526("COM6");



        }

        private void displayPictureBox(PictureBox pb, int iData, bool bErr)
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

        private void displayGroup(GroupBox gb, int iData, bool bErr)
        {
            // find 3 picture box in GroupBox and check their left right place

            PictureBox pbL = new PictureBox();
            PictureBox pbR = new PictureBox();
            PictureBox pbM = new PictureBox();
            int gbWidth = gb.Width;

            foreach (Control ctrl in gb.Controls)
            {
                if (ctrl is PictureBox)
                {
                    Point l = ctrl.Location;
                    if (l.X > gbWidth / 2)
                    {
                        pbR = (PictureBox)ctrl;
                    }
                    else if (l.X > gbWidth / 4)
                    {
                        pbM = (PictureBox)ctrl;
                    }
                    else
                    {
                        pbL = (PictureBox)ctrl;
                    }
                }
            }

            int iLeft = iData / 100;
            int iMid = (iData - (iLeft * 100)) / 10;
            int iRight = iData % 10;

            displayPictureBoxDot(pbL, iLeft, bErr);
            displayPictureBox(pbM, iMid, bErr);
            displayPictureBox(pbR, iRight, bErr);

        }

        private void btnTest_Click(object sender, EventArgs e)
        {

            byte byTest = ringBufferUT5526[0];

            foreach (Control ctrl in this.Controls)
            {

                if (ctrl is GroupBox)
                {
                    displayGroup((GroupBox)ctrl, 123, true);
                }
            }
        }

        private byte UTBus_LRC(byte[] str, int len)

        {
            byte uchLRC = 0x00;
            int index;

            for (index = 0; index < len; index++)
            {
                uchLRC += str[index];
            }

            if ((uchLRC & 0x7F) <= 0x20)
            {
                uchLRC += 0x20;
            }

            uchLRC &= 0x7F;
            return uchLRC;
        }

        private void btnTest2_Click(object sender, EventArgs e)
        {
            // string strComData = "01MORDVO";
            string strComData = "01MORG02";  // set range = 20V
            byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
            byte[] byBCC = new byte[1];
            byBCC[0] = UTBus_LRC(cmdStr, 8);

            _UT5526Port?.Write(leadChar, 0, 1);
            _UT5526Port?.Write(strComData);
            _UT5526Port?.Write(byBCC, 0, 1);
            _UT5526Port?.Write(endChar, 0, 1);
        }

        private void fmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
            if (_barcodePort?.IsOpen == true)
            {
                _barcodePort.Close();
            }
            if (_UT5526Port?.IsOpen == true)
            {
                _UT5526Port.Close();
            }

        }

        private void btnRead_Click(object sender, EventArgs e)
        {
         
            iIdxReadUT5526 = 0;
       
            timer1.Enabled = true;
            bCmdReadSend = true;
            iCntReadUT5526 = 24;
          
        }
    }
}