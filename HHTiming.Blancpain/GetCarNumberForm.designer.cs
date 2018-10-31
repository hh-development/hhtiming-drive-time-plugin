namespace HHTiming.Blancpain
{
    partial class GetCarNumberForm
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
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.tb_CarNumber = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.btn_OK = new DevComponents.DotNetBar.ButtonX();
            this.btn_Cancel = new DevComponents.DotNetBar.ButtonX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(115, 36);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(62, 17);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "Car Number";
            // 
            // tb_CarNumber
            // 
            this.tb_CarNumber.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.tb_CarNumber.Border.Class = "TextBoxBorder";
            this.tb_CarNumber.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.tb_CarNumber.DisabledBackColor = System.Drawing.Color.White;
            this.tb_CarNumber.ForeColor = System.Drawing.Color.Black;
            this.tb_CarNumber.Location = new System.Drawing.Point(183, 35);
            this.tb_CarNumber.Name = "tb_CarNumber";
            this.tb_CarNumber.PreventEnterBeep = true;
            this.tb_CarNumber.Size = new System.Drawing.Size(57, 22);
            this.tb_CarNumber.TabIndex = 1;
            // 
            // btn_OK
            // 
            this.btn_OK.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btn_OK.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btn_OK.Location = new System.Drawing.Point(195, 83);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btn_OK.TabIndex = 2;
            this.btn_OK.Text = "OK";
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btn_Cancel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btn_Cancel.Location = new System.Drawing.Point(276, 83);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_Cancel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btn_Cancel.TabIndex = 3;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // labelX2
            // 
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelX2.Location = new System.Drawing.Point(0, 0);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(363, 29);
            this.labelX2.TabIndex = 4;
            this.labelX2.Text = "Please enter the car number for which you will be creating a strategy tool";
            // 
            // GetCarNumberForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 108);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.tb_CarNumber);
            this.Controls.Add(this.labelX1);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GetCarNumberForm";
            this.Text = "Enter a car number";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.TextBoxX tb_CarNumber;
        private DevComponents.DotNetBar.ButtonX btn_OK;
        private DevComponents.DotNetBar.ButtonX btn_Cancel;
        private DevComponents.DotNetBar.LabelX labelX2;
    }
}