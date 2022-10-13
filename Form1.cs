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

        private SerialPort? _MESPort;
        private bool MES_receiving = false;
        delegate void GoGetMESData(byte[] buffer);
        private Thread threadMES;

        private const UInt16 lenBufUT5526 = 256;
        private byte[] ringBufferUT5526;
        private int ringCountUT5526 = 0;
        private int ringOutputUT5526 = 0;
        private int ringInputUT5526 = 0;
        private int iIdxGetUT5526 = 0;  // current read channel 
        private int iCntGetUT5526 = 0;  // read how many channels
 
        private int iCurrentGetUT5526 = 0;
        private int iCntWaitUT5526 = 0;
        private bool bErrorUT5526 = false;
        private int iStateUT5526 = 0;


        private const UInt16 lenBufBarCode = 32;
        private byte[] ringBufferBarCode;
        private int ringCountBarCode = 0;
        private int ringOutputBarCode = 0;
        private int ringInputBarCode = 0;
        private int iIdxReadBarCode = 0;  // current read channel 
        private int iCntReadBarCode = 0;  // read how many channels
        private int iCurrentReadBarCode = 0;
        private int iCntWaitBarCode = 0;
        private int iStateBarCode = 0;


        private const UInt16 lenBufMES = 256;
        private byte[] ringBufferMES;
        private int ringCountMES = 0;
        private int ringOutputMES = 0;
        private int ringInputMES = 0;
        private int iIdxReadMES = 0;  // current read channel 
        private int iCntReadMES = 0;  // read how many channels
        private int iCurrentReadMES = 0;
        private int iCntWaitMES = 0;


        private string[] chanelNames;
        private Image[] imgDigiNormal;
        private Image[] imgDigiNormalDot;
        private Image[] imgDigiError;
        private Image[] imgDigiErrorDot;
        private byte[] leadChar = new byte[1];
        private byte[] endChar = new byte[1];

        private bool bCmdReadSend = false;


        DateTime time00;
        DateTime time01;

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

        private void initComMES(string sPort)
        {
            _MESPort = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            if (_MESPort.IsOpen == false)
            {
                try
                {
                    _MESPort.Open();
                    //開啟 Serial Port
                    MES_receiving = true;
                    //開啟執行續做接收動作
                    threadMES = new Thread(DoReceiveMES);
                    threadMES.IsBackground = true;
                    threadMES.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟MES Port!");
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


        private void DoReceiveMES()
        {
            Byte[] buffer = new Byte[256];

            try
            {
                while (MES_receiving)
                {
                    if (_MESPort?.BytesToRead >= 1 && _MESPort.BytesToWrite == 0)
                    {
                        Int32 length = _MESPort.Read(buffer, 0, buffer.Length);

                        string buf = Encoding.ASCII.GetString(buffer);
                        Array.Resize(ref buffer, length);
                        GoGetMESData d = new GoGetMESData(MESShow);
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

        private void SetRingBarCode(byte byData)
        {
            ringBufferBarCode[ringInputBarCode] = byData;
            ringCountBarCode++;
            ringInputBarCode = (ringInputBarCode + 1) & (lenBufBarCode - 1);
        }
        private byte GetRingBarCode()
        {
            byte byData = ringBufferBarCode[ringOutputBarCode];
            ringCountBarCode--;
            if (ringCountBarCode < 0) ringCountBarCode = 0;
            ringOutputBarCode = (ringOutputBarCode + 1) & (lenBufBarCode - 1);
            return byData;
        }

        private void SetRingUT5526(byte byData)
        {
            ringBufferUT5526[ringInputUT5526] = byData;
            ringCountUT5526++;
            ringInputUT5526 = (ringInputUT5526 + 1) & (lenBufUT5526 - 1);
        }
        private byte GetRingUT5526()
        {
            byte byData = ringBufferUT5526[ringOutputUT5526];
            ringCountUT5526--;
            if (ringCountUT5526 < 0) ringCountUT5526 = 0;
            ringOutputUT5526 = (ringOutputUT5526 + 1) & (lenBufUT5526 - 1);
            return byData;
        }
        private byte CheckTopByteUT5526()
        {
            return ringBufferUT5526[ringOutputUT5526];
        }
        private void SetUT5526VoltRange()
        {
            string strComData = "01MORG03";  // set range = 200V
            byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
            byte[] byBCC = new byte[1];
            byBCC[0] = UTBus_LRC(cmdStr, 8);
            _UT5526Port?.Write(leadChar, 0, 1);
            _UT5526Port?.Write(strComData);
            _UT5526Port?.Write(byBCC, 0, 1);
            _UT5526Port?.Write(endChar, 0, 1);
        }
        private void setRingMES(byte byData)
        {
            ringBufferMES[ringInputMES] = byData;
            ringCountMES++;
            ringInputMES = (ringInputMES + 1) & (lenBufMES - 1);
        }
        private byte getRingMES()
        {
            byte byData = ringBufferMES[ringOutputMES];
            ringCountMES--;
            if (ringCountMES < 0) ringCountMES = 0;
            ringOutputMES = (ringOutputUT5526 + 1) & (lenBufMES - 1);
            return byData;
        }
        public void BarcodeShow(byte[] buffer)
        {

            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                SetRingBarCode(buf[i]);
            }
        }

        public void UT5526Show(byte[] buffer)
        {
            //  byte[] buf = Encoding.ASCII.GetBytes(buffer);
            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                SetRingUT5526(buf[i]);
            }
        }

        public void MESShow(byte[] buffer)
        {
            //  byte[] buf = Encoding.ASCII.GetBytes(buffer);
            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                setRingMES(buf[i]);
            }

        }

        private void InitializeTimer()
        {
            //   timer1 = new System.Timers.Timer();
            timer1.Interval = 10;
            this.timer1.Tick += new EventHandler(Timer1_Tick);
        
            timer1.Enabled = true;

        }
        private void procUT5526()
        {
            const byte SOH = 0x01;
            const byte EOT = 0x04;

            if (bErrorUT5526)
            {
                iCntWaitUT5526 = 0;
                iStateUT5526 = 0;
                iCurrentGetUT5526 = 0;
                return;
            }




            if (iCntWaitUT5526 > 0) iCntWaitUT5526--;
            //    pictureBoxButton1.Image = Resource1.led_a;
            switch (iStateUT5526)
            {
                case 0:
                    if (iCntGetUT5526 > 0)
                    {
                        time00 = DateTime.Now;
                        //  iCntRead--;
                        if (collectData.Count > iIdxGetUT5526)
                        {
                            string strComData = collectData[iIdxGetUT5526].CmdSelected;   // send channel select comand
                            byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                            byte[] byBCC = new byte[1];
                            byBCC[0] = UTBus_LRC(cmdStr, 8);
                            _UT5526Port?.Write(leadChar, 0, 1);
                            _UT5526Port?.Write(strComData);
                            _UT5526Port?.Write(byBCC, 0, 1);
                            _UT5526Port?.Write(endChar, 0, 1);
                            iCntWaitUT5526 = 60;
                            iStateUT5526++;
                            iCurrentGetUT5526 = 0;
                        }

                    }
                    break;
                case 1:                                             // wait 1ER0
                    if (ringCountUT5526 >= 4)
                    {
                        if (GetRingUT5526() == '1' && GetRingUT5526() == 'E' && GetRingUT5526() == 'R' && GetRingUT5526() == '0')
                        {
                            iCntWaitUT5526 = 40;
                            iStateUT5526++;
                        }
                        else
                        {
                            iStateUT5526 = 0;

                        }


                    }
                    break;
                case 3:
                    if (iCntWaitUT5526 == 0)
                    {
                        string strComData = "01MORDVO";  // read current channel data
                        byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                        byte[] byBCC = new byte[1];
                        byBCC[0] = UTBus_LRC(cmdStr, 8);
                        _UT5526Port?.Write(leadChar, 0, 1);
                        _UT5526Port?.Write(strComData);
                        _UT5526Port?.Write(byBCC, 0, 1);
                        _UT5526Port?.Write(endChar, 0, 1);
                        //  iCntWaitUT5526 = 40;
                        iStateUT5526++;
                    }
                    break;
                case 2:
                    if (iCntGetUT5526 > 0 && iCntGetUT5526 != iCurrentGetUT5526 && iCntWaitUT5526 == 0)
                    {
                        time01 = DateTime.Now;
                        lblTime.Text = (time01 - time00).TotalMilliseconds.ToString();
                        iCurrentGetUT5526 = iCntGetUT5526;
                        string strComData = "01MORDVO";  // read current channel data
                        byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                        byte[] byBCC = new byte[1];
                        byBCC[0] = UTBus_LRC(cmdStr, 8);
                        _UT5526Port?.Write(leadChar, 0, 1);
                        _UT5526Port?.Write(strComData);
                        _UT5526Port?.Write(byBCC, 0, 1);
                        _UT5526Port?.Write(endChar, 0, 1);
                        //  iCntWaitUT5526 = 40;
                        iStateUT5526++;
                    }
                    break;
                case 4:
                    if (ringCountUT5526 >= 11)
                    {
                        do
                        {
                            byte byTemp = GetRingUT5526(); ;
                            if (byTemp == SOH)
                            {
                                iCntGetUT5526--;
                                byTemp = GetRingUT5526();  // range code
                                byTemp = GetRingUT5526(); // address
                                byTemp = GetRingUT5526(); // V code 
                                int iInt = GetRingUT5526() * 100 + GetRingUT5526() * 10 + GetRingUT5526(); // 3 digis integer
                                int iDot = GetRingUT5526() * 10 + GetRingUT5526(); // 2 digi
                                byTemp = GetRingUT5526(); // bcc code
                                byTemp = GetRingUT5526(); // end code
                                iStateUT5526++;
                                if (collectData.Count <= iIdxGetUT5526)
                                {
                                    int iTest = iCntGetUT5526;
                                }
                                collectData[iIdxGetUT5526].PreviousData = collectData[iIdxGetUT5526].CurrentData;
                                collectData[iIdxGetUT5526].CurrentData = iInt * 100 + iDot;
                                if (iIdxGetUT5526 > 0) // skip dummy read 
                                {
                                    foreach (Control ctrl in this.panelDisplay.Controls)
                                    {
                                        if (ctrl is GroupBox)
                                        {
                                            if (ctrl.Text == collectData[iIdxGetUT5526].ChannelName)
                                            {
                                                int iCurrentData = collectData[iIdxGetUT5526].CurrentData;
                                                iCurrentData = (iCurrentData > 999) ? iCurrentData / 10 : iCurrentData;
                                                int iOffset = iCurrentData - (collectData[iIdxGetUT5526].StandardData);
                                                if (iOffset < 0) iOffset = 0 - (iOffset);
                                                bool bErr = (iOffset > (collectData[iIdxGetUT5526].StandardData / 10)) ? true : false;

                                                displayGroup((GroupBox)ctrl, collectData[iIdxGetUT5526].CurrentData, bErr);
                                            }
                                        }
                                    }

                                }


                                iIdxGetUT5526++;  // move to next
                                if (iCntGetUT5526 == 0)
                                {
                                   // string strWriteMAC = "$TH02,MAC," + lblMAC.Text.Remove(lblMAC.Text.Length - 1);
                                  //  _TH02Port?.Write(strWriteMAC);
                                  //  _TH02Port?.DiscardInBuffer();
                                   // ClearRingTH02();
                                   // btnWriteMAC.Visible = true;
                                    //    pictureBoxResult.Enabled = true;
                                    //    pictureBoxResult.Image = Resource1.pass;
                                   // bWritingMAC = true;
                                    iIdxGetUT5526 = 0;
                                }

                            }
                        } while (ringCountUT5526 >= 10);
                    }

                    break;
                case 5:
                    iStateUT5526++;

                    break;
                default:
                    iStateUT5526 = 0;
                    break;
            }

        }
        private void Timer1_Tick(object Sender, EventArgs e)
        {
            procUT5526();
         
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
          //  initComBarcode("COM3");
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
            if (_MESPort?.IsOpen == true)
            {
                _MESPort.Close();
            }
            Environment.Exit(Environment.ExitCode);

        }

        private void btnRead_Click(object sender, EventArgs e)
        {
         
            iIdxGetUT5526 = 0;
       
            timer1.Enabled = true;
            bCmdReadSend = true;
            iCntGetUT5526 = 24;
          
        }

        private void label25_Click(object sender, EventArgs e)
        {

        }
    }
}