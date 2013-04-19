namespace HWTokenLicenseChecker
{
    partial class EnvironmentVariablesForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_Dismiss_EnvVarForm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(381, 248);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Environment Variables for HWTokenLicenseChecker";
            // 
            // btn_Dismiss_EnvVarForm
            // 
            this.btn_Dismiss_EnvVarForm.Location = new System.Drawing.Point(318, 280);
            this.btn_Dismiss_EnvVarForm.Name = "btn_Dismiss_EnvVarForm";
            this.btn_Dismiss_EnvVarForm.Size = new System.Drawing.Size(75, 23);
            this.btn_Dismiss_EnvVarForm.TabIndex = 1;
            this.btn_Dismiss_EnvVarForm.Text = "Dismiss";
            this.btn_Dismiss_EnvVarForm.UseVisualStyleBackColor = true;
            // 
            // EnvironmentVariablesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 315);
            this.Controls.Add(this.btn_Dismiss_EnvVarForm);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "EnvironmentVariablesForm";
            this.Text = "Environment Variables";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btn_Dismiss_EnvVarForm;
    }
}