﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;




namespace EM02_E_HalfTester
{

   
    public partial class fmMain : Form
    {
       

        private SerialPort? _UT5526Port;
        private bool UT5526_receiving = false;
        delegate void GoGetUT5526Data(byte[] buffer);
        private Thread threadUT5526;

        private SerialPort? _BarCodePort;
        private bool BarCode_receiving = false;
        delegate void GoGetBarCodeData(byte[] buffer);
        private Thread threadBarCode;

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
        private bool bWaitACC = false;


        private const UInt16 lenBufEM02 = 512;
        private byte[] ringBufferEM02;
        private int ringCountEM02 = 0;
        private int ringOutputEM02 = 0;
        private int ringInputEM02 = 0;
        private int iIdxReadBarEM02 = 0;  // current read channel 
        private int iCntReadEM02 = 0;  // read how many channels
        private int iCurrentReadEM02 = 0;
        private int iCntWaitEM02 = 0;
        private int iStateEM02 = 0;
        private int iCntEM02 = 0;
        private int iCntCount = 0;

        private const UInt16 lenBufBarCode = 256;
        private byte[] ringBufferBarCode;
        private int ringCountBarCode = 0;
        private int ringOutputBarCode = 0;
        private int ringInputBarCode = 0;
        private int iIdxReadBarCode = 0;  // current read channel 
        private int iCntReadBarCode = 0;  // read how many channels
        private int iCurrentReadBarCode = 0;
        private int iCntWaitBarCode = 0;
        private int iStateBarCode = 0;
        private bool bErrorBarcode = false;
        


        private string[] chanelNames;
        private Image[] imgDigiNormal;
        private Image[] imgDigiNormalDot;
        private Image[] imgDigiError;
        private Image[] imgDigiErrorDot;
        private byte[] leadChar = new byte[1];
        private byte[] endChar = new byte[1];

        string strLogFilename = "";
        StreamWriter file ;

        private bool bSerialNO = false;  // serial no in already
        private bool bReadUT5526 = false;
        private int iErrors = 0;
     

        string comUT5526 = "";
        string comEM02 = "";
        string comBarCode = "";
        DateTime time00;
        DateTime time01;

        class EM02DBGTYPE
        {
            public string code { get; set; }
            public string name { get; set; }
        }


        private List<EM02DBGTYPE> car_type = new List<EM02DBGTYPE>();
        private List<EM02DBGTYPE> gpsStatus = new List<EM02DBGTYPE>();
        private List<EM02DBGTYPE> gSensorStatus = new List<EM02DBGTYPE>();
        private List<EM02DBGTYPE> sdCardStatus = new List<EM02DBGTYPE>();
        private List<EM02DBGTYPE> cameraStatus = new List<EM02DBGTYPE>();
       
        private List<EM02DBGTYPE> em02DataField = new List<EM02DBGTYPE>();

        class ChannelData
        {
            public string ChannelName { get; set; }
            public string CmdSelected { get; set; }
            public int PreviousData { get; set; }
            public int CurrentData { get; set; }
            public int StandardData { get; set; }
            public string fieldName { get; set; }
        }


        private List<ChannelData> collectData = new List<ChannelData>();


        class DynamicConverter : JsonConverter<Dynamic>
        {
          
            public override Dynamic ReadJson(JsonReader reader, Type objectType, [AllowNull] Dynamic existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }


            public override void WriteJson(JsonWriter writer, [AllowNull] Dynamic value, Newtonsoft.Json.JsonSerializer serializer)
            {
                writer.WriteStartObject();
                foreach (var kvp in value._dictionary)
                {
                    writer.WriteValue(kvp.Key);
                    writer.WriteValue(kvp.Value);
                }
                writer.WriteEndObject();
            }
        }

        class Dynamic : DynamicObject
        {
            internal Dictionary<string, object> _dictionary = new Dictionary<string, object>();
            public object this[string propertyName]
            {
                get { return _dictionary[propertyName]; }
                set { AddProperty(propertyName, value); }
            }
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                 return _dictionary.TryGetValue(binder.Name, out result);
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                AddProperty(binder.Name, value);
                return true;
            }
           
             public void AddProperty(string name, object value)
             {
                _dictionary[name] = value;
             }
        }


     

        Dynamic em02TestDatas = new Dynamic();



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

        private void initComBarCode(string sPort)
        {

            _BarCodePort = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            if (_BarCodePort.IsOpen == false)
            {
                try
                {
                    _BarCodePort.Open();
                    //開啟 Serial Port
                    BarCode_receiving = true;
                    //開啟執行續做接收動作
                    threadBarCode = new Thread(DoReceiveBarCode);
                    threadBarCode.IsBackground = true;
                    threadBarCode.Start();

                }
                catch (Exception)
                {
                    // port will not be open, therefore will become null
                    MessageBox.Show("無法開啟BarCode Port!");
                    Application.Exit();
                }
            }
        }

        private void initComEM02(string sPort)
        {
            _EM02Port = new SerialPort()
            {
                PortName = sPort,
                BaudRate = 115200,
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
                    EM02_receiving = true;
                    //開啟執行續做接收動作
                    threadEM02 = new Thread(DoReceiveEM02);
                    threadEM02.IsBackground = true;
                    threadEM02.Start();

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


        private void DoReceiveBarCode()
        {
            Byte[] buffer = new Byte[256];

            try
            {
                while (BarCode_receiving)
                {
                    if (_BarCodePort?.BytesToRead >= 1 && _BarCodePort.BytesToWrite == 0)
                    {
                        Int32 length = _BarCodePort.Read(buffer, 0, buffer.Length);
                        Array.Resize(ref buffer, length);
                        GoGetBarCodeData d = new GoGetBarCodeData(BarCodeShow);
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
            Byte[] buffer = new Byte[512];

            try
            {
                while (EM02_receiving)
                {
                    if (_EM02Port?.BytesToRead >= 1 && _EM02Port.BytesToWrite == 0)
                    {
                        Int32 length = _EM02Port.Read(buffer, 0, buffer.Length);
                        Array.Resize(ref buffer, length);
                        GoGetEM02Data d = new GoGetEM02Data(EM02Show);
                        this.Invoke(d, new Object[] { buffer });
                        Array.Resize(ref buffer, length);
                    }

                //    Thread.Sleep(2);
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
            if (ringCountEM02 == 0)
            {
                return 0;
            }
                byte byData = ringBufferEM02[ringOutputEM02];
                ringCountEM02--;
                if (ringCountEM02 < 0) ringCountEM02 = 0;
                ringOutputEM02 = (ringOutputEM02 + 1) & (lenBufEM02 - 1);
                return byData;
            
            
        }
        private int SearchEM02LineFeed()
        {
            if (ringCountEM02 > 0)
            {
                for (int i = 0; i <= ringCountEM02; i++)
                {
                    int iOp = (ringOutputEM02 + i) & (lenBufEM02 - 1);
                    if (ringBufferEM02[iOp] == 0x0a)
                    {
                        return i + 1;
                    }
                }
                return 0;

            }
            else
            {
                return 0;   // nothing in there
            }
        }
        private int  SearchEM02Tail() // tail is 0x1b 0x5b 0x6d  [ESC][m
        {

            int iCntSearch = ringCountEM02;
            int iIdxSearch = 0;
            int iOp = 0;
            while (iCntSearch >=3)
            {
                iOp = (ringOutputEM02 + iIdxSearch) & (lenBufEM02 - 1);
                iIdxSearch++;
                iCntSearch--;
                if (ringBufferEM02[iOp] == 0x1b)
                {  
                    iCntSearch--;
                    iOp = (ringOutputEM02 + iIdxSearch) & (lenBufEM02 - 1);
                    iIdxSearch++;
                    if (ringBufferEM02[iOp] == 0x5b)
                    { 
                        
                        iOp = (ringOutputEM02 + iIdxSearch) & (lenBufEM02 - 1);
                        iIdxSearch++;
                        iCntSearch--;
                        if (ringBufferEM02[iOp] == 0x6d)
                        {
                            return iIdxSearch + 1;
                        }
                    }
                   
                }
            }
            return 0;

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

        private void SetRingBarCode(byte byData)
        {
            ringBufferBarCode[ringInputBarCode] = byData;
            ringCountBarCode++;
            ringInputBarCode = (ushort)((ringInputBarCode + 1) & (lenBufBarCode - 1));
        }
        private byte GetRingBarCode()
        {
            byte byData = ringBufferBarCode[ringOutputBarCode];
            ringCountBarCode--;
            if (ringCountBarCode < 0) ringCountBarCode = 0;
            ringOutputBarCode = (ushort)((ringOutputBarCode + 1) & (lenBufBarCode - 1));
            return byData;
        }
        private int SearchBarCodeLineFeed()
        {
            if (ringCountBarCode > 0)
            {
                for (int i = 0; i < ringCountBarCode; i++)
                {
                    int iOp = (ringOutputBarCode + i) & (lenBufBarCode - 1);
                    if (ringBufferBarCode[iOp] == 0x0a)
                    {
                        return i + 1;
                    }
                }
                return 0;

            }
            else
            {
                return 0;   // nothing in there
            }
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
          
            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                SetRingUT5526(buf[i]);
            }
        }

        public void BarCodeShow(byte[] buffer)
        {
           
            byte[] buf = buffer;
            for (int i = 0; i < buf.Length; i++)
            {
                SetRingBarCode(buf[i]);
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
            
            this.timer1.Tick += new EventHandler(timer1_Tick_1);
            timer1.Enabled = true;

        }
        private void procEM02GM(int iLFCRPos)
        {
        
                byte[] buf = new byte[iLFCRPos];
                for (int i = 0; i < iLFCRPos; i++)
                {
                    buf[i] = GetRingEM02();
                }
                var str = System.Text.Encoding.Default.GetString(buf);
                string[] strTokens = str.Split(',');
                switch (strTokens[0])
                {
                    case "FACTORY":
                        lblEM02.Text = strTokens[1];
                        break;
                    case "BOOT":
                        lblEM02.Text = strTokens[1];
                        break;
                    case "DEV":
                        lblEM02.Text = strTokens[1];
                        break;
                    case "CAN":
                        lblEM02.Text = strTokens[1];
                        break;
                    case "INIT":
                        lblEM02.Text = strTokens[1];
                        break;
                    case "SD":
                        lblEM02.Text = strTokens[1];
                        break;
                    default:
                        iStateEM02 = 0;
                        break;
                }
          
        }

   


        private void  procEM02() 
        {
            const byte ESC = 0x1b;
              
            switch (iStateEM02)
            {
                case 0:
                   
                    if (ringCountEM02 >= 11)
                    {
                        lblLength.Text = iCntCount.ToString();
                        iCntCount++;
                        do {                      
                                        if (GetRingEM02() == ESC)
                                        {
                                            if (GetRingEM02() == '[')
                                            {
                                                if (GetRingEM02() == '1')
                                                {
                                                    if (GetRingEM02() == ';')
                                                    {
                                                        if (GetRingEM02() == '3')
                                                        {
                                                            if (GetRingEM02() == '3')
                                                            {
                                                                if (GetRingEM02() == 'm')
                                                                {
                                                                    if (GetRingEM02() == '$')
                                                                    {
                                                                        if (GetRingEM02() == 'G')
                                                                        {
                                                                            if (GetRingEM02() == 'M')
                                                                            {
                                                                                if (GetRingEM02() == ',')
                                                                                {
                                                                                    iCntEM02++;
                                                                                    lblTime.Text = iCntEM02.ToString();
                                                                                    iStateEM02++;
                                                                                    break;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                        } while (ringCountEM02 >= 11);
                    }
                    break;
                case 1:
                    if( ringCountEM02 >= 13)
                    {
                      //  lblLength.Text = ringCountEM02.ToString();
                        int iLFCRPos = SearchEM02Tail();
                        if (iLFCRPos > 0)  // find a Line feed
                        {
                            byte[] buf = new byte[iLFCRPos];
                            for (int i = 0; i < iLFCRPos; i++)
                            {
                                buf[i] = GetRingEM02();
                            }
                            var str = System.Text.Encoding.Default.GetString(buf);
                            string[] strTokens = str.Split(',');
                            
                            switch (strTokens[0])
                            {
                                case "FACTORY":
                                    startReadUT5526();
                                    lblEM02.Text = strTokens[0];
                                    if (strTokens[1] == "SD")
                                    {
                                        break;
                                    }
                                    lblSoftware.Text = strTokens[1];
                                    lblFirmware.Text = strTokens[2];
                                    lblCarModel.Text = car_type.Find(x => x.code == strTokens[3]).name;
                                    lblGPS.Text = gpsStatus.Find(x => x.code == strTokens[4]).name;
                                    lblSpeed.Text = strTokens[5];
                                   // Random ran = new Random();

                                   // lblSpeed.Text = (30 + ran.Next(62)).ToString();
                                    lblGSensor.Text = gSensorStatus.Find(x => x.code == strTokens[6]).name;
                                    lblACC.Text = strTokens[7];
                                    lblSDCard.Text = sdCardStatus.Find(x => x.code == strTokens[9]).name;
                                    lblFrontCAM.Text = cameraStatus.Find(x => x.code == strTokens[10]).name;
                                    lblRearCAM.Text = cameraStatus.Find(x => x.code == strTokens[11]).name;
                                    em02TestDatas["dateTest"] = DateTime.Now.ToString();
                                    em02TestDatas["SN"] = lblSN.Text;
                                    em02TestDatas["SoftwareVersion"] = lblSoftware.Text;
                                    em02TestDatas["FirmwareVersion"] = lblFirmware.Text;
                                    
                                    
                                    break;
                                case "BOOT":
                                    lblEM02.Text = strTokens[0];
                                    startReadUT5526();
                                   
                                    break;
                                case "DEV":
                                    lblEM02.Text = strTokens[0];
                                    break;
                                case "CAN":
                                    lblEM02.Text = strTokens[0];
                                    break;
                                case "INIT":
                                    startReadUT5526();
                                    lblEM02.Text = strTokens[0];
                                    break;
                                case "SD":
                                    lblEM02.Text = strTokens[0];
                                    break;
                                default:
                                    iStateEM02 = 0;
                                    break;
                            }
                            iStateEM02=0;
                        }
                    }
                   
                    break;
                case 2:
                    iStateEM02 = 0;
                    break;
                 default:
                    iStateEM02 = 0;
                    break;
            }
        }  


        private void procBarcode()
        {
            if (bErrorBarcode)
            {
                iStateBarCode = 0;
                return;
            }
            switch (iStateBarCode)
            {
                case 0:
                    if (ringCountBarCode >= 20)    // EM02 SN ==> 20 byte with 0a 0d
                    {
                        int iTemp = SearchBarCodeLineFeed();
                        if (iTemp >= 18)    // it is SN��
                        {
                            byte[] bySN = new byte[iTemp + 1];
                            for (int i = 0; i < iTemp; i++)
                            {
                                bySN[i] = GetRingBarCode();
                            }
                            string strSN = System.Text.Encoding.Default.GetString(bySN);
                            lblSN.Text = strSN;
                            if (strSN.Contains("EM02"))
                            {
                                bSerialNO = true;
                                clearComBuffer();
                                iStateBarCode++;
                            } else
                            {
                                iStateBarCode = 0; // not em02 barcode
                            }                                                
                        } else
                        {
                            while (iTemp > 0) // if not 18 byte must noise
                            {
                                GetRingBarCode();
                                iTemp--;
                            }
                        }

                    }
                    break;
                default:
                    iStateBarCode = 0;
                    break;

            }
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
                                                if (collectData[iIdxGetUT5526].StandardData == 0)
                                                {
                                                    bErr = false;
                                                }
                                                iErrors = (bErr) ? (iErrors + 1) : iErrors;
                                                em02TestDatas[ctrl.Text] = iCurrentData.ToString();
                                                DisplayGroup((GroupBox)ctrl, collectData[iIdxGetUT5526].CurrentData, bErr);
                                            }
                                        }
                                    }

                                }

                                iIdxGetUT5526++;  // move to next
                                if (iCntGetUT5526 == 0)
                                {
                                 
                                    bWaitACC = true;
                                    pbPushButton.Visible= true;
                                    pbPushButton.Image = Resource1.red_button_spam;
                                    iCntGetUT5526 = 1;
                                    iIdxGetUT5526 = 0;
                                    iStateUT5526 = 0;
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
        private void ProcUT5526ACC()
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
                  
                        time00 = DateTime.Now;
                        //  iCntRead--;
                        if (collectData.Count > iIdxGetUT5526)
                        {
                            iIdxGetUT5526 = collectData.FindIndex(x => x.ChannelName == "ACC");  
                            string strComData = collectData[iIdxGetUT5526].CmdSelected;   // send channel select comand                                                                //  string strComData = "01MOCH05";   // send channel for ACC channel comand
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
                    break;
                case 1:                                             // wait 1ER0
                    if (ringCountUT5526 >= 4)
                    {
                        do
                        {
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
                    if (iCntWaitUT5526 == 0)
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
                                              //  int iOffset = iCurrentData - (collectData[iIdxGetUT5526].StandardData);
                                              //  if (iOffset < 0) iOffset = 0 - (iOffset);
                                              //  bool bErr = (iOffset > (collectData[iIdxGetUT5526].StandardData / 10)) ? true : false;
                                             //   
                                                DisplayGroup((GroupBox)ctrl, collectData[iIdxGetUT5526].CurrentData, false);
                                            }
                                        }
                                    }

                                }

                                if (collectData[iIdxGetUT5526].CurrentData <= 100)
                                {
                                    bWaitACC = false;
                                
                                    pbPushButton.Image = (iErrors == 0) ? Resource1.pass : Resource1.fail;
                                    iCntGetUT5526 = 0;
                                    iIdxGetUT5526 = 0;
                                    bSerialNO = false;
                                    em02TestDatas["RESULT"] = "PASS";
                                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(em02TestDatas._dictionary);
                                    using (StreamWriter sw = File.AppendText(strLogFilename))
                                    {
                                        sw.WriteLine(output.ToString());
                                        sw.Close();
                                    }
                                  
                                } else
                                {
                                    iCntGetUT5526 = 1;
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

        private void timer1_Tick_1(object sender, EventArgs e)
        {
          //  lblLength.Text = iCntCount.ToString();
          //  iCntCount++;
            procBarcode();
            if(bSerialNO == true)
            {
                procEM02();
            } 

           
        
            if (bWaitACC == false)
            {
                ProcUT5526();
            }
            else
            {
                ProcUT5526ACC();
            }

        }
     
        private void FmMain_Load(object sender, EventArgs e)
        {

            imgDigiNormal = new Image[10];
            imgDigiNormalDot = new Image[10];
            imgDigiError = new Image[10];
            imgDigiErrorDot = new Image[10];
            ringBufferUT5526 = new byte[lenBufUT5526];
            ringBufferEM02 = new byte[lenBufEM02];
            ringBufferBarCode = new byte[lenBufBarCode];


           strLogFilename = String.Format("EM02-F-Log-{0}.csv",DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"));
            

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
                                    collectData.Add(new ChannelData() { ChannelName = "Dummy", CmdSelected = "01MOCH" + channelInt.ToString().PadLeft(2, '0'), PreviousData = 0, CurrentData = 0, StandardData = iStandard });
                                }
                                collectData.Add(new ChannelData() { ChannelName = ctrlGp.Text, CmdSelected = "01MOCH" + channelInt.ToString().PadLeft(2, '0'), PreviousData = 0, CurrentData = 0, StandardData = iStandard });
                                em02TestDatas[ctrlGp.Text] = "0.0";
                            }
                        }
                    }
                }

            }

            car_type.Add(new EM02DBGTYPE() { code = "0", name = "UNKNOW" });
            car_type.Add(new EM02DBGTYPE() { code = "1", name = "AZ" });
            car_type.Add(new EM02DBGTYPE() { code = "2", name = "RE" });
            car_type.Add(new EM02DBGTYPE() { code = "3", name = "NS" });
            car_type.Add(new EM02DBGTYPE() { code = "4", name = "SR" });
            car_type.Add(new EM02DBGTYPE() { code = "5", name = "DE" });
            car_type.Add(new EM02DBGTYPE() { code = "6", name = "JD" });
            car_type.Add(new EM02DBGTYPE() { code = "7", name = "NPZ" });

            gpsStatus.Add(new EM02DBGTYPE() { code = "-1", name = "尚未抓取到NEMA資料" });
            gpsStatus.Add(new EM02DBGTYPE() { code = "0", name = "已正確抓取到NEMA資料" });
            gpsStatus.Add(new EM02DBGTYPE() { code = "1", name = "時間校正完成" });
            gpsStatus.Add(new EM02DBGTYPE() { code = "2", name = "定位完成" });

            gSensorStatus.Add(new EM02DBGTYPE() { code = "0", name = "未偵測到裝置" });
            gSensorStatus.Add(new EM02DBGTYPE() { code = "1", name = "有偵測到裝置" });

            sdCardStatus.Add(new EM02DBGTYPE() { code = "0", name = "未插卡" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "1", name = "插入偵測" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "2", name = "卡片異常" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "3", name = "檔案系統確認中" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "4", name = "檔案系統確認失敗" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "5", name = "檔案系統出現例外" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "6", name = "SD卡掛載完成" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "7", name = "SD卡掛載失敗" });
            sdCardStatus.Add(new EM02DBGTYPE() { code = "8", name = "SD卡閒置" });

            cameraStatus.Add(new EM02DBGTYPE() { code = "0", name = "1080P25" });
            cameraStatus.Add(new EM02DBGTYPE() { code = "1", name = "1080P30" });
            cameraStatus.Add(new EM02DBGTYPE() { code = "2", name = "720P25" });
            cameraStatus.Add(new EM02DBGTYPE() { code = "3", name = "720P30" });
            cameraStatus.Add(new EM02DBGTYPE() { code = "4", name = "720P60" });
            cameraStatus.Add(new EM02DBGTYPE() { code = "5", name = "無連接" });

            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Standby 5V", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Standby3V3", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Rear CAM Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });
            em02DataField.Add(new EM02DBGTYPE() { code = "Main Power", name = "fMainPower" });

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
                config.AppSettings.Settings.Add("UT5526", "COM6");
            }
            else
            {
                comUT5526 = config.AppSettings.Settings["UT5526"].Value;
            }

            results = Array.Find(allkeys, s => s.Equals("EM02"));
            if (results == null)
            {
                config.AppSettings.Settings.Add("EM02", "COM7");
            }
            else
            {
                comEM02 = config.AppSettings.Settings["EM02"].Value;

            }
            results = Array.Find(allkeys, s => s.Equals("BarCode"));
            if (results == null)
            {
                config.AppSettings.Settings.Add("BarCode", "COM3");
            }
            else
            {
                comBarCode = config.AppSettings.Settings["BarCode"].Value;

            }
            config.Save(ConfigurationSaveMode.Modified);

            string[] ports = SerialPort.GetPortNames();
            cbBarCode.Items.AddRange(ports);
            cbUT5526.Items.AddRange(ports);
            cbEM02.Items.AddRange(ports);

            cbBarCode.SelectedItem =  comBarCode;
            cbEM02.SelectedItem = comEM02;
            cbUT5526.SelectedItem = comUT5526;

            leadChar[0] = 0x01;
            endChar[0] = 0x04;

            InitializeTimer();
          //  initComBarcode("COM3");
            initComUT5526(comUT5526);
            initComBarCode(comBarCode);
            initComEM02(comEM02);

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
            if (_BarCodePort?.IsOpen == true)
            {
                _BarCodePort.Close();
            }
            if (_EM02Port?.IsOpen == true)
            {
                _EM02Port.Close();
            }
            Environment.Exit(Environment.ExitCode);

        }

        private void startReadUT5526()
        {
            string strComData = "01MORG03";  // set range = 200V
            byte[] cmdStr = Encoding.ASCII.GetBytes(strComData);
            byte[] byBCC = new byte[1];
            byBCC[0] = UTBus_LRC(cmdStr, 8);

            _UT5526Port?.Write(leadChar, 0, 1);
            _UT5526Port?.Write(strComData);
            _UT5526Port?.Write(byBCC, 0, 1);
            _UT5526Port?.Write(endChar, 0, 1);

            while(_UT5526Port?.BytesToWrite > 0)
            {

            }

            if (bReadUT5526 == false)
            {
                ResetLedDisplay();
                iIdxGetUT5526 = 0;
                iCntGetUT5526 = 19;
                bReadUT5526 = true;
            } else
            {

            }
        
        }
        private void BtnRead_Click(object sender, EventArgs e)
        {
           

            startReadUT5526();
          
        }

        private void resetComPorts()
        {
            if (_BarCodePort?.IsOpen == true)
            {
                _BarCodePort?.Close();
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
            initComBarCode(comBarCode);
          
        }

        private void clearComBuffer()
        {
            if(_EM02Port?.IsOpen == true)
            {
                _EM02Port.DiscardInBuffer();
            }
            if(_UT5526Port?.IsOpen == true)
            {
                _UT5526Port.DiscardInBuffer();
            }
            ringCountEM02 = 0;
            ringCountUT5526 = 0;
            ringInputEM02 = 0;
            ringInputUT5526 = 0;
            ringOutputEM02 = 0;
            ringOutputUT5526 = 0;
        }
        private void btnSaveSetting_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            if(cbBarCode.SelectedItem.ToString() != "")
            {
                config.AppSettings.Settings.Remove("BarCode");
                config.AppSettings.Settings.Add("BarCode", cbBarCode.SelectedItem.ToString());
                config.Save(ConfigurationSaveMode.Modified);
            }
            if (cbUT5526.SelectedItem.ToString() != "")
            {
                config.AppSettings.Settings.Remove("UT5526");
                config.AppSettings.Settings.Add("UT5526", cbUT5526.SelectedItem.ToString());
                config.Save(ConfigurationSaveMode.Modified);
            }
            if (cbEM02.SelectedItem.ToString() != "")
            {
                config.AppSettings.Settings.Remove("EM02");
                config.AppSettings.Settings.Add("EM02", cbEM02.SelectedItem.ToString());
                config.Save(ConfigurationSaveMode.Modified);
            }
          
            MessageBox.Show("Com Port 更新! 請重新開機!");
        }

        private void btnGetSetting_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            comEM02 = config.AppSettings.Settings["EM02"].Value;
            comUT5526 = config.AppSettings.Settings["UT5526"].Value;
            comBarCode = config.AppSettings.Settings["BarCode"].Value;
            cbBarCode.SelectedItem = comBarCode;
            cbUT5526.SelectedItem = comUT5526;
            cbEM02.SelectedItem = comEM02;
            MessageBox.Show("Com Port 重新設定OK!");
            resetComPorts();
        }


        private void clearEM02Msg()
        {
            lblACC.Text = "-";
            lblCarModel.Text = "-";
            lblFirmware.Text = "-";
            lblFrontCAM.Text = "-"; 
            lblGPS.Text = "-";  
            lblGSensor.Text = "-";
            lblRearCAM.Text = "-";
            lblSDCard.Text = "-";
            lblSoftware.Text = "-";
            lblSpeed.Text = "-";
            
            
        }
        private void pbPushButton_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox) sender;
        
            if(bWaitACC == true )
            {
                bWaitACC = false;
                pb.Image = Resource1.fail;
                bSerialNO = false;
                iErrors = 0;
                bReadUT5526 = false;
                iCntGetUT5526 = 0;
                iIdxGetUT5526 = 0;
                em02TestDatas["RESULT"] = "FAIL";
                var output = Newtonsoft.Json.JsonConvert.SerializeObject(em02TestDatas._dictionary);
                using (StreamWriter sw = File.AppendText(strLogFilename))
                {
                    sw.WriteLine(output.ToString());
                    sw.Close();
                }
                return;
            }
            if( iErrors == 0 && bWaitACC == false)
            {
                pb.Image = Resource1.none;
                bSerialNO = false;
                pb.Visible = false;
                iErrors = 0;
                lblSN.Text = "-";
                bSerialNO = false;
                ResetLedDisplay();
                clearEM02Msg();
                bReadUT5526 = false;    
            } else
            {
                pb.Image = Resource1.none;
                bSerialNO = false;
                pb.Visible = false;
                iErrors = 0;
                bWaitACC = false;
                lblSN.Text = "-";
                ResetLedDisplay();
                clearEM02Msg();
                bReadUT5526 = false;
            }
        }
    }
}