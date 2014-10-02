using log4net;
using MissionPlanner.Comms;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MissionPlanner
{
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

    public class Aircraft
    {
        static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        static Aircraft instance = null;
        public static Aircraft Default
        {
            get
            {
                if (instance == null)
                    instance = new Aircraft();

                return instance;
            }
        }

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
        /// Active Comport interface
        /// </summary>
        MAVLinkInterface comPort = new MAVLinkInterface();

        public MAVLinkInterface Link
        {
            get { return this.comPort; }
        }

        public CurrentState Current
        {
            get { return this.comPort.MAV.cs; }
        }

        Thread serialreaderthread;
        Thread thisthread;

        private Aircraft()
        {
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
            MAVLinkInterface comPort = Aircraft.Default.Link;

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
                    //UpdateConnectIcon();

                    // attenuate the link qualty over time
                    if ((DateTime.Now - comPort.lastvalidpacket).TotalSeconds >= 1)
                    {
                        if (linkqualitytime.Second != DateTime.Now.Second)
                        {
                            Aircraft.Default.Current.linkqualitygcs = (ushort)(Aircraft.Default.Current.linkqualitygcs * 0.8f);
                            linkqualitytime = DateTime.Now;
                        }
                    }

                    // data loss warning - wait min of 10 seconds, ignore first 30 seconds of connect, repeat at 5 seconds interval
                    if ((DateTime.Now - comPort.lastvalidpacket).TotalSeconds > 10
                        && (DateTime.Now - connecttime).TotalSeconds > 30
                        && (DateTime.Now - nodatawarning).TotalSeconds > 5
                        && (comPort.logreadmode || comPort.BaseStream.IsOpen)
                        && Aircraft.Default.Current.armed)
                    {
                    }

                    // get home point on armed status change.
                    if (armedstatus != Aircraft.Default.Current.armed && comPort.BaseStream.IsOpen)
                    {
                        armedstatus = Aircraft.Default.Current.armed;
                        // status just changed to armed
                        if (Aircraft.Default.Current.armed == true)
                        {
                            try
                            {
                                Aircraft.Default.Current.HomeLocation = new PointLatLngAlt(comPort.getWP(0));
                                //if (MyView.current != null && MyView.current.Name == "FlightPlanner")
                                //{
                                //    // update home if we are on flight data tab
                                //    FlightPlanner.updateHome();
                                //}
                            }
                            catch
                            {
                                // dont hang this loop
                                //this.BeginInvoke((MethodInvoker)delegate { CustomMessageBox.Show("Failed to update home location"); });
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
                        Aircraft.Default.Current.UpdateCurrentSettings(null, false, comPort);
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

        public static bool threadrun = false;

        private void mainloop()
        {
            MAVLinkInterface comPort = Aircraft.Default.Link;
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
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, Aircraft.Default.Current.ratestatus); // mode
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.POSITION, Aircraft.Default.Current.rateposition); // request gps
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA1, Aircraft.Default.Current.rateattitude); // request attitude
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA2, Aircraft.Default.Current.rateattitude); // request vfr
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA3, Aircraft.Default.Current.ratesensors); // request extra stuff - tridge
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RAW_SENSORS, Aircraft.Default.Current.ratesensors); // request raw sensor
                        comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RC_CHANNELS, Aircraft.Default.Current.raterc); // request rc info
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
                    //    OpenGLtest.instance.rpy = new OpenTK.Vector3(Aircraft.Default.Current.roll, Aircraft.Default.Current.pitch, Aircraft.Default.Current.yaw);
                    //    OpenGLtest.instance.LocationCenter = new PointLatLngAlt(Aircraft.Default.Current.lat, Aircraft.Default.Current.lng, Aircraft.Default.Current.alt, "here");
                    //}

                    //// update opengltest2
                    //if (OpenGLtest2.instance != null)
                    //{
                    //    OpenGLtest2.instance.rpy = new OpenTK.Vector3(Aircraft.Default.Current.roll, Aircraft.Default.Current.pitch, Aircraft.Default.Current.yaw);
                    //    OpenGLtest2.instance.LocationCenter = new PointLatLngAlt(Aircraft.Default.Current.lat, Aircraft.Default.Current.lng, Aircraft.Default.Current.alt, "here");
                    //}

                    // update vario info
                    MissionPlanner.Utilities.Vario.SetValue(Aircraft.Default.Current.climbrate);

                    CurrentState state = Aircraft.Default.Current;
                    if(this.DataReceived != null)
                    {
                        try
                        {
                            this.DataReceived(this, new DataReceivedEventArgs(Aircraft.Default.Current));
                        }
                        catch (Exception ex)
                        { }
                    }
                }
                catch (Exception ex) { log.Error(ex); Console.WriteLine("FD Main loop exception " + ex.ToString()); }
            }
            Console.WriteLine("FD Main loop exit");
        }

        public void Connect()
        {
            this.Connect(true, null, null);
        }
        public void Connect(string portName, string baudRate)
        {
            this.Connect(false, portName, baudRate);
        }
        protected void Connect(bool autoscan, string portName, string baudRate)
        {
            // decide if this is a connect or disconnect
            if (comPort.BaseStream.IsOpen)
                return;

            comPort.giveComport = false;
            log.Info("MenuConnect Start");
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

            log.Info("We are connecting");
            comPort.BaseStream = new SerialPort();
            //comPort.BaseStream = new TcpSerial();
            //comPort.BaseStream = new UdpSerial();

            // Tell the connection UI that we are now connected.
            this.IsConnected = true;

            // Here we want to reset the connection stats counter etc.
            this.ResetConnectionStats();

            comPort.MAV.cs.ResetInternals();

            //cleanup any log being played
            comPort.logreadmode = false;
            if (comPort.logplaybackfile != null)
                comPort.logplaybackfile.Close();
            comPort.logplaybackfile = null;

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
                            this.IsConnected = false;
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
                comPort.Open(true);

                if (!comPort.BaseStream.IsOpen)
                {
                    log.Info("comport is closed. existing connect");
                    try
                    {
                        this.IsConnected = false;
                        comPort.Close();
                    }
                    catch { }
                    return;
                }

                //// detect firmware we are conected to.
                //if (Aircraft.Default.Current.firmware == Firmwares.ArduCopter2)
                //{
                //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduCopter2);
                //}
                //else if (Aircraft.Default.Current.firmware == Firmwares.Ateryx)
                //{
                //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.Ateryx);
                //}
                //else if (Aircraft.Default.Current.firmware == Firmwares.ArduRover)
                //{
                //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduRover);
                //}
                //else if (Aircraft.Default.Current.firmware == Firmwares.ArduPlane)
                //{
                //    _connectionControl.TOOL_APMFirmware.SelectedIndex = _connectionControl.TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduPlane);
                //}

                // check for newer firmware
                var softwares = Firmware.LoadSoftwares();

                if (softwares.Count > 0)
                {
                }

                MissionPlanner.Utilities.Tracking.AddEvent("Connect", "Connect", Aircraft.Default.Current.firmware.ToString(), comPort.MAV.param.Count.ToString());
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
                    this.IsConnected = false;
                    comPort.Close();
                }
                catch { }
                CustomMessageBox.Show("Can not establish a connection\n\n" + ex.Message);
                return;
            }
        }

        private void ResetConnectionStats()
        {
            log.Info("Reset connection stats");
        }

        public void Disconnect()
        {
            if (!comPort.BaseStream.IsOpen)
                return;

            comPort.giveComport = false;

            log.Info("MenuConnect Start");

            // sanity check
            if (Aircraft.Default.Current.groundspeed > 4)
            {
                //if (DialogResult.No == CustomMessageBox.Show("Your model is still moving are you sure you want to disconnect?", "Disconnect", MessageBoxButtons.YesNo))
                //{
                //    return;
                //}
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

            //this.MenuConnect.Image = global::MissionPlanner.Properties.Resources.light_connect_icon;
        }

        bool isConnected = false;
        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                if (this.isConnected == value)
                    return;
                this.isConnected = value;
                if (this.ConnectStateChanged != null)
                    this.ConnectStateChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler ConnectStateChanged;
    }

    public class DataReceivedEventArgs :EventArgs
    {
        public CurrentState Data {get; private set; }
        public DataReceivedEventArgs(CurrentState state)
        {
            this.Data = state;
        }
    }
}
