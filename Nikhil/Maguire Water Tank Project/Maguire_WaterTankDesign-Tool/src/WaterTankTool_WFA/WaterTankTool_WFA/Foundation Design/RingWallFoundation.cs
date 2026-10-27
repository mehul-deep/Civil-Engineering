using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class RingWallFoundation : Form
    {
        private readonly WaterTankDbContext _context;
        private RingWallEntity? _existingEntity;

        private TextBox txtFcPrime = null!, txtFy = null!, txtPhi = null!, txtHef = null!, txtHp = null!, txtGammaC = null!, txtPedestalB = null!, txtPedestalL = null!, txtPedestalAs = null!, txtFootingAs = null!;
        private ComboBox cmbTransverse = null!;
        private TextBox? txtNumLegs, txtPu, txtMu, txtVu;
        private GroupBox? groupBoxLoads;

        public RingWallFoundation()
        {
            InitializeComponent();
            _context = WaterTankDbContext.GetInstance();
            this.Load += RingWallFoundation_Load;
            button1.Click += Button1_Click;
        }

        private void RingWallFoundation_Load(object? sender, EventArgs e)
        {
            SetupMultiColumnUI();
            LoadExistingData();
        }

        private void SetupMultiColumnUI()
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                this.Text = "Multi-Leg Pedestal Foundation";
                this.Size = new Size(670, 455);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;

                // --- GroupBox 1: Governing Loads & ACI Factors ---
                groupBoxLoads = new GroupBox
                {
                    Text = "Governing Loads & ACI Factors",
                    Location = new Point(12, 12),
                    Size = new Size(295, 355)
                };
                this.Controls.Add(groupBoxLoads);

                int yLoad = 30;
                txtNumLegs = AddFieldToGroup(groupBoxLoads, "Number of Legs:", yLoad, true);
                txtPu = AddFieldToGroup(groupBoxLoads, "Axial Pu (kips):", yLoad + 28, true);
                txtMu = AddFieldToGroup(groupBoxLoads, "Moment Mu (k-ft):", yLoad + 56, true);
                txtVu = AddFieldToGroup(groupBoxLoads, "Shear Vu (kips):", yLoad + 84, true);

                Label lblNote = new Label
                {
                    Text = "Note: Loads and leg count are automatically\ninherited from Tank Analysis & Anchor Bolt.",
                    Location = new Point(12, yLoad + 118),
                    Size = new Size(270, 38),
                    ForeColor = Color.DimGray
                };
                groupBoxLoads.Controls.Add(lblNote);

                Label lblPhiTableHeader = new Label
                {
                    Text = "ACI 318 Transverse Reinf. Table (φ):",
                    Location = new Point(12, yLoad + 165),
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                groupBoxLoads.Controls.Add(lblPhiTableHeader);

                Label lblPhiTable = new Label
                {
                    Text = "No.   Shape          φ       Max Axial\n" +
                           "1.     Spirals      0.75       0.85 Po\n" +
                           "2.     Tied          0.65       0.80 Po",
                    Location = new Point(12, yLoad + 188),
                    Size = new Size(268, 62),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(248, 249, 250),
                    Padding = new Padding(6, 4, 6, 4)
                };
                groupBoxLoads.Controls.Add(lblPhiTable);

                // --- GroupBox 2: Pedestal Geometry & Material ---
                groupBox2.Text = "Pedestal Geometry & Material";
                groupBox2.Location = new Point(318, 12);
                groupBox2.Size = new Size(322, 355);

                label1.Visible = textBox1.Visible = false;
                label2.Visible = textBox2.Visible = false;
                label3.Visible = textBox3.Visible = false;
                label4.Visible = textBox4.Visible = false;
                label5.Visible = textBox5.Visible = false;
                label6.Visible = textBox6.Visible = false;
                label7.Visible = textBox7.Visible = false;
                label8.Visible = textBox8.Visible = false;
                label9.Visible = textBox9.Visible = false;
                label10.Visible = textBox10.Visible = false;

                int startY = 24;
                int gapY = 31;

                txtFcPrime = AddFieldToGroup(groupBox2, "Concrete f'c (ksi):", startY, false, 175);
                txtFy = AddFieldToGroup(groupBox2, "Steel fy (ksi):", startY + gapY * 1, false, 175);

                // Transverse Reinforcement Shape Selector + Auto-populated φ box
                Label lblTrans = new Label { Text = "Transverse Reinf. (φ):", Location = new Point(12, startY + gapY * 2 + 3), AutoSize = true };
                cmbTransverse = new ComboBox
                {
                    Location = new Point(175, startY + gapY * 2),
                    Size = new Size(82, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbTransverse.Items.AddRange(new object[] { "1. Spirals", "2. Tied" });
                txtPhi = new TextBox
                {
                    Location = new Point(262, startY + gapY * 2),
                    Size = new Size(46, 23),
                    ReadOnly = true,
                    BackColor = Color.FromArgb(240, 240, 240),
                    TextAlign = HorizontalAlignment.Center
                };
                cmbTransverse.SelectedIndexChanged += (s, e) =>
                {
                    txtPhi.Text = cmbTransverse.SelectedIndex == 0 ? "0.75" : "0.65";
                };
                cmbTransverse.SelectedIndex = 1; // Default: 2. Tied (0.65)
                groupBox2.Controls.Add(lblTrans);
                groupBox2.Controls.Add(cmbTransverse);
                groupBox2.Controls.Add(txtPhi);

                txtHef = AddFieldToGroup(groupBox2, "Embedment Depth hef (in):", startY + gapY * 3, false, 175);
                txtHp = AddFieldToGroup(groupBox2, "Pedestal Clear Height Hp (ft):", startY + gapY * 4, false, 175);
                txtGammaC = AddFieldToGroup(groupBox2, "Concrete Unit Wt (pcf):", startY + gapY * 5, false, 175);
                txtPedestalB = AddFieldToGroup(groupBox2, "Pedestal Size B (in):", startY + gapY * 6, false, 175);
                txtPedestalL = AddFieldToGroup(groupBox2, "Pedestal Size L (in):", startY + gapY * 7, false, 175);
                txtPedestalAs = AddFieldToGroup(groupBox2, "Provided Pedestal As (in2):", startY + gapY * 8, false, 175);
                txtFootingAs = AddFieldToGroup(groupBox2, "Provided Dev. Length (in):", startY + gapY * 9, false, 175);

                button1.Location = new Point(235, 380);
                button2.Location = new Point(335, 380);
            }
        }

        private TextBox AddFieldToGroup(GroupBox grp, string labelText, int yPos, bool isReadOnly, int txtX = 155)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(12, yPos + 3), AutoSize = true };
            TextBox txt = new TextBox
            {
                Location = new Point(txtX, yPos),
                Size = new Size(133, 23),
                ReadOnly = isReadOnly,
                BackColor = isReadOnly ? Color.FromArgb(240, 240, 240) : Color.White
            };
            grp.Controls.Add(lbl);
            grp.Controls.Add(txt);
            txt.BringToFront();
            return txt;
        }

        private void LoadExistingData()
        {
            AnchorBoltEntity? anchorBolt = null;
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                AppState.Load();
                anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
                var basePlate = _context.BasePlateEntity?.FirstOrDefault();
                var loadService = new Services.LoadService();
                var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

                int numLegs = AppState.NoOfColumns > 1
                    ? AppState.NoOfColumns
                    : ((anchorBolt?.Ns.HasValue == true && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);

                double pu = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
                double mu = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                            ? basePlate.OverturningMoment.Value
                            : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
                double vu = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;

                if (txtNumLegs != null) txtNumLegs.Text = numLegs.ToString(CultureInfo.InvariantCulture);
                if (txtPu != null) txtPu.Text = pu.ToString("F4", CultureInfo.InvariantCulture);
                if (txtMu != null) txtMu.Text = mu.ToString("F4", CultureInfo.InvariantCulture);
                if (txtVu != null) txtVu.Text = vu.ToString("F4", CultureInfo.InvariantCulture);
            }

            _existingEntity = _context.Set<RingWallEntity>().FirstOrDefault();
            if (_existingEntity == null)
            {
                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    double autoB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
                    double autoL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : autoB;
                    double autoHef = (anchorBolt?.Hef.HasValue == true && anchorBolt.Hef.Value > 0) ? anchorBolt.Hef.Value : 35.0;

                    txtFcPrime.Text = "4.0";
                    txtFy.Text = "60";
                    cmbTransverse.SelectedIndex = 1; // 2. Tied (0.65)
                    txtHef.Text = autoHef.ToString(CultureInfo.InvariantCulture);
                    txtHp.Text = "7.417";
                    txtGammaC.Text = "150";
                    txtPedestalB.Text = autoB.ToString(CultureInfo.InvariantCulture);
                    txtPedestalL.Text = autoL.ToString(CultureInfo.InvariantCulture);
                    txtPedestalAs.Text = "7.92"; // 18-#6 bars
                    txtFootingAs.Text = "30";    // 30 in provided development length
                }
                return;
            }

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                double autoB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
                double autoL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : autoB;
                double autoHef = (anchorBolt?.Hef.HasValue == true && anchorBolt.Hef.Value > 0) ? anchorBolt.Hef.Value : 35.0;

                txtFcPrime.Text = (_existingEntity.FcPrime ?? 4.0).ToString(CultureInfo.InvariantCulture);
                txtFy.Text = (_existingEntity.Fy ?? 60.0).ToString(CultureInfo.InvariantCulture);

                double phiSaved = _existingEntity.PhiCompression ?? 0.65;
                cmbTransverse.SelectedIndex = Math.Abs(phiSaved - 0.75) < 0.01 ? 0 : 1;
                txtPhi.Text = cmbTransverse.SelectedIndex == 0 ? "0.75" : "0.65";

                txtHef.Text = (_existingEntity.Hef ?? autoHef).ToString(CultureInfo.InvariantCulture);
                txtHp.Text = (_existingEntity.Hp ?? 7.417).ToString(CultureInfo.InvariantCulture);
                txtGammaC.Text = (_existingEntity.GammaC ?? 150.0).ToString(CultureInfo.InvariantCulture);
                txtPedestalB.Text = (_existingEntity.PedestalSizeB ?? autoB).ToString(CultureInfo.InvariantCulture);
                txtPedestalL.Text = (_existingEntity.PedestalSizeL ?? autoL).ToString(CultureInfo.InvariantCulture);
                txtPedestalAs.Text = (_existingEntity.PedestalAsProv ?? 7.92).ToString(CultureInfo.InvariantCulture);

                double savedLprov = _existingEntity.FootingAsProv ?? 30.0;
                if (Math.Abs(savedLprov - 5.28) < 0.01) savedLprov = 30.0; // Upgrade legacy default
                txtFootingAs.Text = savedLprov.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                textBox1.Text = _existingEntity.Trw.ToString();
                textBox2.Text = _existingEntity.B.ToString();
                textBox3.Text = _existingEntity.Rcl.ToString();
                textBox4.Text = _existingEntity.TCE.ToString();
                textBox5.Text = _existingEntity.Tedge.ToString();
                textBox6.Text = _existingEntity.Cc.ToString();
                textBox7.Text = _existingEntity.Rin.ToString();
                textBox8.Text = _existingEntity.Rout.ToString();
                textBox9.Text = _existingEntity.A.ToString();
                textBox10.Text = _existingEntity.Rrw.ToString();
            }
        }

        private void Button1_Click(object? sender, EventArgs e)
        {
            if (_existingEntity == null)
            {
                _existingEntity = new RingWallEntity();
                _context.Set<RingWallEntity>().Add(_existingEntity);
            }

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                double.TryParse(txtFcPrime.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fc); _existingEntity.FcPrime = fc;
                double.TryParse(txtFy.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fy); _existingEntity.Fy = fy;
                _existingEntity.PhiCompression = cmbTransverse.SelectedIndex == 0 ? 0.75 : 0.65;
                double.TryParse(txtHef.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double hef); _existingEntity.Hef = hef;
                double.TryParse(txtHp.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double hp); _existingEntity.Hp = hp;
                double.TryParse(txtGammaC.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double gc); _existingEntity.GammaC = gc;
                double.TryParse(txtPedestalB.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double b); _existingEntity.PedestalSizeB = b;
                double.TryParse(txtPedestalL.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double l); _existingEntity.PedestalSizeL = l;
                double.TryParse(txtPedestalAs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double asProv); _existingEntity.PedestalAsProv = asProv;
                double.TryParse(txtFootingAs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fAsProv); _existingEntity.FootingAsProv = fAsProv;
            }
            else
            {
                double.TryParse(textBox1.Text, out double trw); _existingEntity.Trw = trw;
                double.TryParse(textBox2.Text, out double b); _existingEntity.B = b;
                double.TryParse(textBox3.Text, out double rcl); _existingEntity.Rcl = rcl;
                double.TryParse(textBox4.Text, out double tce); _existingEntity.TCE = tce;
                double.TryParse(textBox5.Text, out double tedge); _existingEntity.Tedge = tedge;
                double.TryParse(textBox6.Text, out double cc); _existingEntity.Cc = cc;
                double.TryParse(textBox7.Text, out double rin); _existingEntity.Rin = rin;
                double.TryParse(textBox8.Text, out double rout); _existingEntity.Rout = rout;
                double.TryParse(textBox9.Text, out double a); _existingEntity.A = a;
                double.TryParse(textBox10.Text, out double rrw); _existingEntity.Rrw = rrw;
            }
            _context.SaveChanges();
        }
    }
}
