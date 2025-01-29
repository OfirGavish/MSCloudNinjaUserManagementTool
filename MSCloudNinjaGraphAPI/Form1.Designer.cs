using System;
using System.Drawing;
using System.Windows.Forms;

namespace MSCloudNinjaGraphAPI
{
    partial class MainForm : Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            mainContent = new Panel();
            headerPanel = new Panel();
            SuspendLayout();
            // 
            // mainContent
            // 
            mainContent.Location = new Point(12, 62);
            mainContent.Name = "mainContent";
            mainContent.Size = new Size(776, 376);
            mainContent.TabIndex = 2;
            // 
            // headerPanel
            // 
            headerPanel.Location = new Point(12, 12);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(776, 50);
            headerPanel.TabIndex = 1;
            // 
            // MainForm
            // 
            ClientSize = new Size(278, 244);
            Controls.Add(mainContent);
            Controls.Add(headerPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            ResumeLayout(false);
        }

        #endregion

        protected Panel mainContent;
        protected Panel headerPanel;
    }
}
