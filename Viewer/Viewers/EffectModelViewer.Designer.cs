using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using engenious.WinForms;

namespace engenious.ContentTool.Viewer.Viewers
{
    public partial class EffectModelViewer
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.gameControl = new GameControl();
            this.splitContainer = new SplitContainer();
            this.tableLayout = new TableLayoutPanel();
            this.techniquesComboBox = new ComboBox();
            this.techniquesLabel = new Label();
            this.SuspendLayout();
            
            //
            // splitContainer
            // 
            this.splitContainer.SuspendLayout();
            this.splitContainer.Dock = DockStyle.Fill;
            this.splitContainer.Panel1.Controls.Add(tableLayout);
            this.splitContainer.Panel2.Controls.Add(gameControl);
            this.splitContainer.ResumeLayout();
            // 
            // techniquesLabel
            // 
            this.techniquesLabel.Location = new System.Drawing.Point(0, 0);
            this.techniquesLabel.Name = "techniquesLabel";
            this.techniquesLabel.Size = new System.Drawing.Size(150, 20);
            this.techniquesLabel.AutoSize = true;
            this.techniquesLabel.TabIndex = 0;
            this.techniquesLabel.TabStop = false;
            this.techniquesLabel.Text = "Techniques:";
            // 
            // techniquesComboBox
            // 
            this.techniquesComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.techniquesComboBox.Location = new System.Drawing.Point(0, 0);
            this.techniquesComboBox.Name = "techniquesComboBox";
            this.techniquesComboBox.TabIndex = 0;
            this.techniquesComboBox.TabStop = false;
            this.techniquesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            // 
            // tableLayout
            // 
            this.tableLayout.Dock = DockStyle.Fill;
            this.tableLayout.Location = new System.Drawing.Point(0, 0);
            this.tableLayout.Name = "tableLayout";
            this.tableLayout.TabIndex = 0;
            this.tableLayout.TabStop = false;
            this.tableLayout.RowCount = 1;
            this.tableLayout.ColumnCount = 2;
            this.tableLayout.Controls.Add(techniquesLabel, 0, 0);
            this.tableLayout.Controls.Add(techniquesComboBox, 1, 0);
            this.tableLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            // 
            // gameControl
            // 
            this.gameControl.Dock = DockStyle.Fill;
            this.gameControl.Location = new System.Drawing.Point(0, 0);
            this.gameControl.Name = "gameControl";
            this.gameControl.Size = new System.Drawing.Size(150, 150);
            this.gameControl.TabIndex = 0;
            this.gameControl.TabStop = false;
            // 
            // EffectViewer
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer);
            this.Name = "EffectViewer";
            this.ResumeLayout(false);

        }

        #endregion

        private Label techniquesLabel;
        private ComboBox techniquesComboBox;
        private TableLayoutPanel tableLayout;
        private SplitContainer splitContainer;
        private GameControl gameControl;
    }
}