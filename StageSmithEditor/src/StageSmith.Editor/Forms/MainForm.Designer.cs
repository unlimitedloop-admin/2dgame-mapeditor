using StageSmith.Editor.Controls;

namespace StageSmith.Editor
{
    partial class MainForm
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
            panel1 = new DoubleBufferedPanel();
            panelPalette = new DoubleBufferedPanel();
            btnGrid = new Button();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Location = new Point(83, 48);
            panel1.Name = "panel1";
            panel1.Size = new Size(256, 240);
            panel1.TabIndex = 0;
            panel1.Paint += panel1_Paint;
            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseMove += panel1_MouseMove;
            panel1.MouseUp += panel1_MouseUp;
            // 
            // panelPalette
            // 
            panelPalette.AutoScroll = true;
            panelPalette.Location = new Point(12, 294);
            panelPalette.Name = "panelPalette";
            panelPalette.Size = new Size(512, 128);
            panelPalette.TabIndex = 1;
            panelPalette.Paint += panelPalette_Paint;
            panelPalette.MouseDown += panelPalette_MouseDown;
            // 
            // btnGrid
            // 
            btnGrid.BackgroundImageLayout = ImageLayout.Stretch;
            btnGrid.Image = StageSmithEditor.Properties.Resources.icons8_グリッド_24;
            btnGrid.Location = new Point(12, 48);
            btnGrid.Name = "btnGrid";
            btnGrid.Size = new Size(30, 30);
            btnGrid.TabIndex = 2;
            btnGrid.UseVisualStyleBackColor = true;
            btnGrid.Click += btnGrid_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(799, 450);
            Controls.Add(btnGrid);
            Controls.Add(panelPalette);
            Controls.Add(panel1);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private DoubleBufferedPanel panel1;
        private DoubleBufferedPanel panelPalette;
        private Button btnGrid;
    }
}
