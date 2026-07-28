using System;
using System.Drawing;
using System.Windows.Forms;

namespace WaterTankTool_WFA
{
    public partial class TankTypeSelectionForm : Form
    {
        public TankType SelectedTankType { get; private set; } = TankType.None;

        public TankTypeSelectionForm()
        {
            InitializeComponent();

            // Smooth rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            BuildUI();
        }

        private void BuildUI()
        {
            // --- Form ---
            this.Text = "Select Water Tank Type";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(860, 500);
            this.BackColor = ColorTranslator.FromHtml("#4F8D95");
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.Font = new Font("Segoe UI", 10F);
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    this.DialogResult = DialogResult.Cancel;
            };

            // Main layout
            var mainPanel = new TableLayoutPanel
            {
                RowCount = 3,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 12),
                BackColor = Color.Transparent,
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));   // header
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // cards
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // footer
            this.Controls.Add(mainPanel);

            // Header
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var title = new Label
            {
                Text = "Select Water Tank Type",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 52,
                TextAlign = ContentAlignment.BottomCenter
            };

            var subtitle = new Label
            {
                Text = "Choose the structural configuration to start your project",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 245, 245, 245),
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.TopCenter
            };

            headerPanel.Controls.Add(subtitle);
            headerPanel.Controls.Add(title);
            mainPanel.Controls.Add(headerPanel, 0, 0);

            // Cards panel
            var cardsPanel = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 10, 30, 10),
                BackColor = Color.Transparent
            };
            cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainPanel.Controls.Add(cardsPanel, 0, 1);

            // Card 1: Single Column
            var cardSingle = new TankTypeCard
            {
                Label = "Single Column",
                CardImage = Properties.Resources.Sheldon_IA_New_Tank_Paint_2,
                CardTankType = TankType.SingleColumn,
                Margin = new Padding(12, 10, 12, 10),
                Dock = DockStyle.Fill
            };

            cardSingle.CardClick += (s, e) =>
            {
                AppState.CurrentTankType = TankType.SingleColumn;
                SelectedTankType = TankType.SingleColumn;
                this.DialogResult = DialogResult.OK;
            };

            cardsPanel.Controls.Add(cardSingle, 0, 0);

            // Card 2: Multi Column
            var cardMulti = new TankTypeCard
            {
                Label = "Multi-Column",
                CardImage = Properties.Resources.Katy,
                CardTankType = TankType.MultiColumn,
                Margin = new Padding(12, 10, 12, 10),
                Dock = DockStyle.Fill
            };

            cardMulti.CardClick += (s, e) =>
            {
                AppState.CurrentTankType = TankType.MultiColumn;
                if (AppState.NoOfColumns <= 1) AppState.NoOfColumns = 4;
                SelectedTankType = TankType.MultiColumn;
                this.DialogResult = DialogResult.OK;
            };

            cardsPanel.Controls.Add(cardMulti, 1, 0);

            // Footer
            var footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var footer = new Label
            {
                Text = "© 2024 SDSU • Maguire Iron • Water Tank Design Tool",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(220, 235, 235, 235),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            footerPanel.Controls.Add(footer);
            mainPanel.Controls.Add(footerPanel, 0, 2);
        }
    }
}