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

        // Single-Column controls
        private GroupBox? grpScLoads;
        private TextBox txtScP = null!, txtScMu = null!, txtScT = null!, txtScBp = null!, txtScTp = null!, txtScBpFy = null!, txtScNs = null!, txtScNb = null!, txtScDb = null!, txtScRb = null!;
        private TextBox txtScRi = null!, txtScRo = null!, txtScTw = null!, txtScHw = null!, txtScFc = null!, txtScFy = null!, txtScPhi = null!, txtScGammaC = null!, txtScGammaS = null!, txtScHoopSpacing = null!, txtScVertRebar = null!;
        private ComboBox cmbScHoopBar = null!;
        private bool _scSuppressAutoUplift = false;
        private bool _scUserEditedUplift = false;
        private double _scCurrentMu = 0;

        private static readonly (string Label, double Area)[] HoopBarOptions = new[]
        {
            ("#4", 0.20),
            ("#5", 0.31),
            ("#6", 0.44),
            ("#7", 0.60),
            ("#8", 0.79)
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
            this.Text = "Single-Leg Ring Wall Foundation";
            this.Size = new Size(1175, 505);
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

            // --- Left Group: Inherited Loads, Bolts & Base Plate ---
            grpScLoads = new GroupBox
            {
                Text = "Inherited Loads, Bolts & Base Plate",
                Location = new Point(12, 12),
                Size = new Size(332, 398)
            };
            this.Controls.Add(grpScLoads);

            int yL = 24;
            int gap = 31;
            txtScP = AddScField(grpScLoads, "Vertical Load P (kips):", yL + gap * 0, false, 185, 130);
            txtScMu = AddScField(grpScLoads, "Overturning Moment Mu (k-ft):", yL + gap * 1, false, 185, 130);
            txtScT = AddScField(grpScLoads, "Total Uplift Force T (kips):", yL + gap * 2, false, 185, 130);
            txtScMu.TextChanged += (s, e) =>
            {
                if (double.TryParse(txtScMu.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double muParsed))
                {
                    _scCurrentMu = muParsed;
                    UpdateSingleColumnDerivedGeometry(s, e);
                }
            };
            txtScT.TextChanged += (s, e) =>
            {
                if (!_scSuppressAutoUplift) _scUserEditedUplift = true;
            };

            txtScBp = AddScField(grpScLoads, "Base Plate Width bp (in):", yL + gap * 3, true, 185, 130);
            txtScTp = AddScField(grpScLoads, "Base Plate Thickness tp (in):", yL + gap * 4, true, 185, 130);
            txtScBpFy = AddScField(grpScLoads, "Base Plate Steel Fy (ksi):", yL + gap * 5, true, 185, 130);
            txtScNs = AddScField(grpScLoads, "Base Plate Segments ns:", yL + gap * 6, true, 185, 130);
            txtScNb = AddScField(grpScLoads, "Number of Anchor Bolts Nb:", yL + gap * 7, true, 185, 130);
            txtScDb = AddScField(grpScLoads, "Anchor Bolt Diameter db (in):", yL + gap * 8, true, 185, 130);
            txtScRb = AddScField(grpScLoads, "Bolt Circle Radius rb (ft):", yL + gap * 9, true, 185, 130);

            Label lblAiscNote = new Label
            {
                Text = "Note: Per AISC, minimum ringwall width is tw = 2.0 ft (24 in).\nRingwall footing is designed separately.",
                Location = new Point(12, yL + gap * 10 + 4),
                Size = new Size(308, 36),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            grpScLoads.Controls.Add(lblAiscNote);

            // --- Right Group: Ringwall Geometry, Material & Reinforcement ---
            groupBox2.Text = "Ringwall Geometry, Material & Reinforcement";
            groupBox2.Location = new Point(354, 12);
            groupBox2.Size = new Size(352, 398);

            int yR = 24;
            txtScRi = AddScField(groupBox2, "Ringwall Inner Radius Ri (ft):", yR + gap * 0, false, 195, 140);
            txtScRo = AddScField(groupBox2, "Ringwall Outer Radius Ro (ft):", yR + gap * 1, false, 195, 140);
            txtScTw = AddScField(groupBox2, "Ringwall Thickness tw (ft):", yR + gap * 2, true, 195, 140);
            txtScHw = AddScField(groupBox2, "Ringwall Height hw (ft):", yR + gap * 3, false, 195, 140);
            txtScFc = AddScField(groupBox2, "Concrete Strength f'c (psi):", yR + gap * 4, false, 195, 140);
            txtScFy = AddScField(groupBox2, "Steel Yield Strength fy (ksi):", yR + gap * 5, false, 195, 140);
            txtScPhi = AddScField(groupBox2, "Strength Reduction Factor φ:", yR + gap * 6, false, 195, 140);
            txtScGammaC = AddScField(groupBox2, "Concrete Unit Weight γc (pcf):", yR + gap * 7, false, 195, 140);
            txtScGammaS = AddScField(groupBox2, "Steel Unit Weight γs (pcf):", yR + gap * 8, false, 195, 140);

            // Hoop Rebar Size + Spacing (e.g. #5 @ 6 in)
            Label lblHoop = new Label { Text = "Hoop Rebar & Spacing (in):", Location = new Point(12, yR + gap * 9 + 3), AutoSize = true };
            cmbScHoopBar = new ComboBox
            {
                Location = new Point(195, yR + gap * 9),
                Size = new Size(86, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var opt in HoopBarOptions)
            {
                cmbScHoopBar.Items.Add($"{opt.Label} ({opt.Area:F2})");
            }
            cmbScHoopBar.SelectedIndex = 1; // Default #5 (0.31)
            Label lblAt = new Label { Text = "@", Location = new Point(283, yR + gap * 9 + 3), AutoSize = true };
            txtScHoopSpacing = new TextBox
            {
                Location = new Point(297, yR + gap * 9),
                Size = new Size(38, 23),
                Text = "6"
            };
            groupBox2.Controls.Add(lblHoop);
            groupBox2.Controls.Add(cmbScHoopBar);
            groupBox2.Controls.Add(lblAt);
            groupBox2.Controls.Add(txtScHoopSpacing);

            txtScVertRebar = AddScField(groupBox2, "Vertical Rebar (each face):", yR + gap * 10, false, 195, 140);

            txtScRi.TextChanged += UpdateSingleColumnDerivedGeometry;
            txtScRo.TextChanged += UpdateSingleColumnDerivedGeometry;

            // --- Far Right Group: Ringwall Section Cut Diagram ---
            GroupBox grpScDiagram = new GroupBox
            {
                Text = "Ringwall Section Detail",
                Location = new Point(716, 12),
                Size = new Size(430, 398)
            };
            PictureBox picScDiagram = new PictureBox
            {
                Location = new Point(8, 20),
                Size = new Size(414, 368),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            string? imgPath = null;
            string directPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "ringwallimage100k.PNG");
            if (System.IO.File.Exists(directPath))
            {
                imgPath = directPath;
            }
            else
            {
                string? dir = AppDomain.CurrentDomain.BaseDirectory;
                while (!string.IsNullOrEmpty(dir))
                {
                    string candidate1 = System.IO.Path.Combine(dir, "Images", "ringwallimage100k.PNG");
                    if (System.IO.File.Exists(candidate1)) { imgPath = candidate1; break; }
                    string candidate2 = System.IO.Path.Combine(dir, "ringwallimage100k.PNG");
                    if (System.IO.File.Exists(candidate2)) { imgPath = candidate2; break; }
                    string candidate3 = System.IO.Path.Combine(dir, "src", "ringwallimage100k.PNG");
                    if (System.IO.File.Exists(candidate3)) { imgPath = candidate3; break; }
                    dir = System.IO.Directory.GetParent(dir)?.FullName;
                }
            }

            if (imgPath != null)
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(imgPath);
                    var ms = new System.IO.MemoryStream(bytes);
                    picScDiagram.Image = Image.FromStream(ms);
                }
                catch { }
            }

            grpScDiagram.Controls.Add(picScDiagram);
            this.Controls.Add(grpScDiagram);

            button1.Location = new Point(495, 424);
            button2.Location = new Point(585, 424);
            this.CenterToScreen();
        }

        private void UpdateSingleColumnDerivedGeometry(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn) return;

            if (double.TryParse(txtScRi.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ri) &&
                double.TryParse(txtScRo.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ro))
            {
                double tw = ro - ri;
                txtScTw.Text = $"{tw:F2} ({tw * 12.0:F1} in)";
                txtScTw.BackColor = tw >= 2.0 ? Color.LightGreen : Color.LightCoral;

                if (!_scUserEditedUplift && ro > ri && _scCurrentMu > 0)
                {
                    double meanR = (ro + ri) / 2.0;
                    var eq = new FoundationEquations.RingWallEquations();
                    double derivedT = eq.DerivedTotalUpliftKips(_scCurrentMu, meanR);
                    _scSuppressAutoUplift = true;
                    txtScT.Text = derivedT.ToString("F2", CultureInfo.InvariantCulture);
                    _scSuppressAutoUplift = false;
                }
            }
            else
            {
                txtScTw.Text = "";
                txtScTw.BackColor = Color.FromArgb(240, 240, 240);
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
            var (puGov, muGov, _) = loadService.GetGoverningLoads();
            var eq = new FoundationEquations.RingWallEquations();

            _existingEntity = _context.Set<RingWallEntity>().FirstOrDefault();

            // Governing vertical load P & overturning moment Mu (live LoadService first, matching BasePlate/AnchorBolt)
            double pVal = puGov > 0
                ? puGov
                : ((_existingEntity?.ScPu.HasValue == true && _existingEntity.ScPu.Value > 0)
                    ? _existingEntity.ScPu.Value
                    : (basePlate?.Pu ?? 0));
            double muVal = muGov > 0
                ? muGov
                : ((_existingEntity?.ScMu.HasValue == true && _existingEntity.ScMu.Value > 0)
                    ? _existingEntity.ScMu.Value
                    : (basePlate?.OverturningMoment ?? anchorBolt?.Mu ?? 0));
            _scCurrentMu = muVal;

            // Base Cone Diameter -> Default Centerline Radius R = Dcone / 2
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
                    coneDiamFt = 2.0 * anchorBolt.Rb;
            }

            double defaultRcl = coneDiamFt / 2.0;
            double defaultTw = (basePlate?.Wrw.HasValue == true && basePlate.Wrw.Value >= 2.0) ? basePlate.Wrw.Value : 2.0;
            double defaultRi = defaultRcl - (defaultTw / 2.0);
            double defaultRo = defaultRcl + (defaultTw / 2.0);

            // Inherited Base Plate & Anchor Bolt display values
            double bpWidthIn = (basePlate != null && basePlate.Ro > basePlate.Ri) ? (basePlate.Ro - basePlate.Ri) * 12.0 : 12.0;
            double bpThkIn = (basePlate != null && basePlate.T > 0) ? basePlate.T : ((anchorBolt != null && anchorBolt.Tbp > 0) ? anchorBolt.Tbp : 1.0);
            double bpFy = (basePlate != null && basePlate.Fy > 0) ? basePlate.Fy : 36.0;
            int nsVal = (basePlate != null && basePlate.N > 0) ? basePlate.N : (anchorBolt?.Ns ?? 4);
            int nbVal = (anchorBolt != null && anchorBolt.Nb > 0) ? anchorBolt.Nb : (basePlate?.Nb ?? 20);
            double dbVal = (anchorBolt != null && anchorBolt.Db > 0) ? anchorBolt.Db : 1.5;
            double rbVal = (anchorBolt != null && anchorBolt.Rb > 0) ? anchorBolt.Rb : (basePlate?.Rb ?? 10.75);

            double inheritedFcPsi = 4000.0;
            if (basePlate != null && basePlate.Fc_prime > 0)
                inheritedFcPsi = basePlate.Fc_prime > 100 ? basePlate.Fc_prime : basePlate.Fc_prime * 1000.0;
            else if (anchorBolt?.FcPrime.HasValue == true && anchorBolt.FcPrime.Value > 0)
                inheritedFcPsi = anchorBolt.FcPrime.Value > 100 ? anchorBolt.FcPrime.Value : anchorBolt.FcPrime.Value * 1000.0;

            _scSuppressAutoUplift = true;
            txtScP.Text = pVal.ToString("F4", CultureInfo.InvariantCulture);
            txtScMu.Text = muVal.ToString("F2", CultureInfo.InvariantCulture);
            _scSuppressAutoUplift = false;
            txtScBp.Text = bpWidthIn.ToString("F2", CultureInfo.InvariantCulture);
            txtScTp.Text = bpThkIn.ToString("F2", CultureInfo.InvariantCulture);
            txtScBpFy.Text = bpFy.ToString("F0", CultureInfo.InvariantCulture);
            txtScNs.Text = nsVal.ToString(CultureInfo.InvariantCulture);
            txtScNb.Text = nbVal.ToString(CultureInfo.InvariantCulture);
            txtScDb.Text = dbVal.ToString("F2", CultureInfo.InvariantCulture);
            txtScRb.Text = rbVal.ToString("F2", CultureInfo.InvariantCulture);

            if (_existingEntity == null || (_existingEntity.Rin <= 0 && _existingEntity.Rout <= 0))
            {
                _scUserEditedUplift = false;
                txtScRi.Text = defaultRi.ToString("0.####", CultureInfo.InvariantCulture);
                txtScRo.Text = defaultRo.ToString("0.####", CultureInfo.InvariantCulture);
                txtScHw.Text = "11.5";
                txtScFc.Text = inheritedFcPsi.ToString("F0", CultureInfo.InvariantCulture);
                txtScFy.Text = "60";
                txtScPhi.Text = "0.90";
                txtScGammaC.Text = "150";
                txtScGammaS.Text = "490";
                cmbScHoopBar.SelectedIndex = 1; // #5 (0.31)
                txtScHoopSpacing.Text = "6";
                txtScVertRebar.Text = "#6 @ 12\"";

                double derivedT = eq.DerivedTotalUpliftKips(muVal, (defaultRi + defaultRo) / 2.0);
                _scSuppressAutoUplift = true;
                txtScT.Text = derivedT.ToString("F2", CultureInfo.InvariantCulture);
                _scSuppressAutoUplift = false;
                UpdateSingleColumnDerivedGeometry(null, EventArgs.Empty);
                return;
            }

            double ri = _existingEntity.Rin > 0 ? _existingEntity.Rin : defaultRi;
            double ro = _existingEntity.Rout > ri ? _existingEntity.Rout : defaultRo;
            double meanR = (ri + ro) / 2.0;
            double autoT = eq.DerivedTotalUpliftKips(muVal, meanR);
            double savedT = (_existingEntity.TotalUpliftT.HasValue && _existingEntity.TotalUpliftT.Value > 0)
                ? _existingEntity.TotalUpliftT.Value
                : autoT;

            _scUserEditedUplift = Math.Abs(savedT - autoT) > 0.05;
            _scSuppressAutoUplift = true;
            txtScT.Text = savedT.ToString("F2", CultureInfo.InvariantCulture);
            _scSuppressAutoUplift = false;

            txtScRi.Text = ri.ToString("0.####", CultureInfo.InvariantCulture);
            txtScRo.Text = ro.ToString("0.####", CultureInfo.InvariantCulture);
            txtScHw.Text = (_existingEntity.Hw ?? 11.5).ToString("0.####", CultureInfo.InvariantCulture);

            double fcSaved = _existingEntity.ScFcPrime ?? inheritedFcPsi;
            if (fcSaved > 0 && fcSaved < 100) fcSaved *= 1000.0;
            txtScFc.Text = fcSaved.ToString("F0", CultureInfo.InvariantCulture);
            txtScFy.Text = (_existingEntity.ScFy ?? 60.0).ToString("0.##", CultureInfo.InvariantCulture);
            txtScPhi.Text = (_existingEntity.ScPhi ?? 0.90).ToString("F2", CultureInfo.InvariantCulture);
            txtScGammaC.Text = (_existingEntity.ScGammaC ?? 150.0).ToString("F0", CultureInfo.InvariantCulture);
            txtScGammaS.Text = (_existingEntity.ScGammaS ?? 490.0).ToString("F0", CultureInfo.InvariantCulture);

            string savedBar = _existingEntity.HoopBarLabel ?? "#5";
            int barIdx = Array.FindIndex(HoopBarOptions, b => b.Label == savedBar);
            cmbScHoopBar.SelectedIndex = barIdx >= 0 ? barIdx : 1;
            txtScHoopSpacing.Text = (_existingEntity.HoopSpacingProv ?? 6.0).ToString("0.##", CultureInfo.InvariantCulture);
            txtScVertRebar.Text = string.IsNullOrWhiteSpace(_existingEntity.VertRebarCallout) ? "#6 @ 12\"" : _existingEntity.VertRebarCallout;

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
                double.TryParse(txtScRi.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ri);
                double.TryParse(txtScRo.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ro);
                if (ro <= ri || ri <= 0)
                {
                    MessageBox.Show("Ringwall Outer Radius (Ro) must be greater than Inner Radius (Ri).", "Invalid Geometry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                double tw = ro - ri;
                double meanR = (ro + ri) / 2.0;
                var eq = new FoundationEquations.RingWallEquations();

                double.TryParse(txtScP.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double scPu);
                double.TryParse(txtScMu.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double scMu);
                double.TryParse(txtScHw.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double hw);
                double.TryParse(txtScT.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double tUplift);
                double.TryParse(txtScFc.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double fcPsi);
                double.TryParse(txtScFy.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double fyKsi);
                double.TryParse(txtScPhi.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double phiVal);
                double.TryParse(txtScGammaC.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gammaC);
                double.TryParse(txtScGammaS.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gammaS);
                double.TryParse(txtScHoopSpacing.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double sProv);

                int barIdx = cmbScHoopBar.SelectedIndex >= 0 ? cmbScHoopBar.SelectedIndex : 1;
                var barOpt = HoopBarOptions[barIdx];

                _existingEntity.Rin = ri;
                _existingEntity.Rout = ro;
                _existingEntity.Trw = tw;
                _existingEntity.B = tw;
                _existingEntity.Rcl = meanR;
                _existingEntity.Rrw = meanR;
                _existingEntity.A = eq.AnnularAreaFt2(ro, ri);

                _existingEntity.ScPu = scPu;
                _existingEntity.ScMu = scMu;
                _existingEntity.Hw = hw > 0 ? hw : 11.5;
                _existingEntity.TotalUpliftT = tUplift;
                _existingEntity.ScFcPrime = fcPsi > 0 ? fcPsi : 4000.0;
                _existingEntity.ScFy = fyKsi > 0 ? fyKsi : 60.0;
                _existingEntity.ScPhi = phiVal > 0 ? phiVal : 0.90;
                _existingEntity.ScGammaC = gammaC > 0 ? gammaC : 150.0;
                _existingEntity.ScGammaS = gammaS > 0 ? gammaS : 490.0;
                _existingEntity.HoopBarLabel = barOpt.Label;
                _existingEntity.HoopBarArea = barOpt.Area;
                _existingEntity.HoopSpacingProv = sProv > 0 ? sProv : 6.0;
                _existingEntity.VertRebarCallout = string.IsNullOrWhiteSpace(txtScVertRebar.Text) ? "#6 @ 12\"" : txtScVertRebar.Text.Trim();
            }
            _context.SaveChanges();
            if (AppState.CurrentTankType != TankType.MultiColumn)
            {
                MessageBox.Show("Ring wall foundation data saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
