using System;
using System.Configuration;
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
       

        private SerialPort? _UT5526Port;
        private bool UT5526_receiving = false;
        delegate void GoGetUT5526Data(byte[] buffer);
        private Thread threadUT5526;

        private SerialPort? _MESPort;
        private bool MES_receiving = false;
        delegate void GoGetMESData(byte[] buffer);
        private Thread threadMES;

        private SerialPort? _EM02Port;
        private bool EM02_receiving = false;
        delegate void GoGetEM02Data(byte[] buffer);
        private Thread threadEM02;

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


        private const UInt16 lenBufEM02 = 32;
        private byte[] ringBufferEM02;
        private int ringCountEM02 = 0;
        private int ringOutputEM02 = 0;
        private int ringInputEM02 = 0;
        private int iIdxReadBarEM02 = 0;  // current read channel 
        private int iCntReadEM02 = 0;  // read how many channels
        private int iCurrentReadEM02 = 0;
        private int iCntWaitEM02 = 0;
        private int iStateEM02 = 0;


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


      
        string comUT5526 = "";
        string comEM02 = "";
        string comMES = "";
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

        private void initComEM02(string sPort)
        {
            _EM02Port = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            if (_EM02Port.IsOpen == false)
            {
                try
                {
                    _EM02Port.Open();
                    //開啟 Serial Port
                    MES_receiving = true;
                    //開啟執行續做接收動作
                    threadMES = new Thread(DoReceiveEM02);
                    threadMES.IsBackground = true;
                    threadMES.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟EM02 Port!");
                    Application.Exit();
                }
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

        private void DoReceiveEM02()
        {
            Byte[] buffer = new Byte[256];

            try
            {
                while (EM02_receiving)
                {
                    if (_EM02Port?.BytesToRead >= 1 && _EM02Port.BytesToWrite == 0)
                    {
                        Int32 length = _EM02Port.Read(buffer, 0, buffer.Length);

                        string buf = Encoding.ASCII.GetString(buffer);
                        Array.Resize(ref buffer, length);
                        GoGetEM02Data d = new GoGetEM02Data(EM02Show);
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

        private void SetRingEM02(byte byData)
        {
            ringBufferEM02[ringInputEM02] = byData;
            ringCountEM02++;
            ringInputEM02 = (ringInputEM02 + 1) & (lenBufEM02 - 1);
        }
        private byte GetRingEM02()
        {
            byte byData = ringBufferEM02[ringOutputEM02];
            ringCountEM02--;
            if (ringCountEM02 < 0) ringCountEM02 = 0;
            ringOutputEM02 = (ringOutputEM02 + 1) & (lenBufEM02 - 1);
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
        public void EM02Show(byte[] buffer)
        {

            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                SetRingEM02(buf[i]);
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
        private void ResetLedDisplay()
        {

            foreach (Control ctrl in this.panelDisplay.Controls)
            {

                if (ctrl is GroupBox)
                {
                    DisplayGroup((GroupBox)ctrl, 0, false);
                }
            }
        }
        private void InitializeTimer()
        {
           
            timer1.Interval = 10;
            EventHandler timer1_Tick = Timer1_Tick;
            this.timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Enabled = true;

        }
        private void ProcUT5526()
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
                            iCntWaitUT5526 = 1;
                            iStateUT5526++;
                            iCurrentGetUT5526 = 0;
                        }

                    }
                    break;
                case 1:                                             // wait 1ER0
                    if (ringCountUT5526 >= 4 )
                    {
                      do {
                            if (GetRingUT5526() == '1')
                            {
                                if (GetRingUT5526() == 'E')
                                {
                                    if (GetRingUT5526() == 'R')
                                    {
                                        if (GetRingUT5526() == '0')
                                        {
                                            iCntWaitUT5526 = 30;
                                            iStateUT5526++;
                                            break;
                                        }
                                    }
                                }
                            };
                        } while (ringCountUT5526 >= 4);
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
                        iCntWaitUT5526 = 1;
                        iStateUT5526++;
                    }
                    break;
                case 3:
                    if (iCntWaitUT5526 == 0)
                    {
                    //    string strComData = "01MORDVO";  // read current channel data
                    //    byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
                    //    byte[] byBCC = new byte[1];
                    //    byBCC[0] = UTBus_LRC(cmdStr, 8);
                    //    _UT5526Port?.Write(leadChar, 0, 1);
                    //    _UT5526Port?.Write(strComData);
                    //    _UT5526Port?.Write(byBCC, 0, 1);
                     //   _UT5526Port?.Write(endChar, 0, 1);
                     //  iCntWaitUT5526 = 60;
                        iStateUT5526++;
                    }
                    break;
                case 4:
                    if (ringCountUT5526 >= 11 )
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

                                                DisplayGroup((GroupBox)ctrl, collectData[iIdxGetUT5526].CurrentData, bErr);
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
                        } while (ringCountUT5526 >= 11);
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
            ProcUT5526();
         
        }

        private void FmMain_Load(object sender, EventArgs e)
        {
     
            imgDigiNormal = new Image[10];
            imgDigiNormalDot = new Image[10];
            imgDigiError = new Image[10];
            imgDigiErrorDot = new Image[10];
            ringBufferUT5526 = new byte[lenBufUT5526];
            ringBufferEM02 = new byte[lenBufEM02];

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Panel)
                {
                    Panel panel = (Panel)ctrl;
                    if (panel.Name == "panelDisplay")
                    {
                        foreach (Control ctrlGp in panel.Controls)
                        {
                            if (ctrlGp is GroupBox)
                            {
                                if (ctrlGp.Tag == null)
                                {
                                    MessageBox.Show("缺通道設定!");
                                    Application.Exit();
                                }
                                string strChannel = ctrlGp.Tag?.ToString();
                                int channelInt = int.Parse(strChannel);
                                if (channelInt > 32 || channelInt < 1)
                                {
                                    MessageBox.Show("通道設定錯誤!");
                                    Application.Exit();
                                }
                                GroupBox grp = (GroupBox)ctrlGp;
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
                                if (collectData.Count == 0)  // add a dummy line
                                {
                                    collectData.Add(new ChannelData() { ChannelName = ctrlGp.Text, CmdSelected = "01MOCH" + channelInt.ToString().PadLeft(2, '0'), PreviousData = 0, CurrentData = 0, StandardData = iStandard });
                                }
                                collectData.Add(new ChannelData() { ChannelName = ctrlGp.Text, CmdSelected = "01MOCH" + channelInt.ToString().PadLeft(2, '0'), PreviousData = 0, CurrentData = 0, StandardData = iStandard });
                            }
                        }
                    }
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

            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            string[] allkeys = config.AppSettings.Settings.AllKeys;


            var results = Array.Find(allkeys, s => s.Equals("UT5526"));
            if (results == null)
            {
                config.AppSettings.Settings.Add("UT5526", "COM5");
            }
            else
            {
                comUT5526 = config.AppSettings.Settings["UT5526"].Value;
            }

            results = Array.Find(allkeys, s => s.Equals("EM02"));
            if (results == null)
            {
                config.AppSettings.Settings.Add("EM02", "COM4");
            }
            else
            {
                comEM02 = config.AppSettings.Settings["EM02"].Value;

            }
            results = Array.Find(allkeys, s => s.Equals("MES"));
            if (results == null)
            {
                config.AppSettings.Settings.Add("MES", "COM2");
            }
            else
            {
                comMES = config.AppSettings.Settings["MES"].Value;

            }
            config.Save(ConfigurationSaveMode.Modified);



            string[] ports = SerialPort.GetPortNames();
            cbMES.Items.AddRange(ports);
            cbUT5526.Items.AddRange(ports);
            cbEM02.Items.AddRange(ports);

            cbMES.SelectedItem = cbMES;
            cbEM02.SelectedItem = comEM02;
            cbUT5526.SelectedItem = comUT5526;



            leadChar[0] = 0x01;
            endChar[0] = 0x04;

            InitializeTimer();
          //  initComBarcode("COM3");
            initComUT5526("COM6");

            if (bErrorUT5526 == false)
            {
                SetUT5526VoltRange();
            }

        }

        private void DisplayPictureBox(PictureBox pb, int iData, bool bErr)
        {
            if (iData <= 9)
            {
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = bErr ? imgDigiError[iData] : imgDigiNormal[iData];
            }
            else
            {
                pb.Image = Resource1.none;
            }

        }
        private void DisplayPictureBoxDot(PictureBox pb, int iData, bool bErr)
        {
            if (iData <= 9)
            {
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = bErr ? imgDigiErrorDot[iData] : imgDigiNormalDot[iData];
            }
            else
            {
                pb.Image = Resource1.none;
            }
        }

        private void DisplayGroup(GroupBox gb, int iData, bool bErr)
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

            // auto range 0.00 ~ 9.00  or  10.0 ~ 99.0

            if (iData > 999)
            {
                int iLeft = iData / 1000;
                int iMid = (iData - (iLeft * 1000)) / 100;
                int iRight = (iData - (iLeft * 1000) - (iMid * 100)) / 10;

                DisplayPictureBox(pbL, iLeft, bErr);
                DisplayPictureBoxDot(pbM, iMid, bErr);
                DisplayPictureBox(pbR, iRight, bErr);
            }
            else
            {
                int iLeft = iData / 100;
                int iMid = (iData - (iLeft * 100)) / 10;
                int iRight = iData % 10;
                bErr = false;
                if (iRight == 0 && iLeft == 0 && iMid == 0)
                {
                    DisplayPictureBoxDot(pbL, 10, bErr);
                    DisplayPictureBox(pbM, 10, bErr);
                    DisplayPictureBox(pbR, iRight, bErr);
                }
                else
                {
                    DisplayPictureBoxDot(pbL, iLeft, bErr);
                    DisplayPictureBox(pbM, iMid, bErr);
                    DisplayPictureBox(pbR, iRight, bErr);
                }

            }

        }

        private void BtnTest_Click(object sender, EventArgs e)
        {

            byte byTest = ringBufferUT5526[0];

            foreach (Control ctrl in this.Controls)
            {

                if (ctrl is GroupBox)
                {
                    DisplayGroup((GroupBox)ctrl, 123, true);
                }
            }
        }

        private static byte UTBus_LRC(byte[] str, int len)

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

        private void BtnTest2_Click(object sender, EventArgs e)
        {
            // string strComData = "01MORDVO";
            string strComData = "01MORG03";  // set range = 200V
            byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
            byte[] byBCC = new byte[1];
            byBCC[0] = UTBus_LRC(cmdStr, 8);

            _UT5526Port?.Write(leadChar, 0, 1);
            _UT5526Port?.Write(strComData);
            _UT5526Port?.Write(byBCC, 0, 1);
            _UT5526Port?.Write(endChar, 0, 1);
        }

        private void FmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
           
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

        private void BtnRead_Click(object sender, EventArgs e)
        {
         
            ResetLedDisplay();
            iIdxGetUT5526 = 0;
     
            timer1.Enabled = true;
            bCmdReadSend = true;
            iCntGetUT5526 = 25;
          
        }

        private void resetComPorts()
        {
            if (_MESPort?.IsOpen == true)
            {
                _MESPort?.Close();
            };
            if (_EM02Port?.IsOpen == true)
            {
                _EM02Port?.Close();
            }
            if (_UT5526Port?.IsOpen == true)
            {
                _UT5526Port?.Close();
            }
      
            initComEM02(comEM02);
            initComUT5526(comUT5526);
        }
        private void btnSaveSetting_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            config.AppSettings.Settings.Remove("MES");
            config.AppSettings.Settings.Add("MES", cbMES.SelectedItem.ToString());
            config.Save(ConfigurationSaveMode.Modified);
            MessageBox.Show("Com Port ��s����!");
        }

        private void btnGetSetting_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            comEM02 = config.AppSettings.Settings["EM02"].Value;
            comUT5526 = config.AppSettings.Settings["UT5526"].Value;
            comEM02 = config.AppSettings.Settings["TH02"].Value;
            cbMES.SelectedItem = comMES;
            cbUT5526.SelectedItem = comUT5526;
            cbEM02.SelectedItem = comEM02;
            MessageBox.Show("Com Port Ū��OK!");
            resetComPorts();
        }
    }
}