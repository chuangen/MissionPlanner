namespace MissionPlanner
{
    partial class MainV2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MenuConnect = new System.Windows.Forms.Button();
            this.artificialHorizons1 = new FCG.AircraftInstrument.ArtificialHorizons();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.trackBarYaw = new System.Windows.Forms.TrackBar();
            this.trackBarRoll = new System.Windows.Forms.TrackBar();
            this.trackBarPitch = new System.Windows.Forms.TrackBar();
            this.trackBarThrottle = new System.Windows.Forms.TrackBar();
            this.cmbPortName = new System.Windows.Forms.ComboBox();
            this.cmbBaudRate = new System.Windows.Forms.ComboBox();
            this.btnArm = new System.Windows.Forms.Button();
            this.btnDisarm = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarYaw)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarRoll)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPitch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThrottle)).BeginInit();
            this.SuspendLayout();
            // 
            // MenuConnect
            // 
            this.MenuConnect.Location = new System.Drawing.Point(496, 42);
            this.MenuConnect.Name = "MenuConnect";
            this.MenuConnect.Size = new System.Drawing.Size(69, 39);
            this.MenuConnect.TabIndex = 0;
            this.MenuConnect.Text = "连接";
            this.MenuConnect.UseVisualStyleBackColor = true;
            // 
            // artificialHorizons1
            // 
            this.artificialHorizons1.Direction = 0D;
            this.artificialHorizons1.Location = new System.Drawing.Point(12, 12);
            this.artificialHorizons1.Name = "artificialHorizons1";
            this.artificialHorizons1.PitchAngle = 0D;
            this.artificialHorizons1.PitchRadian = 0D;
            this.artificialHorizons1.RollAngle = 0D;
            this.artificialHorizons1.RollRadian = 0D;
            this.artificialHorizons1.ShowStyle = FCG.AircraftInstrument.ArtificialHorizons.ShowStyleEnum.horizon_GA_elec_flag_adj;
            this.artificialHorizons1.Size = new System.Drawing.Size(168, 168);
            this.artificialHorizons1.TabIndex = 8;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(223, 141);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(96, 16);
            this.checkBox1.TabIndex = 9;
            this.checkBox1.Text = "启用虚拟摇杆";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // trackBarYaw
            // 
            this.trackBarYaw.Location = new System.Drawing.Point(40, 410);
            this.trackBarYaw.Maximum = 1000;
            this.trackBarYaw.Minimum = -1000;
            this.trackBarYaw.Name = "trackBarYaw";
            this.trackBarYaw.Size = new System.Drawing.Size(321, 45);
            this.trackBarYaw.TabIndex = 10;
            this.trackBarYaw.TickStyle = System.Windows.Forms.TickStyle.Both;
            // 
            // trackBarRoll
            // 
            this.trackBarRoll.Location = new System.Drawing.Point(392, 410);
            this.trackBarRoll.Maximum = 1000;
            this.trackBarRoll.Minimum = -1000;
            this.trackBarRoll.Name = "trackBarRoll";
            this.trackBarRoll.Size = new System.Drawing.Size(321, 45);
            this.trackBarRoll.TabIndex = 11;
            this.trackBarRoll.TickStyle = System.Windows.Forms.TickStyle.Both;
            // 
            // trackBarPitch
            // 
            this.trackBarPitch.Location = new System.Drawing.Point(181, 201);
            this.trackBarPitch.Maximum = 1000;
            this.trackBarPitch.Minimum = -1000;
            this.trackBarPitch.Name = "trackBarPitch";
            this.trackBarPitch.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarPitch.Size = new System.Drawing.Size(45, 203);
            this.trackBarPitch.TabIndex = 12;
            this.trackBarPitch.TickStyle = System.Windows.Forms.TickStyle.Both;
            // 
            // trackBarTh
            // 
            this.trackBarThrottle.Location = new System.Drawing.Point(532, 201);
            this.trackBarThrottle.Maximum = 1000;
            this.trackBarThrottle.Minimum = -1000;
            this.trackBarThrottle.Name = "trackBarThrottle";
            this.trackBarThrottle.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarThrottle.Size = new System.Drawing.Size(45, 203);
            this.trackBarThrottle.TabIndex = 13;
            this.trackBarThrottle.TickStyle = System.Windows.Forms.TickStyle.Both;
            // 
            // cmbPortName
            // 
            this.cmbPortName.FormattingEnabled = true;
            this.cmbPortName.Location = new System.Drawing.Point(223, 52);
            this.cmbPortName.Name = "cmbPortName";
            this.cmbPortName.Size = new System.Drawing.Size(121, 20);
            this.cmbPortName.TabIndex = 14;
            // 
            // cmbBaudRate
            // 
            this.cmbBaudRate.FormattingEnabled = true;
            this.cmbBaudRate.Location = new System.Drawing.Point(350, 52);
            this.cmbBaudRate.Name = "cmbBaudRate";
            this.cmbBaudRate.Size = new System.Drawing.Size(121, 20);
            this.cmbBaudRate.TabIndex = 15;
            // 
            // btnArm
            // 
            this.btnArm.Location = new System.Drawing.Point(396, 116);
            this.btnArm.Name = "btnArm";
            this.btnArm.Size = new System.Drawing.Size(75, 23);
            this.btnArm.TabIndex = 16;
            this.btnArm.Text = "准备";
            this.btnArm.UseVisualStyleBackColor = true;
            // 
            // btnDisarm
            // 
            this.btnDisarm.Location = new System.Drawing.Point(477, 116);
            this.btnDisarm.Name = "btnDisarm";
            this.btnDisarm.Size = new System.Drawing.Size(75, 23);
            this.btnDisarm.TabIndex = 17;
            this.btnDisarm.Text = "解除准备";
            this.btnDisarm.UseVisualStyleBackColor = true;
            // 
            // MainV2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 480);
            this.Controls.Add(this.btnDisarm);
            this.Controls.Add(this.btnArm);
            this.Controls.Add(this.cmbBaudRate);
            this.Controls.Add(this.cmbPortName);
            this.Controls.Add(this.trackBarThrottle);
            this.Controls.Add(this.trackBarPitch);
            this.Controls.Add(this.trackBarRoll);
            this.Controls.Add(this.trackBarYaw);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.artificialHorizons1);
            this.Controls.Add(this.MenuConnect);
            this.Name = "MainV2";
            this.Text = "简单的地面站";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarYaw)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarRoll)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPitch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThrottle)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button MenuConnect;
        private FCG.AircraftInstrument.ArtificialHorizons artificialHorizons1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TrackBar trackBarYaw;
        private System.Windows.Forms.TrackBar trackBarRoll;
        private System.Windows.Forms.TrackBar trackBarPitch;
        private System.Windows.Forms.TrackBar trackBarThrottle;
        private System.Windows.Forms.ComboBox cmbPortName;
        private System.Windows.Forms.ComboBox cmbBaudRate;
        private System.Windows.Forms.Button btnArm;
        private System.Windows.Forms.Button btnDisarm;
    }
}