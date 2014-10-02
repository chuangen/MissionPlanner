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
            log.Info("closing fd");
            try
            {
                aircraft.Disconnect();
            }
            catch { }

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
                aircraft.Connect("COM10", "115200");
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
    }
}
