using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class RingWallFoundation : Form
    {
        private readonly WaterTankDbContext _context;
        private RingWallEntity? _existingEntity;

        // Multi-Column controls (UNTOUCHED)
        private TextBox txtFcPrime = null!, txtFy = null!, txtPhi = null!, txtHef = null!, txtHp = null!, txtGammaC = null!, txtPedestalB = null!, txtPedestalL = null!, txtPedestalAs = null!, txtFootingAs = null!;
        private ComboBox cmbTransverse = null!;
        private TextBox? txtNumLegs, txtPu, txtMu, txtVu;
        private GroupBox? groupBoxLoads;

        // Single-Column controls (Updated to Ringwallsinglelegdesign.pdf Steps 1 - 14)
        private GroupBox? grpScLoads;
        private TextBox txtScP = null!, txtScMu = null!, txtScVu = null!, txtScDcone = null!, txtScBpInfo = null!, txtScAbInfo = null!, txtScBfHf = null!, txtScQa = null!, txtScGammaSoil = null!, txtScKa = null!;
        private TextBox txtScDcl = null!, txtScTw = null!, txtScDerivedRadii = null!, txtScHw = null!, txtScFc = null!, txtScFy = null!, txtScGammaC = null!, txtScCover = null!;
        private ComboBox cmbScSlenderK = null!;
        private Button btnScVertBar = null!, btnScHoopBar = null!;
        private int _selectedVertBarIdx = 1; // Default #4
        private int _selectedHoopBarIdx = 1; // Default #4
        private TextBox txtScVertCount = null!, txtScHoopCount = null!;

        private static readonly (string Label, string DiameterDesc, double Area, string TypicalUse)[] RebarOptions = new[]
        {
            ("#3", "3/8\" (0.375 in)", 0.11, "Light ties / temperature steel"),
            ("#4", "4/8\" (0.500 in)", 0.20, "Standard wall vertical & hoop bars (Default / PDF)"),
            ("#5", "5/8\" (0.625 in)", 0.31, "Medium wall / footing reinforcement"),
            ("#6", "6/8\" (0.750 in)", 0.44, "Heavy wall / pedestal vertical bars"),
            ("#7", "7/8\" (0.875 in)", 0.60, "Heavy foundation reinforcement"),
            ("#8", "8/8\" (1.000 in)", 0.79, "Large foundation / mat reinforcement")
        };

        public RingWallFoundation()
        {
            InitializeComponent();
            _context = WaterTankDbContext.GetInstance();
            this.Load += RingWallFoundation_Load;
            button1.Click += Button1_Click;
        }

        private void RingWallFoundation_Load(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                SetupMultiColumnUI();
            }
            else
            {
                SetupSingleColumnUI();
            }
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

        private void SetupSingleColumnUI()
        {
            this.Text = "Single-Leg Ring Wall Foundation (ACI 318-19 / PIP STE03020)";
            this.Size = new Size(755, 515);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

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

            // --- Left Group: Inherited Loads, Predefined Geometry & Geotechnical Report Inputs ---
            grpScLoads = new GroupBox
            {
                Text = "Predefined Tank Loads & Foundation Inputs",
                Location = new Point(12, 12),
                Size = new Size(340, 410)
            };
            this.Controls.Add(grpScLoads);

            int yL = 24;
            int gap = 29;
            txtScP = AddScField(grpScLoads, "Factored Axial Load Pu (kip):", yL + gap * 0, false, 192, 134);
            txtScMu = AddScField(grpScLoads, "Factored Moment Mu (kip-ft):", yL + gap * 1, false, 192, 134);
            txtScVu = AddScField(grpScLoads, "Factored Shear Vu (kip):", yL + gap * 2, false, 192, 134);
            txtScDcone = AddScField(grpScLoads, "Tank Cone Diameter Dcone (ft):", yL + gap * 3, true, 192, 134);
            txtScBpInfo = AddScField(grpScLoads, "Base Plate Width / Thk (in):", yL + gap * 4, true, 192, 134);
            txtScAbInfo = AddScField(grpScLoads, "Anchor Bolts (Nb / db / hef):", yL + gap * 5, true, 192, 134);
            txtScBfHf = AddScField(grpScLoads, "Prelim. Footing Bf / hf (ft):", yL + gap * 6, false, 192, 134);

            // Heading: Geotechnical Report Inputs
            int yGeoHeader = yL + gap * 7 + 6;
            Label lblGeotechHeader = new Label
            {
                Text = "Geotechnical Report Inputs",
                Location = new Point(12, yGeoHeader),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            grpScLoads.Controls.Add(lblGeotechHeader);

            int yGeo = yGeoHeader + 24;
            txtScQa = AddScField(grpScLoads, "Allow. Soil Bearing qa (ksf):", yGeo + gap * 0, false, 192, 134);
            txtScGammaSoil = AddScField(grpScLoads, "Soil Unit Weight γs (pcf):", yGeo + gap * 1, false, 192, 134);
            txtScKa = AddScField(grpScLoads, "Active Earth Coeff. Ka:", yGeo + gap * 2, false, 192, 134);

            Label lblPipNote = new Label
            {
                Text = "Note: Per PIP STE03020, minimum ringwall thickness tw ≥ 1.0 ft\n(2.0 ft standard). Retained soil height Hs = Hw.",
                Location = new Point(12, yGeo + gap * 3 + 4),
                Size = new Size(316, 34),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            grpScLoads.Controls.Add(lblPipNote);

            // --- Right Group: Ring-Wall Geometry, Material & Reinforcement ---
            groupBox2.Text = "Ring-Wall Geometry, Material & Reinforcement";
            groupBox2.Location = new Point(362, 12);
            groupBox2.Size = new Size(364, 410);

            int yR = 24;
            int gapR = 31;
            txtScDcl = AddScField(groupBox2, "Centerline Diameter D_CL (ft):", yR + gapR * 0, false, 200, 150);
            txtScTw = AddScField(groupBox2, "Ring-Wall Thickness tw (ft):", yR + gapR * 1, false, 200, 150);
            txtScDerivedRadii = AddScField(groupBox2, "Derived Do / Di / Ro / Ri (ft):", yR + gapR * 2, true, 200, 150);
            txtScHw = AddScField(groupBox2, "Ring-Wall Height Hw (ft):", yR + gapR * 3, false, 200, 150);
            txtScFc = AddScField(groupBox2, "Concrete Strength f'c (ksi):", yR + gapR * 4, false, 200, 150);
            txtScFy = AddScField(groupBox2, "Reinforcing Steel Fy (ksi):", yR + gapR * 5, false, 200, 150);
            txtScGammaC = AddScField(groupBox2, "Concrete Unit Wt γc (pcf):", yR + gapR * 6, false, 200, 150);
            txtScCover = AddScField(groupBox2, "Concrete Clear Cover cc (in):", yR + gapR * 7, false, 200, 150);

            // Vertical Reinforcement (Count per face + Bar size selector button, e.g., 66 #4 bars EF)
            Label lblVert = new Label { Text = "Vertical Bars (Each Face):", Location = new Point(12, yR + gapR * 8 + 3), AutoSize = true };
            txtScVertCount = new TextBox
            {
                Location = new Point(200, yR + gapR * 8),
                Size = new Size(42, 23),
                Text = "66"
            };
            Label lblVertX = new Label { Text = "bars", Location = new Point(244, yR + gapR * 8 + 3), AutoSize = true };
            btnScVertBar = new Button
            {
                Location = new Point(274, yR + gapR * 8 - 1),
                Size = new Size(76, 25),
                Text = "#4 (Select)",
                BackColor = Color.FromArgb(235, 243, 254),
                FlatStyle = FlatStyle.Standard,
                Cursor = Cursors.Hand
            };
            btnScVertBar.Click += (s, e) => OpenRebarSelectionDialog(true);
            groupBox2.Controls.Add(lblVert);
            groupBox2.Controls.Add(txtScVertCount);
            groupBox2.Controls.Add(lblVertX);
            groupBox2.Controls.Add(btnScVertBar);

            // Horizontal Hoop Reinforcement (Count per face + Bar size selector button, e.g., 8 #4 bars EF)
            Label lblHoop = new Label { Text = "Horizontal Hoop Bars (EF):", Location = new Point(12, yR + gapR * 9 + 3), AutoSize = true };
            txtScHoopCount = new TextBox
            {
                Location = new Point(200, yR + gapR * 9),
                Size = new Size(42, 23),
                Text = "8"
            };
            Label lblHoopX = new Label { Text = "bars", Location = new Point(244, yR + gapR * 9 + 3), AutoSize = true };
            btnScHoopBar = new Button
            {
                Location = new Point(274, yR + gapR * 9 - 1),
                Size = new Size(76, 25),
                Text = "#4 (Select)",
                BackColor = Color.FromArgb(235, 243, 254),
                FlatStyle = FlatStyle.Standard,
                Cursor = Cursors.Hand
            };
            btnScHoopBar.Click += (s, e) => OpenRebarSelectionDialog(false);
            groupBox2.Controls.Add(lblHoop);
            groupBox2.Controls.Add(txtScHoopCount);
            groupBox2.Controls.Add(lblHoopX);
            groupBox2.Controls.Add(btnScHoopBar);

            // Slenderness Effective Length Factor k
            Label lblSlenderK = new Label { Text = "Slenderness Factor k:", Location = new Point(12, yR + gapR * 10 + 3), AutoSize = true };
            cmbScSlenderK = new ComboBox
            {
                Location = new Point(200, yR + gapR * 10),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbScSlenderK.Items.AddRange(new object[]
            {
                "Both k=1.0 & k=2.0",
                "k = 1.0 (Braced)",
                "k = 2.0 (Cantilever)"
            });
            cmbScSlenderK.SelectedIndex = 0;
            groupBox2.Controls.Add(lblSlenderK);
            groupBox2.Controls.Add(cmbScSlenderK);

            txtScDcl.TextChanged += UpdateSingleColumnDerivedGeometry;
            txtScTw.TextChanged += UpdateSingleColumnDerivedGeometry;

            button1.Location = new Point(280, 434);
            button2.Location = new Point(380, 434);
            this.CenterToScreen();
        }

        private void UpdateSingleColumnDerivedGeometry(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn) return;

            if (double.TryParse(txtScDcl.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dcl) &&
                double.TryParse(txtScTw.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double tw) &&
                dcl > tw && tw > 0)
            {
                double doFt = dcl + tw;
                double diFt = dcl - tw;
                double roFt = doFt / 2.0;
                double riFt = diFt / 2.0;
                txtScDerivedRadii.Text = $"Do={doFt:0.##}, Di={diFt:0.##} (Ro={roFt:0.##}, Ri={riFt:0.##})";
                txtScDerivedRadii.BackColor = tw >= 1.0 ? Color.LightGreen : Color.LightCoral;
            }
            else
            {
                txtScDerivedRadii.Text = "";
                txtScDerivedRadii.BackColor = Color.FromArgb(240, 240, 240);
            }
        }

        private TextBox AddScField(GroupBox grp, string labelText, int yPos, bool isReadOnly, int txtX, int txtW)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(12, yPos + 3), AutoSize = true };
            TextBox txt = new TextBox
            {
                Location = new Point(txtX, yPos),
                Size = new Size(txtW, 23),
                ReadOnly = isReadOnly,
                BackColor = isReadOnly ? Color.FromArgb(240, 240, 240) : Color.White
            };
            grp.Controls.Add(lbl);
            grp.Controls.Add(txt);
            txt.BringToFront();
            return txt;
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
            else
            {
                LoadSingleColumnData();
                return;
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
        }

        private void LoadSingleColumnData()
        {
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();
            var loadService = new Services.LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            _existingEntity = _context.Set<RingWallEntity>().FirstOrDefault();

            // Governing Pu, Mu, Vu (live LoadService first, matching BasePlate/AnchorBolt)
            double pVal = puGov > 0
                ? puGov
                : ((_existingEntity?.ScPu.HasValue == true && _existingEntity.ScPu.Value > 0)
                    ? _existingEntity.ScPu.Value
                    : (basePlate?.Pu ?? 1653.556));
            double muVal = muGov > 0
                ? muGov
                : ((_existingEntity?.ScMu.HasValue == true && _existingEntity.ScMu.Value > 0)
                    ? _existingEntity.ScMu.Value
                    : (basePlate?.OverturningMoment ?? anchorBolt?.Mu ?? 2903.3789));
            double vuVal = vuGov > 0
                ? vuGov
                : ((_existingEntity?.ScVu.HasValue == true && _existingEntity.ScVu.Value > 0)
                    ? _existingEntity.ScVu.Value
                    : ((anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : 53.43));

            // Tank Cone Diameter Dcone
            double coneDiamFt = 21.0;
            if (basePlate != null && basePlate.Dbp > 0)
            {
                coneDiamFt = basePlate.Dbp;
            }
            else
            {
                var bottomSeg = _context.SegmentProperties?.OrderBy(s => s.HeightInitial).FirstOrDefault();
                if (bottomSeg != null && (bottomSeg.DiameterFinal ?? bottomSeg.Diameter) > 0)
                    coneDiamFt = bottomSeg.DiameterFinal ?? bottomSeg.Diameter;
                else if (anchorBolt != null && anchorBolt.Rb > 0)
                    coneDiamFt = Math.Round(2.0 * anchorBolt.Rb - 0.5, 2);
            }

            double bpWidthIn = (basePlate != null && basePlate.Ro > basePlate.Ri) ? (basePlate.Ro - basePlate.Ri) * 12.0 : 12.0;
            double bpThkIn = (basePlate != null && basePlate.T > 0) ? basePlate.T : ((anchorBolt != null && anchorBolt.Tbp > 0) ? anchorBolt.Tbp : 3.0);
            int nbVal = (anchorBolt != null && anchorBolt.Nb > 0) ? anchorBolt.Nb : (basePlate?.Nb ?? 20);
            double dbVal = (anchorBolt != null && anchorBolt.Db > 0) ? anchorBolt.Db : 1.5;
            double hefVal = (anchorBolt?.Hef.HasValue == true && anchorBolt.Hef.Value > 0) ? anchorBolt.Hef.Value : 96.0;

            txtScP.Text = pVal.ToString("F4", CultureInfo.InvariantCulture);
            txtScMu.Text = muVal.ToString("F4", CultureInfo.InvariantCulture);
            txtScVu.Text = vuVal.ToString("F4", CultureInfo.InvariantCulture);
            txtScDcone.Text = coneDiamFt.ToString("F2", CultureInfo.InvariantCulture);
            txtScBpInfo.Text = $"{bpWidthIn:0.#}\" W x {bpThkIn:0.##}\" Thk";
            txtScAbInfo.Text = $"({nbVal}) {dbVal:0.##}\" Ø, hef={hefVal:0.#}\"";

            // Check if entity is brand-new or from legacy v1 (where ScQa was null)
            bool isUpdatedSchema = _existingEntity != null && _existingEntity.ScQa.HasValue;
            if (!isUpdatedSchema)
            {
                double defaultTw = 2.0;
                int defaultVertBars = (int)Math.Round(Math.PI * coneDiamFt); // 66 for D_CL=21 ft

                txtScBfHf.Text = "6.5 / 3.0";
                txtScQa.Text = "4.0";
                txtScGammaSoil.Text = "125";
                txtScKa.Text = "0.30";

                txtScDcl.Text = coneDiamFt.ToString("0.####", CultureInfo.InvariantCulture);
                txtScTw.Text = defaultTw.ToString("0.##", CultureInfo.InvariantCulture);
                txtScHw.Text = "9.0";
                txtScFc.Text = "4.0";
                txtScFy.Text = "60";
                txtScGammaC.Text = "150";
                txtScCover.Text = "2.0";

                txtScVertCount.Text = defaultVertBars.ToString(CultureInfo.InvariantCulture);
                _selectedVertBarIdx = 1; // #4
                txtScHoopCount.Text = "8";
                _selectedHoopBarIdx = 1; // #4
                UpdateRebarButtonLabels();
                cmbScSlenderK.SelectedIndex = 0; // Both k=1.0 & k=2.0

                UpdateSingleColumnDerivedGeometry(null, EventArgs.Empty);
                return;
            }

            double dcl = (_existingEntity!.ScDcl.HasValue && _existingEntity.ScDcl.Value > 0)
                ? _existingEntity.ScDcl.Value
                : (_existingEntity.Rcl > 0 ? 2.0 * _existingEntity.Rcl : coneDiamFt);
            double tw = _existingEntity.Trw > 0 ? _existingEntity.Trw : 2.0;
            double hw = (_existingEntity.Hw.HasValue && _existingEntity.Hw.Value > 0) ? _existingEntity.Hw.Value : 9.0;

            double fcKsi = _existingEntity.ScFcPrime ?? 4.0;
            if (fcKsi > 100) fcKsi /= 1000.0;

            double bf = _existingEntity.ScBf ?? 6.5;
            double hf = _existingEntity.ScHf ?? 3.0;
            txtScBfHf.Text = $"{bf:0.##} / {hf:0.##}";
            txtScQa.Text = (_existingEntity.ScQa ?? 4.0).ToString("0.##", CultureInfo.InvariantCulture);
            txtScGammaSoil.Text = (_existingEntity.ScGammaSoil ?? 125.0).ToString("0.#", CultureInfo.InvariantCulture);
            txtScKa.Text = (_existingEntity.ScKa ?? 0.30).ToString("0.##", CultureInfo.InvariantCulture);

            txtScDcl.Text = dcl.ToString("0.####", CultureInfo.InvariantCulture);
            txtScTw.Text = tw.ToString("0.##", CultureInfo.InvariantCulture);
            txtScHw.Text = hw.ToString("0.##", CultureInfo.InvariantCulture);
            txtScFc.Text = fcKsi.ToString("0.##", CultureInfo.InvariantCulture);
            txtScFy.Text = (_existingEntity.ScFy ?? 60.0).ToString("0.##", CultureInfo.InvariantCulture);
            txtScGammaC.Text = (_existingEntity.ScGammaC ?? 150.0).ToString("0.#", CultureInfo.InvariantCulture);
            txtScCover.Text = (_existingEntity.Cc > 0 ? _existingEntity.Cc : 2.0).ToString("0.##", CultureInfo.InvariantCulture);

            int vertCount = _existingEntity.VertBarCountPerFace ?? (int)Math.Round(Math.PI * dcl);
            txtScVertCount.Text = vertCount.ToString(CultureInfo.InvariantCulture);
            string savedVertBar = _existingEntity.VertBarLabel ?? "#4";
            int vIdx = Array.FindIndex(RebarOptions, b => b.Label == savedVertBar);
            _selectedVertBarIdx = vIdx >= 0 ? vIdx : 1;

            int hoopCount = _existingEntity.HoopBarCountPerFace ?? 8;
            txtScHoopCount.Text = hoopCount.ToString(CultureInfo.InvariantCulture);
            string savedHoopBar = _existingEntity.HoopBarLabel ?? "#4";
            int hIdx = Array.FindIndex(RebarOptions, b => b.Label == savedHoopBar);
            _selectedHoopBarIdx = hIdx >= 0 ? hIdx : 1;
            UpdateRebarButtonLabels();

            double savedK = _existingEntity.SlendernessK ?? 0.0;
            if (Math.Abs(savedK - 1.0) < 0.05) cmbScSlenderK.SelectedIndex = 1;
            else if (Math.Abs(savedK - 2.0) < 0.05) cmbScSlenderK.SelectedIndex = 2;
            else cmbScSlenderK.SelectedIndex = 0;

            UpdateSingleColumnDerivedGeometry(null, EventArgs.Empty);
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
                double.TryParse(txtScDcl.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dcl);
                double.TryParse(txtScTw.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double tw);
                if (dcl <= tw || tw < 1.0)
                {
                    MessageBox.Show("Ring-wall Centerline Diameter (D_CL) must be greater than thickness (tw), and minimum tw ≥ 1.0 ft per PIP STE03020.", "Invalid Geometry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                double doFt = dcl + tw;
                double diFt = dcl - tw;
                double ro = doFt / 2.0;
                double ri = diFt / 2.0;
                double rcl = dcl / 2.0;
                var eq = new FoundationEquations.RingWallEquations();

                double.TryParse(txtScP.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double scPu);
                double.TryParse(txtScMu.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double scMu);
                double.TryParse(txtScVu.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double scVu);
                double.TryParse(txtScHw.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double hw);
                double.TryParse(txtScFc.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double fcInput);
                double.TryParse(txtScFy.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double fyKsi);
                double.TryParse(txtScGammaC.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gammaC);
                double.TryParse(txtScCover.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double coverIn);
                double.TryParse(txtScQa.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double qaKsf);
                double.TryParse(txtScGammaSoil.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gammaSoil);
                double.TryParse(txtScKa.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ka);

                double bf = 6.5, hf = 3.0;
                var bfhfParts = txtScBfHf.Text.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (bfhfParts.Length >= 1) double.TryParse(bfhfParts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out bf);
                if (bfhfParts.Length >= 2) double.TryParse(bfhfParts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out hf);

                int.TryParse(txtScVertCount.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int vertCount);
                if (vertCount <= 0) vertCount = (int)Math.Round(Math.PI * dcl);
                int vIdx = (_selectedVertBarIdx >= 0 && _selectedVertBarIdx < RebarOptions.Length) ? _selectedVertBarIdx : 1;
                var vOpt = RebarOptions[vIdx];

                int.TryParse(txtScHoopCount.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int hoopCount);
                if (hoopCount <= 0) hoopCount = 8;
                int hIdx = (_selectedHoopBarIdx >= 0 && _selectedHoopBarIdx < RebarOptions.Length) ? _selectedHoopBarIdx : 1;
                var hOpt = RebarOptions[hIdx];

                double fcPsi = fcInput > 100 ? fcInput : (fcInput > 0 ? fcInput * 1000.0 : 4000.0);
                double hwFinal = hw > 0 ? hw : 9.0;
                double shIn = eq.HorizontalTributarySpacingIn(hwFinal, hoopCount);

                _existingEntity.ScDcl = dcl;
                _existingEntity.Rin = ri;
                _existingEntity.Rout = ro;
                _existingEntity.Trw = tw;
                _existingEntity.B = bf > 0 ? bf : 6.5;
                _existingEntity.Tedge = hf > 0 ? hf : 3.0;
                _existingEntity.Rcl = rcl;
                _existingEntity.Rrw = rcl;
                _existingEntity.Cc = coverIn > 0 ? coverIn : 2.0;
                _existingEntity.A = eq.RingWallCrossSectionalAreaFt2(doFt, diFt);

                _existingEntity.ScPu = scPu;
                _existingEntity.ScMu = scMu;
                _existingEntity.ScVu = scVu;
                _existingEntity.Hw = hwFinal;
                _existingEntity.ScBf = bf > 0 ? bf : 6.5;
                _existingEntity.ScHf = hf > 0 ? hf : 3.0;
                _existingEntity.ScFcPrime = fcPsi;
                _existingEntity.ScFy = fyKsi > 0 ? fyKsi : 60.0;
                _existingEntity.ScGammaC = gammaC > 0 ? gammaC : 150.0;
                _existingEntity.ScQa = qaKsf > 0 ? qaKsf : 4.0;
                _existingEntity.ScGammaSoil = gammaSoil > 0 ? gammaSoil : 125.0;
                _existingEntity.ScKa = ka > 0 ? ka : 0.30;

                _existingEntity.VertBarLabel = vOpt.Label;
                _existingEntity.VertBarArea = vOpt.Area;
                _existingEntity.VertBarCountPerFace = vertCount;
                _existingEntity.VertRebarCallout = $"{vertCount} {vOpt.Label} EF";

                _existingEntity.HoopBarLabel = hOpt.Label;
                _existingEntity.HoopBarArea = hOpt.Area;
                _existingEntity.HoopBarCountPerFace = hoopCount;
                _existingEntity.HoopSpacingProv = shIn;

                _existingEntity.SlendernessK = cmbScSlenderK.SelectedIndex == 1 ? 1.0 : (cmbScSlenderK.SelectedIndex == 2 ? 2.0 : 0.0);
            }
            _context.SaveChanges();
            if (AppState.CurrentTankType != TankType.MultiColumn)
            {
                MessageBox.Show("Ring wall foundation data saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateRebarButtonLabels()
        {
            if (btnScVertBar != null && _selectedVertBarIdx >= 0 && _selectedVertBarIdx < RebarOptions.Length)
            {
                btnScVertBar.Text = $"{RebarOptions[_selectedVertBarIdx].Label} (Select)";
            }
            if (btnScHoopBar != null && _selectedHoopBarIdx >= 0 && _selectedHoopBarIdx < RebarOptions.Length)
            {
                btnScHoopBar.Text = $"{RebarOptions[_selectedHoopBarIdx].Label} (Select)";
            }
        }

        private void OpenRebarSelectionDialog(bool isVertical)
        {
            int currentIdx = isVertical ? _selectedVertBarIdx : _selectedHoopBarIdx;
            string roleTitle = isVertical ? "Vertical Bars (Each Face)" : "Horizontal Hoop Bars (Each Face)";

            using (Form dlg = new Form())
            {
                dlg.Text = $"Select Reinforcing Bar Size — {roleTitle}";
                dlg.Size = new Size(680, 390);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Color.White;
                dlg.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

                Label lblHeader = new Label
                {
                    Text = $"ACI 318 / ASTM A615 Standard Reinforcing Bar Table — {roleTitle}",
                    Location = new Point(14, 12),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(25, 55, 109)
                };
                dlg.Controls.Add(lblHeader);

                Label lblSub = new Label
                {
                    Text = "Bar numbers represent bar diameter in eighths of an inch (1/8 in). Select a row and click 'Select Bar' (or double-click):",
                    Location = new Point(14, 34),
                    AutoSize = true,
                    ForeColor = Color.DimGray
                };
                dlg.Controls.Add(lblSub);

                DataGridView dgv = new DataGridView
                {
                    Location = new Point(14, 58),
                    Size = new Size(636, 178),
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                    ColumnHeadersHeight = 28,
                    RowTemplate = { Height = 24 },
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 240, 245);
                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 228, 247);
                dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
                dgv.EnableHeadersVisualStyles = false;

                dgv.Columns.Add("BarSize", "Dropdown Selection");
                dgv.Columns.Add("BarDiam", "Bar Diameter (in)");
                dgv.Columns.Add("BarArea", "Cross-Sectional Area Ab (in²)");
                dgv.Columns.Add("TypicalUse", "Typical Use");

                dgv.Columns[0].FillWeight = 18;
                dgv.Columns[1].FillWeight = 22;
                dgv.Columns[2].FillWeight = 25;
                dgv.Columns[3].FillWeight = 35;

                for (int i = 0; i < RebarOptions.Length; i++)
                {
                    var opt = RebarOptions[i];
                    string labelDisplay = i == 1 ? $"{opt.Label} (Default)" : opt.Label;
                    dgv.Rows.Add(labelDisplay, opt.DiameterDesc, $"{opt.Area:0.00} in²", opt.TypicalUse);
                }

                if (currentIdx >= 0 && currentIdx < dgv.Rows.Count)
                {
                    dgv.ClearSelection();
                    dgv.Rows[currentIdx].Selected = true;
                    dgv.CurrentCell = dgv.Rows[currentIdx].Cells[0];
                }

                dlg.Controls.Add(dgv);

                Label lblGuide = new Label
                {
                    Text = isVertical
                        ? "Selection Guide (Vertical Bars): Typically spaced at 12 in. o.c. around the ring-wall circumference (Nv ≈ π × D_CL = 66 bars for D_CL = 21 ft).\nACI 318-19 requires minimum vertical steel As,min = 0.0012 × b × tw. #4 bars (0.20 in² EF = 0.40 in²/ft) is standard."
                        : "Selection Guide (Horizontal Hoop Bars): Distributed over wall height Hw. ACI 318-19 / PIP STE03020 requires minimum horizontal steel\nAsh,min = 0.00225 × b × tw. If the results window shows 'NG - INCREASE', increase bar count or select #5 / #6.",
                    Location = new Point(14, 244),
                    Size = new Size(636, 52),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(8, 6, 8, 6),
                    Font = new Font("Segoe UI", 8.25F, FontStyle.Regular)
                };
                dlg.Controls.Add(lblGuide);

                Button btnSelect = new Button
                {
                    Text = "Select Bar",
                    Location = new Point(460, 306),
                    Size = new Size(90, 30),
                    BackColor = Color.FromArgb(25, 118, 210),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnSelect.Click += (s, e) =>
                {
                    if (dgv.SelectedRows.Count > 0)
                    {
                        int chosen = dgv.SelectedRows[0].Index;
                        if (isVertical) _selectedVertBarIdx = chosen;
                        else _selectedHoopBarIdx = chosen;
                        UpdateRebarButtonLabels();
                    }
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(560, 306),
                    Size = new Size(90, 30)
                };
                btnCancel.Click += (s, e) =>
                {
                    dlg.DialogResult = DialogResult.Cancel;
                    dlg.Close();
                };

                dgv.CellDoubleClick += (s, e) =>
                {
                    if (e.RowIndex >= 0 && e.RowIndex < RebarOptions.Length)
                    {
                        if (isVertical) _selectedVertBarIdx = e.RowIndex;
                        else _selectedHoopBarIdx = e.RowIndex;
                        UpdateRebarButtonLabels();
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                dlg.Controls.Add(btnSelect);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnSelect;
                dlg.CancelButton = btnCancel;
                dlg.ShowDialog(this);
            }
        }
    }
}
