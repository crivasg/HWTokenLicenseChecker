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
            this.evDataGridView = new System.Windows.Forms.DataGridView();
            this.btn_Dismiss_EnvVarForm = new System.Windows.Forms.Button();
            this.VariableTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValueTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.evDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.evDataGridView);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(381, 154);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Environment Variables for HWTokenLicenseChecker";
            // 
            // evDataGridView
            // 
            this.evDataGridView.AllowUserToAddRows = false;
            this.evDataGridView.AllowUserToDeleteRows = false;
            this.evDataGridView.AllowUserToResizeRows = false;
            this.evDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.evDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.VariableTextBoxColumn,
            this.ValueTextBoxColumn});
            this.evDataGridView.Location = new System.Drawing.Point(7, 19);
            this.evDataGridView.Name = "evDataGridView";
            this.evDataGridView.ReadOnly = true;
            this.evDataGridView.RowHeadersVisible = false;
            this.evDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.evDataGridView.Size = new System.Drawing.Size(368, 129);
            this.evDataGridView.TabIndex = 0;
            this.evDataGridView.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.evDataGridView_CellContentDoubleClick);
            // 
            // btn_Dismiss_EnvVarForm
            // 
            this.btn_Dismiss_EnvVarForm.Location = new System.Drawing.Point(319, 173);
            this.btn_Dismiss_EnvVarForm.Name = "btn_Dismiss_EnvVarForm";
            this.btn_Dismiss_EnvVarForm.Size = new System.Drawing.Size(75, 23);
            this.btn_Dismiss_EnvVarForm.TabIndex = 1;
            this.btn_Dismiss_EnvVarForm.Text = "Dismiss";
            this.btn_Dismiss_EnvVarForm.UseVisualStyleBackColor = true;
            this.btn_Dismiss_EnvVarForm.Click += new System.EventHandler(this.btn_Dismiss_EnvVarForm_Click);
            // 
            // VariableTextBoxColumn
            // 
            this.VariableTextBoxColumn.FillWeight = 150F;
            this.VariableTextBoxColumn.HeaderText = "Variable";
            this.VariableTextBoxColumn.Name = "VariableTextBoxColumn";
            this.VariableTextBoxColumn.ReadOnly = true;
            this.VariableTextBoxColumn.Width = 150;
            // 
            // ValueTextBoxColumn
            // 
            this.ValueTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ValueTextBoxColumn.HeaderText = "Value";
            this.ValueTextBoxColumn.Name = "ValueTextBoxColumn";
            this.ValueTextBoxColumn.ReadOnly = true;
            // 
            // EnvironmentVariablesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 207);
            this.Controls.Add(this.btn_Dismiss_EnvVarForm);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "EnvironmentVariablesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Environment Variables";
            this.Load += new System.EventHandler(this.EnvironmentVariablesForm_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.evDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btn_Dismiss_EnvVarForm;
        private System.Windows.Forms.DataGridView evDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn VariableTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValueTextBoxColumn;
    }
}