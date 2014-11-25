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
        /// used to call anything as needed.
        /// </summary>
        public static MainV2 instance = null;

        readonly Aircraft aircraft = Aircraft.Default;

        Thread joystickthread;
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

            aircraft.DataReceived += aircraft_DataReceived;
            aircraft.ConnectStateChanged += aircraft_ConnectStateChanged;

            checkBox1.CheckedChanged += delegate(object sender, EventArgs e)
            {
                joystickEnabled = checkBox1.Checked;
            };
            trackBarYaw.ValueChanged += trackBar2_ValueChanged;
            trackBarRoll.ValueChanged += trackBar2_ValueChanged;
            trackBarPitch.ValueChanged += trackBar2_ValueChanged;
            trackBarThrottle.ValueChanged += trackBar2_ValueChanged;

            cmbPortName.DropDown += cmbPortName_DropDown;
            cmbPortName_DropDown(this, EventArgs.Empty);
            cmbBaudRate.Items.AddRange(new object[]{
                57600,
                115200,
            });
            cmbBaudRate.SelectedItem = 57600;
            cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;

            btnArm.Click += btnArm_Click;
        }

        void btnArm_Click(object sender, EventArgs e)
        {
            if (!aircraft.Link.BaseStream.IsOpen)
                return;

            byte MAV_MODE_FLAG_SAFETY_ARMED = 128;
            // arm the MAV
            try
            {
                //SENDING REQUEST TO SET MODE TO SYSTEM 1 , MODE  209   65536
                //SENDING REQUEST TO SET MODE TO SYSTEM 1 , MODE  29   100925440
                if (aircraft.Link.MAV.cs.armed)
                {
                    aircraft.Link.setMode(new MAVLink.mavlink_set_mode_t
                    {
                        target_system = aircraft.Link.MAV.sysid,
                        base_mode = (byte)(aircraft.Current.base_mode & ~MAV_MODE_FLAG_SAFETY_ARMED),
                        custom_mode = aircraft.Current._mode,
                    });
                }
                else
                {
                    aircraft.Link.setMode(new MAVLink.mavlink_set_mode_t
                    {
                        target_system = aircraft.Link.MAV.sysid,
                        base_mode = (byte)(aircraft.Current.base_mode | MAV_MODE_FLAG_SAFETY_ARMED),
                        custom_mode = aircraft.Current._mode,
                    });
                }


                //bool ans = aircraft.Link.doARM(!aircraft.Link.MAV.cs.armed);
                //if (ans == false)
                //    CustomMessageBox.Show("Error: Arm message rejected by MAV", "Error");
            }
            catch { CustomMessageBox.Show("Error: No response from MAV", "Error"); }
        }

        void cmbPortName_DropDown(object sender, EventArgs e)
        {
            cmbPortName.Items.Clear();
            cmbPortName.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
        }

        volatile bool joystickEnabled = false;

        MAVLink.mavlink_manual_control_t rc11 = new MAVLink.mavlink_manual_control_t();
        void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            rc11.x = (Int16)trackBarYaw.Value;
            rc11.y = (Int16)trackBarPitch.Value;
            rc11.z = (Int16)trackBarThrottle.Value;
            rc11.r = (Int16)trackBarRoll.Value; 
        }

        void aircraft_ConnectStateChanged(object sender, EventArgs e)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new EventHandler(aircraft_ConnectStateChanged), sender, e);
                return;
            }
            //if(aircraft.IsConnected)
            if(aircraft.Link.BaseStream.IsOpen)
            {
                MenuConnect.Text = "断开";
            }
            else
            {
                MenuConnect.Text = "连接";
            }
            UpdateConnectIcon();
        }

        void aircraft_DataReceived(object sender, DataReceivedEventArgs e)
        {
            UpdateUI(e.Data);
        }

        protected override void OnClosing(CancelEventArgs e)
        {

            log.Info("closing joystickthread");

            joystickthreadrun = false;

            joystickthread.Join();

            log.Info("closing fd");
            try
            {
                aircraft.Disconnect();
            }
            catch { }

            Console.WriteLine(joystickthread.IsAlive);
            log.Info("MainV2_FormClosing done");


            base.OnClosing(e);
        }

        private void MenuConnect_Click(object sender, EventArgs e)
        {
            if (aircraft.Link.BaseStream.IsOpen)
            {
                aircraft.Disconnect();
            }
            else
            {
                aircraft.Connect(cmbPortName.SelectedItem as string, ((int)cmbBaudRate.SelectedItem).ToString());
            }
        }
        /// <summary>
        /// Used to fix the icon status for unexpected unplugs etc...
        /// </summary>
        private void UpdateConnectIcon()
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            /// setup joystick packet sender
            joystickthread = new Thread(new ThreadStart(joysticksend))
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "Main joystick sender"
            };
            joystickthread.Start();
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
        /// track last joystick packet sent. used to control rate
        /// </summary>
        DateTime lastjoystick = DateTime.Now;

        bool joystickthreadrun = false;
        /// <summary>
        /// needs to be true by default so that exits properly if no joystick used.
        /// </summary>
        volatile private bool joysendThreadExited = true;

        /// <summary>
        /// thread used to send joystick packets to the MAV
        /// </summary>
        private void joysticksend()
        {
            float rate = 50; // 1000 / 50 = 20 hz
            int count = 0;

            DateTime lastratechange = DateTime.Now;

            joystickthreadrun = true;

            while (joystickthreadrun)
            {
                joysendThreadExited = false;
                //so we know this thread is stil alive.           
                try
                {
                    if (MONO)
                    {
                        log.Error("Mono: closing joystick thread");
                        break;
                    }

                    if (!MONO)
                    {
                        //joystick stuff
                        if (joystickEnabled)
                        {
                            //MAVLink.mavlink_rc_channels_override_t rc = new MAVLink.mavlink_rc_channels_override_t();

                            //rc.target_component = aircraft.Link.MAV.compid;
                            //rc.target_system = aircraft.Link.MAV.sysid;

                            //rc.chan1_raw = aircraft.Current.rcoverridech1;
                            //rc.chan2_raw = aircraft.Current.rcoverridech2;
                            //rc.chan3_raw = aircraft.Current.rcoverridech3;
                            //rc.chan4_raw = aircraft.Current.rcoverridech4;
                            //rc.chan5_raw = aircraft.Current.rcoverridech5;
                            //rc.chan6_raw = aircraft.Current.rcoverridech6;
                            //rc.chan7_raw = aircraft.Current.rcoverridech7;
                            //rc.chan8_raw = aircraft.Current.rcoverridech8;


                            MAVLink.mavlink_manual_control_t rc = new MAVLink.mavlink_manual_control_t();

                            rc.target = aircraft.Link.MAV.sysid;

                            rc.x = rc11.x;
                            rc.y = rc11.y;
                            rc.z = rc11.z;
                            rc.r = rc11.r;

                            if (lastjoystick.AddMilliseconds(rate) < DateTime.Now)
                            {
                                /*
                                if (aircraft.Current.rssi > 0 && aircraft.Current.remrssi > 0)
                                {
                                    if (lastratechange.Second != DateTime.Now.Second)
                                    {
                                        if (aircraft.Current.txbuffer > 90)
                                        {
                                            if (rate < 20)
                                                rate = 21;
                                            rate--;

                                            if (aircraft.Current.linkqualitygcs < 70)
                                                rate = 50;
                                        }
                                        else
                                        {
                                            if (rate > 100)
                                                rate = 100;
                                            rate++;
                                        }

                                        lastratechange = DateTime.Now;
                                    }
                                 
                                }
                                */
                                //                                Console.WriteLine(DateTime.Now.Millisecond + " {0} {1} {2} {3} {4}", rc.chan1_raw, rc.chan2_raw, rc.chan3_raw, rc.chan4_raw,rate);

                                //Console.WriteLine("Joystick btw " + comPort.BaseStream.BytesToWrite);

                                if (!aircraft.Link.BaseStream.IsOpen)
                                    continue;

                                if (aircraft.Link.BaseStream.BytesToWrite < 50)
                                {
                                    aircraft.Link.sendPacket(rc);
                                    count++;
                                    lastjoystick = DateTime.Now;
                                }
                            }

                        }
                    }
                    Thread.Sleep(20);
                }
                catch
                {

                } // cant fall out
            }
            joysendThreadExited = true;//so we know this thread exited.    
        }
    }
}
