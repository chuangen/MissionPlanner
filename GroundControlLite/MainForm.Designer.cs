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
            this.SuspendLayout();
            // 
            // MenuConnect
            // 
            this.MenuConnect.Location = new System.Drawing.Point(223, 61);
            this.MenuConnect.Name = "MenuConnect";
            this.MenuConnect.Size = new System.Drawing.Size(129, 48);
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
            // MainV2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(483, 300);
            this.Controls.Add(this.artificialHorizons1);
            this.Controls.Add(this.MenuConnect);
            this.Name = "MainV2";
            this.Text = "简单的地面站";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button MenuConnect;
        private FCG.AircraftInstrument.ArtificialHorizons artificialHorizons1;
    }
}