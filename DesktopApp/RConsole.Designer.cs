namespace DesktopApp
{
    partial class RConsole
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.consoleFeed = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // consoleFeed
            // 
            this.consoleFeed.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.consoleFeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.consoleFeed.Location = new System.Drawing.Point(3, 0);
            this.consoleFeed.Multiline = true;
            this.consoleFeed.Name = "consoleFeed";
            this.consoleFeed.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.consoleFeed.Size = new System.Drawing.Size(608, 84);
            this.consoleFeed.TabIndex = 0;
            this.consoleFeed.MouseClick += new System.Windows.Forms.MouseEventHandler(this.consoleFeed_MouseClick);
            this.consoleFeed.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.consoleFeed_KeyPress);
            // 
            // RConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.consoleFeed);
            this.Name = "RConsole";
            this.Size = new System.Drawing.Size(608, 84);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox consoleFeed;
    }
}
