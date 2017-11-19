using System.Windows.Forms;

namespace DesktopApp
{
    partial class VariablePane
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
            this.components = new System.ComponentModel.Container();
            this.fixedList = new System.Windows.Forms.ListBox();
            this.randomList = new System.Windows.Forms.ListBox();
            this.allVarList = new System.Windows.Forms.ListBox();
            this.fixedLbl = new System.Windows.Forms.Label();
            this.randomLbl = new System.Windows.Forms.Label();
            this.allVarsLbl = new System.Windows.Forms.Label();
            this.predictedVariableList = new System.Windows.Forms.Label();
            this.predictedLst = new System.Windows.Forms.ListBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addInteractionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeInteractionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addCovariateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.var1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.var2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeCovarianceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // fixedList
            // 
            this.fixedList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fixedList.FormattingEnabled = true;
            this.fixedList.Location = new System.Drawing.Point(5, 95);
            this.fixedList.Name = "fixedList";
            this.fixedList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.fixedList.Size = new System.Drawing.Size(161, 147);
            this.fixedList.TabIndex = 0;
            // 
            // randomList
            // 
            this.randomList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.randomList.FormattingEnabled = true;
            this.randomList.Location = new System.Drawing.Point(5, 273);
            this.randomList.Name = "randomList";
            this.randomList.Size = new System.Drawing.Size(161, 147);
            this.randomList.TabIndex = 1;
            // 
            // allVarList
            // 
            this.allVarList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.allVarList.FormattingEnabled = true;
            this.allVarList.Location = new System.Drawing.Point(5, 450);
            this.allVarList.Name = "allVarList";
            this.allVarList.Size = new System.Drawing.Size(161, 160);
            this.allVarList.TabIndex = 2;
            // 
            // fixedLbl
            // 
            this.fixedLbl.AutoSize = true;
            this.fixedLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.fixedLbl.Location = new System.Drawing.Point(8, 72);
            this.fixedLbl.Name = "fixedLbl";
            this.fixedLbl.Size = new System.Drawing.Size(106, 20);
            this.fixedLbl.TabIndex = 3;
            this.fixedLbl.Text = "Fixed Effects:";
            // 
            // randomLbl
            // 
            this.randomLbl.AutoSize = true;
            this.randomLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.randomLbl.Location = new System.Drawing.Point(8, 251);
            this.randomLbl.Name = "randomLbl";
            this.randomLbl.Size = new System.Drawing.Size(129, 20);
            this.randomLbl.TabIndex = 4;
            this.randomLbl.Text = "Random Effects:";
            // 
            // allVarsLbl
            // 
            this.allVarsLbl.AutoSize = true;
            this.allVarsLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.allVarsLbl.Location = new System.Drawing.Point(11, 429);
            this.allVarsLbl.Name = "allVarsLbl";
            this.allVarsLbl.Size = new System.Drawing.Size(100, 20);
            this.allVarsLbl.TabIndex = 5;
            this.allVarsLbl.Text = "All Variables:";
            // 
            // predictedVariableList
            // 
            this.predictedVariableList.AutoSize = true;
            this.predictedVariableList.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.predictedVariableList.Location = new System.Drawing.Point(8, 11);
            this.predictedVariableList.Name = "predictedVariableList";
            this.predictedVariableList.Size = new System.Drawing.Size(142, 20);
            this.predictedVariableList.TabIndex = 7;
            this.predictedVariableList.Text = "Predicted Variable:";
            // 
            // predictedLst
            // 
            this.predictedLst.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.predictedLst.FormattingEnabled = true;
            this.predictedLst.Location = new System.Drawing.Point(5, 34);
            this.predictedLst.Name = "predictedLst";
            this.predictedLst.Size = new System.Drawing.Size(161, 30);
            this.predictedLst.TabIndex = 6;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeToolStripMenuItem,
            this.addInteractionToolStripMenuItem,
            this.removeInteractionToolStripMenuItem,
            this.addCovariateToolStripMenuItem,
            this.removeCovarianceToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(180, 136);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.Remove);
            // 
            // addInteractionToolStripMenuItem
            // 
            this.addInteractionToolStripMenuItem.Name = "addInteractionToolStripMenuItem";
            this.addInteractionToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.addInteractionToolStripMenuItem.Text = "Add Interaction";
            this.addInteractionToolStripMenuItem.Click += new System.EventHandler(this.AddInteraction);
            // 
            // removeInteractionToolStripMenuItem
            // 
            this.removeInteractionToolStripMenuItem.Name = "removeInteractionToolStripMenuItem";
            this.removeInteractionToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.removeInteractionToolStripMenuItem.Text = "Remove Interaction";
            this.removeInteractionToolStripMenuItem.Click += new System.EventHandler(this.RemoveInteraction);
            // 
            // addCovariateToolStripMenuItem
            // 
            this.addCovariateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.var1ToolStripMenuItem,
            this.var2ToolStripMenuItem});
            this.addCovariateToolStripMenuItem.Name = "addCovariateToolStripMenuItem";
            this.addCovariateToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.addCovariateToolStripMenuItem.Text = "Add Covariate";
            // 
            // var1ToolStripMenuItem
            // 
            this.var1ToolStripMenuItem.Name = "var1ToolStripMenuItem";
            this.var1ToolStripMenuItem.Size = new System.Drawing.Size(97, 22);
            this.var1ToolStripMenuItem.Text = "Var1";
            // 
            // var2ToolStripMenuItem
            // 
            this.var2ToolStripMenuItem.Name = "var2ToolStripMenuItem";
            this.var2ToolStripMenuItem.Size = new System.Drawing.Size(97, 22);
            this.var2ToolStripMenuItem.Text = "Var2";
            // 
            // removeCovarianceToolStripMenuItem
            // 
            this.removeCovarianceToolStripMenuItem.Name = "removeCovarianceToolStripMenuItem";
            this.removeCovarianceToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.removeCovarianceToolStripMenuItem.Text = "Remove Covariance";
            this.removeCovarianceToolStripMenuItem.Click += new System.EventHandler(this.RemoveCovariance);
            // 
            // VariablePane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.predictedVariableList);
            this.Controls.Add(this.predictedLst);
            this.Controls.Add(this.allVarsLbl);
            this.Controls.Add(this.randomLbl);
            this.Controls.Add(this.fixedLbl);
            this.Controls.Add(this.allVarList);
            this.Controls.Add(this.randomList);
            this.Controls.Add(this.fixedList);
            this.Name = "VariablePane";
            this.Size = new System.Drawing.Size(188, 610);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox fixedList;
        private System.Windows.Forms.ListBox randomList;
        private System.Windows.Forms.ListBox allVarList;
        private System.Windows.Forms.Label fixedLbl;
        private System.Windows.Forms.Label randomLbl;
        private System.Windows.Forms.Label allVarsLbl;
        private System.Windows.Forms.Label predictedVariableList;
        private System.Windows.Forms.ListBox predictedLst;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addInteractionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeInteractionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addCovariateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem var1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem var2ToolStripMenuItem;
        private ToolStripMenuItem removeCovarianceToolStripMenuItem;
    }
}
