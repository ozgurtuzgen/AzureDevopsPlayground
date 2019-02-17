namespace AzureDevopsClient
{
    partial class Form1
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
            this.executeUpdates = new System.Windows.Forms.Button();
            this.openFolder = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // executeUpdates
            // 
            this.executeUpdates.Enabled = false;
            this.executeUpdates.Location = new System.Drawing.Point(12, 23);
            this.executeUpdates.Name = "executeUpdates";
            this.executeUpdates.Size = new System.Drawing.Size(260, 23);
            this.executeUpdates.TabIndex = 0;
            this.executeUpdates.Text = "ExecuteUpdates";
            this.executeUpdates.UseVisualStyleBackColor = true;
            this.executeUpdates.Click += new System.EventHandler(this.executeUpdates_Click);
            // 
            // openFolder
            // 
            this.openFolder.Location = new System.Drawing.Point(12, 81);
            this.openFolder.Name = "openFolder";
            this.openFolder.Size = new System.Drawing.Size(260, 23);
            this.openFolder.TabIndex = 1;
            this.openFolder.Text = "Open App Folder";
            this.openFolder.UseVisualStyleBackColor = true;
            this.openFolder.Click += new System.EventHandler(this.openFolder_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 52);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(260, 23);
            this.progressBar1.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 126);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.openFolder);
            this.Controls.Add(this.executeUpdates);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button executeUpdates;
        private System.Windows.Forms.Button openFolder;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}

