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
            btnGrid = new Button();
            btnTilePreview = new Button();
            SuspendLayout();
            // 
            // btnGrid
            // 
            btnGrid.Image = StageSmithEditor.Properties.Resources.icons8_グリッド_24;
            btnGrid.Location = new Point(12, 48);
            btnGrid.Name = "btnGrid";
            btnGrid.Size = new Size(30, 30);
            btnGrid.TabIndex = 2;
            btnGrid.UseVisualStyleBackColor = true;
            btnGrid.Click += btnGrid_Click;
            // 
            // btnTilePreview
            // 
            btnTilePreview.Image = StageSmithEditor.Properties.Resources.icons8_目に見える_24;
            btnTilePreview.Location = new Point(44, 48);
            btnTilePreview.Name = "btnTilePreview";
            btnTilePreview.Size = new Size(30, 30);
            btnTilePreview.TabIndex = 6;
            btnTilePreview.UseVisualStyleBackColor = true;
            btnTilePreview.Click += btnTilePreview_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(799, 450);
            Controls.Add(btnTilePreview);
            Controls.Add(btnGrid);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion
        private Button btnGrid;
        private Button btnTilePreview;
    }
}
