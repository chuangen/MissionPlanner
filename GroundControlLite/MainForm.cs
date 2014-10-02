using log4net;
using MissionPlanner.Comms;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MissionPlanner
{
    public partial class MainV2 : Form
    {
        static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// use to store all internal config
        /// </summary>
        public static Hashtable config = new Hashtable();
        public static string getConfig(string paramname)
        {
            if (config[paramname] != null)
                return config[paramname].ToString();
            return "";
        }

        /// <summary>
        /// mono detection
        /// </summary>
        public static bool MONO = false;

        /// <summary>
        /// store the time we first connect
        /// </summary>
        DateTime connecttime = DateTime.Now;
        DateTime nodatawarning = DateTime.Now;
        DateTime OpenTime = DateTime.Now;

        /// <summary>
        /// speech engine enable
        /// </summary>
        public static bool speechEnable = false;
        /// <summary>
        /// spech engine static class
        /// </summary>
        public static Speech speechEngine = null;

        /// <summary>
        /// used to call anything as needed.
        /// </summary>
        public static MainV2 instance = null;
        /// <summary>
        /// Active Comport interface
        /// </summary>
        public static MAVLinkInterface comPort = new MAVLinkInterface();

        /// <summary>
        /// enum of firmwares
        /// </summary>
        public enum Firmwares
        {
            ArduPlane,
            ArduCopter2,
            //ArduHeli,
            ArduRover,
            Ateryx,
            ArduTracker
        }


        public MainV2()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            instance = this;


            try
            {
                log.Info("Create FD");
            }
            catch (ArgumentException e)
            {
                //http://www.microsoft.com/en-us/download/details.aspx?id=16083
                //System.ArgumentException: Font 'Arial' does not support style 'Regular'.

                log.Fatal(e);
                CustomMessageBox.Show(e.ToString() + "\n\n Font Issues? Please install this http://www.microsoft.com/en-us/download/details.aspx?id=16083");
                //splash.Close();
                //this.Close();
                Application.Exit();
            }
            catch (Exception e) { log.Fatal(e); CustomMessageBox.Show("A Major error has occured : " + e.ToString()); Application.Exit(); }

            MenuConnect.Click += MenuConnect_Click;


            // setup guids for droneshare
            if (!MainV2.config.ContainsKey("plane_guid"))
                MainV2.config["plane_guid"] = Guid.NewGuid().ToString();

            if (!MainV2.config.ContainsKey("copter_guid"))
                MainV2.config["copter_guid"] = Guid.NewGuid().ToString();

            if (!MainV2.config.ContainsKey("rover_guid"))
                MainV2.config["rover_guid"] = Guid.NewGuid().ToString();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            log.Info("closing fd");
            try
            {
            }
            catch { }

            base.OnClosing(e);
        }

        private void MenuConnect_Click(object sender, EventArgs e)
        {
            comPort.giveComport = false;

            log.Info("MenuConnect Start");

            // sanity check
            if (comPort.BaseStream.IsOpen && comPort.MAV.cs.groundspeed > 4)
            {
                if (DialogResult.No == CustomMessageBox.Show("Your model is still moving are you sure you want to disconnect?", "Disconnect", MessageBoxButtons.YesNo))
                {
                    return;
                }
            }

            try
            {
                log.Info("Cleanup last logfiles");
                // cleanup from any previous sessions
                if (comPort.logfile != null)
                    comPort.logfile.Close();

                if (comPort.rawlogfile != null)
                    comPort.rawlogfile.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error closing log files (Out of disk space?)\n" + ex.Message, "Error");
            }

            comPort.logfile = null;
            comPort.rawlogfile = null;

            // decide if this is a connect or disconnect
            if (comPort.BaseStream.IsOpen)
            {
                log.Info("We are disconnecting");
                try
                {
                    if (speechEngine != null) // cancel all pending speech
                        speechEngine.SpeakAsyncCancelAll();

                    comPort.BaseStream.DtrEnable = false;
                    comPort.Close();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }

                // now that we have closed the connection, cancel the connection stats
                // so that the 'time connected' etc does not grow, but the user can still
                // look at the now frozen stats on the still open form
                try
                {
                    //// if terminal is used, then closed using this button.... exception
                    //if (this.connectionStatsForm != null)
                    //    ((ConnectionStats)this.connectionStatsForm.Controls[0]).StopUpdates();
                }
                catch { }

                //// refresh config window if needed
                //if (MyView.current != null)
                //{
                //    if (MyView.current.Name == "HWConfig")
                //        MyView.ShowScreen("HWConfig");
                //    if (MyView.current.Name == "SWConfig")
                //        MyView.ShowScreen("SWConfig");
                //}

                this.MenuConnect.Image = global::MissionPlanner.Properties.Resources.light_connect_icon;
            }
            else
            {
                log.Info("We are connecting");
                comPort.BaseStream = new SerialPort();
                //comPort.BaseStream = new TcpSerial();
                //comPort.BaseStream = new UdpSerial();

                // Tell the connection UI that we are now connected.
                //_connectionControl.IsConnected(true);

                // Here we want to reset the connection stats counter etc.
                this.ResetConnectionStats();

                comPort.MAV.cs.ResetInternals();

                //cleanup any log being played
                comPort.logreadmode = false;
                if (comPort.logplaybackfile != null)
                    comPort.logplaybackfile.Close();
                comPort.logplaybackfile = null;

                bool autoscan = false;
                string portName = "COM10";
                string baudRate = "115200";
                try
                {
                    // do autoscan
                    if (autoscan)
                    {
                        Comms.CommsSerialScan.Scan(false);

                        DateTime deadline = DateTime.Now.AddSeconds(50);

                        while (Comms.CommsSerialScan.foundport == false)
                        {
                            System.Threading.Thread.Sleep(100);

                            if (DateTime.Now > deadline)
                            {
                                CustomMessageBox.Show("Timeout waiting for autoscan/no mavlink device connected");
                                //_connectionControl.IsConnected(false);
                                return;
                            }
                        }

                        portName = Comms.CommsSerialScan.portinterface.PortName;
                        baudRate = Comms.CommsSerialScan.portinterface.BaudRate.ToString();
                    }

                    log.Info("Set Portname");
                    // set port, then options
                    comPort.BaseStream.PortName = portName;

                    log.Info("Set Baudrate");
                    try
                    {
                        comPort.BaseStream.BaudRate = int.Parse(baudRate);
                    }
                    catch (Exception exp)
                    {
                        log.Error(exp);
                    }

                    // prevent serialreader from doing anything
                    comPort.giveComport = true;

                    log.Info("About to do dtr if needed");
                    //// reset on connect logic.
                    //if (config["CHK_resetapmonconnect"] == null || bool.Parse(config["CHK_resetapmonconnect"].ToString()) == true)
                    //{
                    //    log.Info("set dtr rts to false");
                    //    comPort.BaseStream.DtrEnable = false;
                    //    comPort.BaseStream.RtsEnable = false;

                    //    comPort.BaseStream.toggleDTR();
                    //}

                    comPort.giveComport = false;

                    // reset connect time - for timeout functions
                    connecttime = DateTime.Now;

                    // do the connect
                    comPort.Open(false);

                    if (!comPort.BaseStream.IsOpen)
                    {
                        log.Info("comport is closed. existing connect");
                        try
                        {
                            //_connectionControl.IsConnected(false);
                            UpdateConnectIcon();
                            comPort.Close();
                        }
                        catch { }
                        return;
                    }

                    //// detect firmware we are conected to.
                    //if (comPort.MAV.cs.firmware == Firmwares.ArduCopter2)
                    //{
                    //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduCopter2);
                    //}
                    //else if (comPort.MAV.cs.firmware == Firmwares.Ateryx)
                    //{
                    //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.Ateryx);
                    //}
                    //else if (comPort.MAV.cs.firmware == Firmwares.ArduRover)
                    //{
                    //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduRover);
                    //}
                    //else if (comPort.MAV.cs.firmware == Firmwares.ArduPlane)
                    //{
                    //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduPlane);
                    //}

                    // check for newer firmware
                    var softwares = Firmware.LoadSoftwares();

                    if (softwares.Count > 0)
                    {
                    }

                    MissionPlanner.Utilities.Tracking.AddEvent("Connect", "Connect", comPort.MAV.cs.firmware.ToString(), comPort.MAV.param.Count.ToString());
                    MissionPlanner.Utilities.Tracking.AddTiming("Connect", "Connect Time", (DateTime.Now - connecttime).TotalMilliseconds, "");

                    MissionPlanner.Utilities.Tracking.AddEvent("Connect", "Baud", comPort.BaseStream.BaudRate.ToString(), "");

                    //// save the baudrate for this port
                    //config[_connectionControl.CMB_serialport.Text + "_BAUD"] = _connectionControl.CMB_baudrate.Text;

                    //// refresh config window if needed
                    //if (MyView.current != null)
                    //{
                    //    if (MyView.current.Name == "HWConfig")
                    //        MyView.ShowScreen("HWConfig");
                    //    if (MyView.current.Name == "SWConfig")
                    //        MyView.ShowScreen("SWConfig");
                    //}


                    //// load wps on connect option.
                    //if (config["loadwpsonconnect"] != null && bool.Parse(config["loadwpsonconnect"].ToString()) == true)
                    //{
                    //    // only do it if we are connected.
                    //    if (comPort.BaseStream.IsOpen)
                    //    {
                    //        MenuFlightPlanner_Click(null, null);
                    //        FlightPlanner.BUT_read_Click(null, null);
                    //    }
                    //}

                    //// set connected icon
                    //this.MenuConnect.Image = diplayicons.disconnect;
                }
                catch (Exception ex)
                {
                    log.Warn(ex);
                    try
                    {
                        //_connectionControl.IsConnected(false);
                        UpdateConnectIcon();
                        comPort.Close();
                    }
                    catch { }
                    CustomMessageBox.Show("Can not establish a connection\n\n" + ex.Message);
                    return;
                }
            }
        }
        private void ResetConnectionStats()
        {
            log.Info("Reset connection stats");
        }
        /// <summary>
        /// Used to fix the icon status for unexpected unplugs etc...
        /// </summary>
        private void UpdateConnectIcon()
        {
        }


        Thread serialreaderthread;
        Thread thisthread;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // setup main serial reader
            serialreaderthread = new Thread(SerialReader)
            {
                IsBackground = true,
                Name = "Main Serial reader",
                Priority = ThreadPriority.AboveNormal
            };
            serialreaderthread.Start();

            thisthread = new Thread(mainloop);
            thisthread.Name = "FD Mainloop";
            thisthread.IsBackground = true;
            thisthread.Start();
        }

        public static bool threadrun = false;

        private void mainloop()
        {
            threadrun = true;
            EndPoint Remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));

            DateTime lastdata = DateTime.MinValue;

            DateTime tracklast = DateTime.Now.AddSeconds(0);

            DateTime tunning = DateTime.Now.AddSeconds(0);

            DateTime mapupdate = DateTime.Now.AddSeconds(0);

            DateTime vidrec = DateTime.Now.AddSeconds(0);

            DateTime waypoints = DateTime.Now.AddSeconds(0);

            DateTime updatescreen = DateTime.Now;

            DateTime tsreal = DateTime.Now;
            double taketime = 0;
            double timeerror = 0;

            //comPort.stopall(true);

            while (threadrun)
            {
                if (comPort.giveComport == true)
                {
                    System.Threading.Thread.Sleep(50);
                    continue;
                }
                try
                {
                    if (!comPort.BaseStream.IsOpen)
                        lastdata = DateTime.Now;
                }
                catch { }
                // re-request servo data
                if (!(lastdata.AddSeconds(8) > DateTime.Now) && comPort.BaseStream.IsOpen)
                {
                    //Console.WriteLine("REQ streams - flightdata");
                    try
                    {
                        //System.Threading.Thread.Sleep(1000);

                        //comPort.requestDatastream((byte)MissionPlanner.MAVLink09.MAV_DATA_STREAM.RAW_CONTROLLER, 0); // request servoout
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, comPort.MAV.cs.ratestatus); // mode
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.POSITION, comPort.MAV.cs.rateposition); // request gps
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA1, comPort.MAV.cs.rateattitude); // request attitude
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA2, comPort.MAV.cs.rateattitude); // request vfr
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA3, comPort.MAV.cs.ratesensors); // request extra stuff - tridge
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RAW_SENSORS, comPort.MAV.cs.ratesensors); // request raw sensor
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RC_CHANNELS, comPort.MAV.cs.raterc); // request rc info
                    }
                    catch { log.Error("Failed to request rates"); }
                    lastdata = DateTime.Now.AddSeconds(60); // prevent flooding
                }

                if (!comPort.logreadmode)
                    System.Threading.Thread.Sleep(50); // max is only ever 10 hz but we go a little faster to empty the serial queue

                // ensure we know to stop
                if (comPort.logreadmode)
                    comPort.logreadmode = false;
                //updatePlayPauseButton(false);


                try
                {
                    //Console.WriteLine(DateTime.Now.Millisecond);
                    //int fixme;
                    //updateBindingSource();
                    // Console.WriteLine(DateTime.Now.Millisecond + " done ");

                    // update opengltest
                    //if (OpenGLtest.instance != null)
                    //{
                    //    OpenGLtest.instance.rpy = new OpenTK.Vector3(comPort.MAV.cs.roll, comPort.MAV.cs.pitch, comPort.MAV.cs.yaw);
                    //    OpenGLtest.instance.LocationCenter = new PointLatLngAlt(comPort.MAV.cs.lat, comPort.MAV.cs.lng, comPort.MAV.cs.alt, "here");
                    //}

                    //// update opengltest2
                    //if (OpenGLtest2.instance != null)
                    //{
                    //    OpenGLtest2.instance.rpy = new OpenTK.Vector3(comPort.MAV.cs.roll, comPort.MAV.cs.pitch, comPort.MAV.cs.yaw);
                    //    OpenGLtest2.instance.LocationCenter = new PointLatLngAlt(comPort.MAV.cs.lat, comPort.MAV.cs.lng, comPort.MAV.cs.alt, "here");
                    //}

                    // update vario info
                    MissionPlanner.Utilities.Vario.SetValue(comPort.MAV.cs.climbrate);

                    CurrentState state = comPort.MAV.cs;
                    UpdateUI(state);
                }
                catch (Exception ex) { log.Error(ex); Console.WriteLine("FD Main loop exception " + ex.ToString()); }
            }
            Console.WriteLine("FD Main loop exit");
        }

        delegate void UpdateUICallback(CurrentState state);
        void UpdateUI(CurrentState state)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new UpdateUICallback(UpdateUI), state);
                return;
            }
            artificialHorizons1.PitchAngle = state.pitch;
            artificialHorizons1.RollAngle = state.roll;
        }

        /// <summary>
        /// controls the main serial reader thread
        /// </summary>
        bool serialThread = false;
        /// <summary>
        /// track the last heartbeat sent
        /// </summary>
        private DateTime heatbeatSend = DateTime.Now;
        /// <summary>
        /// passive comports
        /// </summary>
        public static List<MAVLinkInterface> Comports = new List<MAVLinkInterface>();

        ManualResetEvent SerialThreadrunner = new ManualResetEvent(false);

        /// <summary>
        /// main serial reader thread
        /// controls
        /// serial reading
        /// link quality stats
        /// speech voltage - custom - alt warning - data lost
        /// heartbeat packet sending
        /// 
        /// and can't fall out
        /// </summary>
        private void SerialReader()
        {
            if (serialThread == true)
                return;
            serialThread = true;

            SerialThreadrunner.Reset();

            int minbytes = 0;

            int altwarningmax = 0;

            bool armedstatus = false;

            string lastmessagehigh = "";

            DateTime speechcustomtime = DateTime.Now;

            DateTime speechbatterytime = DateTime.Now;
            DateTime speechlowspeedtime = DateTime.Now;

            DateTime linkqualitytime = DateTime.Now;

            while (serialThread)
            {
                try
                {
                    Thread.Sleep(1); // was 5

                    // update connect/disconnect button and info stats
                    UpdateConnectIcon();

                    // attenuate the link qualty over time
                    if ((DateTime.Now - comPort.lastvalidpacket).TotalSeconds >= 1)
                    {
                        if (linkqualitytime.Second != DateTime.Now.Second)
                        {
                            comPort.MAV.cs.linkqualitygcs = (ushort)(comPort.MAV.cs.linkqualitygcs * 0.8f);
                            linkqualitytime = DateTime.Now;
                        }
                    }

                    // data loss warning - wait min of 10 seconds, ignore first 30 seconds of connect, repeat at 5 seconds interval
                    if ((DateTime.Now - comPort.lastvalidpacket).TotalSeconds > 10
                        && (DateTime.Now - connecttime).TotalSeconds > 30
                        && (DateTime.Now - nodatawarning).TotalSeconds > 5
                        && (comPort.logreadmode || comPort.BaseStream.IsOpen)
                        && comPort.MAV.cs.armed)
                    {
                    }

                    // get home point on armed status change.
                    if (armedstatus != comPort.MAV.cs.armed && comPort.BaseStream.IsOpen)
                    {
                        armedstatus = comPort.MAV.cs.armed;
                        // status just changed to armed
                        if (comPort.MAV.cs.armed == true)
                        {
                            try
                            {
                                comPort.MAV.cs.HomeLocation = new PointLatLngAlt(comPort.getWP(0));
                                //if (MyView.current != null && MyView.current.Name == "FlightPlanner")
                                //{
                                //    // update home if we are on flight data tab
                                //    FlightPlanner.updateHome();
                                //}
                            }
                            catch
                            {
                                // dont hang this loop
                                this.BeginInvoke((MethodInvoker)delegate { CustomMessageBox.Show("Failed to update home location"); });
                            }
                        }
                    }

                    // send a hb every seconds from gcs to ap
                    if (heatbeatSend.Second != DateTime.Now.Second)
                    {
                        MAVLink.mavlink_heartbeat_t htb = new MAVLink.mavlink_heartbeat_t()
                        {
                            type = (byte)MAVLink.MAV_TYPE.GCS,
                            autopilot = (byte)MAVLink.MAV_AUTOPILOT.INVALID,
                            mavlink_version = 3,
                        };

                        comPort.sendPacket(htb);

                        foreach (var port in Comports)
                        {
                            if (port == comPort)
                                continue;
                            try
                            {
                                port.sendPacket(htb);
                            }
                            catch { }
                        }

                        heatbeatSend = DateTime.Now;
                    }

                    // if not connected or busy, sleep and loop
                    if (!comPort.BaseStream.IsOpen || comPort.giveComport == true)
                    {
                        if (!comPort.BaseStream.IsOpen)
                        {
                            // check if other ports are still open
                            foreach (var port in Comports)
                            {
                                if (port.BaseStream.IsOpen)
                                {
                                    Console.WriteLine("Main comport shut, swapping to other mav");
                                    comPort = port;
                                    break;
                                }
                            }
                        }

                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    // actualy read the packets
                    while (comPort.BaseStream.IsOpen && comPort.BaseStream.BytesToRead > minbytes && comPort.giveComport == false)
                    {
                        try
                        {
                            comPort.readPacket();
                        }
                        catch { }
                    }

                    // update currentstate of main port
                    try
                    {
                        comPort.MAV.cs.UpdateCurrentSettings(null, false, comPort);
                    }
                    catch { }

                    // read the other interfaces
                    foreach (var port in Comports)
                    {
                        if (!port.BaseStream.IsOpen)
                        {
                            // modify array and drop out
                            Comports.Remove(port);
                            break;
                        }
                        // skip primary interface
                        if (port == comPort)
                            continue;
                        while (port.BaseStream.IsOpen && port.BaseStream.BytesToRead > minbytes)
                        {
                            try
                            {
                                port.readPacket();
                            }
                            catch { }
                        }
                        // update currentstate of port
                        try
                        {
                            port.MAV.cs.UpdateCurrentSettings(null, false, port);
                        }
                        catch { }
                    }
                }
                catch (Exception e)
                {
                    log.Error("Serial Reader fail :" + e.ToString());
                    try
                    {
                        comPort.Close();
                    }
                    catch { }
                }
            }

            Console.WriteLine("SerialReader Done");
            SerialThreadrunner.Set();
        }
    }

    /// <summary>
    /// enum of firmwares
    /// </summary>
    public enum Firmwares
    {
        ArduPlane,
        ArduCopter2,
        //ArduHeli,
        ArduRover,
        Ateryx,
        ArduTracker
    }
}
