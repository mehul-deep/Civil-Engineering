using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class BasePlateParameters : Form
    {
        private readonly WaterTankDbContext _context;
        private PictureBox _picBoltDiagram = null!;
        private PictureBox _pic99 = null!;
        private Image? _loadedImg99;

        public BasePlateEntity? SavedBasePlate { get; private set; }

        // existing row from DB
        private BasePlateEntity? _existingBasePlate;

        public BasePlateParameters()
        {
            InitializeComponent();
            SetupMultiColumnUI();
            SetupBoltDiagram();

            _context = WaterTankDbContext.GetInstance();

            this.Load += BasePlateParameters_Load;
            textBox1.TextChanged += UpdateDerivedRadii;
            textBox16.TextChanged += UpdateDerivedRadii;
            textBox6.TextChanged += UpdateDerivedBolts;
            textBox17.TextChanged += UpdateDerivedBolts;
        }

        private void SetupMultiColumnUI()
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                this.Text = "Multi-Leg Base Plate Parameters";
                // Hide circular standpipe specific inputs
                label5.Visible = textBox4.Visible = false; // Segment Angle (Theta)
                label6.Visible = textBox6.Visible = false; // No of Segment (N)
                label9.Visible = textBox8.Visible = false; // Start Angle (A)
                label10.Visible = textBox9.Visible = false; // Bolt Circle Radius (Rb)
                label7.Visible = textBox10.Visible = false; // No of Bolt hole in one segment (Nh)
                label16.Visible = textBox16.Visible = false; // Width of ring wall (Wrw)

                // Update labels for Multi-Column rectangular base plate
                label1.Text = "Pipe Diameter (Dpip) (in)";
                label2.Text = "Plate Width B (in)";
                label3.Text = "Plate Length N (in)";
                label4.Text = "Plate Thickness (in)";
                label8.Text = "Anchor Bolt Diam (db) (in)";
                label17.Text = "Total Bolts per Pedestal";

                // Re-position Left Column items to remove empty gaps
                label4.Location = new Point(label1.Location.X, 131);
                textBox5.Location = new Point(textBox1.Location.X, 128);

                label11.Location = new Point(label1.Location.X, 164);
                textBox11.Location = new Point(textBox1.Location.X, 161);

                label12.Location = new Point(label1.Location.X, 205);
                textBox12.Location = new Point(textBox1.Location.X, 202);

                // Re-position Right Column items to remove empty gaps and align with Left Column
                label8.Location = new Point(270, 32);
                textBox7.Location = new Point(410, 29);

                label14.Location = new Point(270, 65);
                textBox14.Location = new Point(410, 62);

                label15.Location = new Point(270, 98);
                textBox15.Location = new Point(410, 95);

                label17.Location = new Point(270, 131);
                textBox17.Location = new Point(410, 128);
            }
        }

        private void UpdateDerivedRadii(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn) return;
            if (double.TryParse(textBox1.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dbp) &&
                double.TryParse(textBox16.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double wrw))
            {
                double ro = (dbp / 2.0) + (wrw / 4.0);
                double ri = (dbp / 2.0) - (wrw / 4.0);
                textBox2.Text = ro.ToString("F4", CultureInfo.InvariantCulture);
                textBox3.Text = ri.ToString("F4", CultureInfo.InvariantCulture);
            }
        }

        private void UpdateDerivedBolts(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn) return;
            if (int.TryParse(textBox17.Text.Trim(), out int nb) &&
                int.TryParse(textBox6.Text.Trim(), out int n) && n > 0)
            {
                int nh = (int)Math.Round((double)nb / n, MidpointRounding.AwayFromZero);
                if (nh < 1) nh = 1;
                textBox10.Text = nh.ToString();
            }
        }

        private void BasePlateParameters_Load(object? sender, EventArgs e)
        {
            LoadExistingData();
        }

        private void LoadExistingData()
        {
            try
            {
                _existingBasePlate = _context.BasePlateEntity.FirstOrDefault();

                // Fetch governing loads from LoadService
                var loadService = Program.GetContainer().GetService<Services.LoadService>();
                var (Pu, Mu, Vu) = loadService.GetGoverningLoads();

                // Fetch bottom-most segment diameter
                double bottomDiameter = 0;
                var bottomSegment = _context.SegmentProperties.OrderBy(s => s.HeightInitial).FirstOrDefault();
                if (bottomSegment != null)
                {
                    bottomDiameter = bottomSegment.DiameterFinal ?? bottomSegment.Diameter;
                }

                // Fetch existing Anchor Bolt input to auto-sync common parameters
                var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();

                int autoNb = (anchorBolt != null && anchorBolt.Nb > 0) ? anchorBolt.Nb : 20;
                double autoWrw = 2.0;

                if (_existingBasePlate == null)
                {
                    // For a new record, auto-populate Mu and Pu
                    textBox15.Text = Mu.ToString(CultureInfo.InvariantCulture);
                    textBox14.Text = Pu.ToString(CultureInfo.InvariantCulture);
                    
                    if (AppState.CurrentTankType == TankType.MultiColumn)
                    {
                        double pipeDiam = bottomDiameter > 10 ? bottomDiameter : (bottomDiameter > 0 ? bottomDiameter * 12.0 : 20.04);
                        textBox1.Text = pipeDiam.ToString("F2", CultureInfo.InvariantCulture);
                        textBox2.Text = "30";
                        textBox3.Text = "30";
                        textBox5.Text = "1.50";
                        textBox11.Text = "36";
                        textBox12.Text = "4000";
                    }
                    else
                    {
                        textBox1.Text = bottomDiameter > 0 ? bottomDiameter.ToString(CultureInfo.InvariantCulture) : "21.0";
                    }
                    textBox16.Text = autoWrw.ToString(CultureInfo.InvariantCulture);
                    textBox17.Text = autoNb.ToString();
                    if (anchorBolt != null && anchorBolt.Dh > 0)
                        textBox7.Text = anchorBolt.Dh.ToString(CultureInfo.InvariantCulture);
                    if (anchorBolt != null && anchorBolt.Rb > 0)
                        textBox9.Text = anchorBolt.Rb.ToString(CultureInfo.InvariantCulture);
                    return;
                }

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    double pipeDiam = bottomDiameter > 10 ? bottomDiameter : (bottomDiameter > 0 ? bottomDiameter * 12.0 : 20.04);
                    textBox1.Text = (_existingBasePlate.Dbp == 0 ? pipeDiam : _existingBasePlate.Dbp).ToString(CultureInfo.InvariantCulture);
                    textBox2.Text = (_existingBasePlate.Ro == 0 ? 30 : _existingBasePlate.Ro).ToString(CultureInfo.InvariantCulture);
                    textBox3.Text = (_existingBasePlate.Ri == 0 ? 30 : _existingBasePlate.Ri).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    textBox1.Text = (_existingBasePlate.Dbp == 0 ? bottomDiameter : _existingBasePlate.Dbp).ToString(CultureInfo.InvariantCulture);
                    textBox2.Text = _existingBasePlate.Ro.ToString(CultureInfo.InvariantCulture);
                    textBox3.Text = _existingBasePlate.Ri.ToString(CultureInfo.InvariantCulture);
                }
                textBox4.Text = _existingBasePlate.Theta.ToString(CultureInfo.InvariantCulture);
                textBox5.Text = _existingBasePlate.T.ToString(CultureInfo.InvariantCulture);

                textBox6.Text = _existingBasePlate.N.ToString();
                textBox7.Text = (_existingBasePlate.Dh == 0 && anchorBolt != null && anchorBolt.Dh > 0)
                    ? anchorBolt.Dh.ToString(CultureInfo.InvariantCulture)
                    : _existingBasePlate.Dh.ToString(CultureInfo.InvariantCulture);
                textBox8.Text = _existingBasePlate.A?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox9.Text = (_existingBasePlate.Rb == null && anchorBolt != null && anchorBolt.Rb > 0)
                    ? anchorBolt.Rb.ToString(CultureInfo.InvariantCulture)
                    : _existingBasePlate.Rb?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox10.Text = _existingBasePlate.Nh.ToString();

                double wrwVal = _existingBasePlate.Wrw ?? 2.0 * (_existingBasePlate.Ro - _existingBasePlate.Ri);
                if (wrwVal <= 0) wrwVal = autoWrw;

                // Grab Nb automatically from Anchor Bolt input if defined, otherwise fall back to Base Plate record
                int nbVal = (anchorBolt != null && anchorBolt.Nb > 0) ? anchorBolt.Nb : (_existingBasePlate.Nb ?? (_existingBasePlate.N * _existingBasePlate.Nh));
                if (nbVal <= 0) nbVal = autoNb;

                textBox16.Text = wrwVal.ToString(CultureInfo.InvariantCulture);
                textBox17.Text = nbVal.ToString();

                // Structural Parameters
                textBox11.Text = _existingBasePlate.Fy.ToString(CultureInfo.InvariantCulture);
                textBox12.Text = _existingBasePlate.Fc_prime.ToString(CultureInfo.InvariantCulture);

                // Always auto-update Pu and Mu with the latest calculations from LoadService
                textBox14.Text = Pu.ToString(CultureInfo.InvariantCulture);
                textBox15.Text = Mu.ToString(CultureInfo.InvariantCulture);

                SavedBasePlate = _existingBasePlate;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load base plate data.\n{ex.Message}",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                bool isNew = _existingBasePlate == null;
                var entity = _existingBasePlate ?? new BasePlateEntity();

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    entity.Dbp = ParseDoubleRequired(textBox1, "Pipe Diameter");
                    entity.Ro = ParseDoubleRequired(textBox2, "Plate Width B");
                    entity.Ri = ParseDoubleRequired(textBox3, "Plate Length N");
                    entity.T = ParseDoubleRequired(textBox5, "Thickness");
                    entity.Dh = ParseDoubleRequired(textBox7, "Anchor Bolt Diameter");
                    entity.Nb = ParseIntNullable(textBox17);
                    
                    entity.Theta = 360;
                    entity.N = 1;
                    entity.Nh = entity.Nb ?? 4;
                    entity.Wrw = 0;
                    entity.A = 0;
                    entity.Rb = 0;
                }
                else
                {
                    entity.Dbp = ParseDoubleRequired(textBox1, "Diameter");
                    entity.Ro = ParseDoubleRequired(textBox2, "Outside Radius");
                    entity.Ri = ParseDoubleRequired(textBox3, "Inside Radius");
                    entity.Theta = ParseDoubleRequired(textBox4, "Segment Angle");
                    entity.T = ParseDoubleRequired(textBox5, "Thickness");

                    entity.N = ParseIntRequired(textBox6, "No of Segment");
                    entity.Dh = ParseDoubleRequired(textBox7, "Bolt Hole Diameter");
                    entity.A = ParseDoubleNullable(textBox8);
                    entity.Rb = ParseDoubleNullable(textBox9);
                    entity.Nh = ParseIntRequired(textBox10, "No of Bolt hole in one segment");
                    entity.Wrw = ParseDoubleNullable(textBox16);
                    entity.Nb = ParseIntNullable(textBox17);
                }

                // Structural Parameters
                entity.Fy = ParseDoubleRequired(textBox11, "Steel Yield (Fy)");
                entity.Fc_prime = ParseDoubleRequired(textBox12, "Concrete Strength (f'c)");
                entity.A2 = null; // Derived from ring wall width
                entity.Pu = ParseDoubleNullable(textBox14);
                entity.OverturningMoment = ParseDoubleNullable(textBox15);

                // fixed value from design input
                entity.Rs = 490;

                if (isNew)
                {
                    _context.BasePlateEntity.Add(entity);
                }

                _context.SaveChanges();

                _existingBasePlate = entity;
                SavedBasePlate = entity;

                MessageBox.Show(
                    isNew ? "Base plate data saved successfully."
                          : "Base plate data updated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int ParseIntRequired(TextBox textBox, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                throw new Exception($"{fieldName} is required.");

            if (!int.TryParse(textBox.Text.Trim(), out int value))
            {
                textBox.Focus();
                throw new Exception($"{fieldName} must be a valid integer.");
            }

            return value;
        }

        private double ParseDoubleRequired(TextBox textBox, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                throw new Exception($"{fieldName} is required.");

            if (!double.TryParse(textBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                !double.TryParse(textBox.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                textBox.Focus();
                throw new Exception($"{fieldName} must be a valid number.");
            }

            return value;
        }

        private double? ParseDoubleNullable(TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                return null;

            if (!double.TryParse(textBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                !double.TryParse(textBox.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                textBox.Focus();
                throw new Exception($"Invalid numeric value entered in {textBox.Name}.");
            }

            return value;
        }

        private int? ParseIntNullable(TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                return null;

            if (!int.TryParse(textBox.Text.Trim(), out int value))
            {
                textBox.Focus();
                throw new Exception($"Invalid integer value entered in {textBox.Name}.");
            }

            return value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SetupBoltDiagram()
        {
            GroupBox groupBoxBoltDiagram = new GroupBox
            {
                Location = new Point(8, 330),
                Size = new Size(345, 240),
                Text = "Base Plate Geometry & Bolt Arrangement Preview"
            };

            _picBoltDiagram = new PictureBox
            {
                Location = new Point(5, 15),
                Size = new Size(335, 220),
                BackColor = Color.Transparent
            };
            _picBoltDiagram.Paint += PicBoltDiagram_Paint;

            groupBoxBoltDiagram.Controls.Add(_picBoltDiagram);
            this.Controls.Add(groupBoxBoltDiagram);

            // Trigger repaint whenever relevant inputs change
            textBox17.TextChanged += (s, e) => _picBoltDiagram.Invalidate();
            textBox1.TextChanged += (s, e) => _picBoltDiagram.Invalidate();
            textBox2.TextChanged += (s, e) => _picBoltDiagram.Invalidate();
            textBox3.TextChanged += (s, e) => _picBoltDiagram.Invalidate();
            textBox9.TextChanged += (s, e) => _picBoltDiagram.Invalidate();
            textBox16.TextChanged += (s, e) => _picBoltDiagram.Invalidate();

            GroupBox groupBoxPic99 = new GroupBox
            {
                Location = new Point(358, 330),
                Size = new Size(250, 240),
                Text = "Base Plate Details"
            };

            _pic99 = new PictureBox
            {
                Location = new Point(5, 15),
                Size = new Size(240, 220),
                BackColor = Color.Transparent
            };

            string pic99Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 99.png");
            if (System.IO.File.Exists(pic99Path))
            {
                _loadedImg99 = Image.FromFile(pic99Path);
            }
            _pic99.Paint += Pic99_Paint;
            textBox5.TextChanged += (s, e) => _pic99.Invalidate();

            groupBoxPic99.Controls.Add(_pic99);
            this.Controls.Add(groupBoxPic99);
        }

        private void PicBoltDiagram_Paint(object? sender, PaintEventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                Graphics gMulti = e.Graphics;
                gMulti.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float cxMulti = 105f;
                float cyMulti = 105f;
                float halfP = 85f; // Pedestal half-size
                float halfB = 65f; // Base plate half-size
                float halfPipe = 35f; // Pipe column half-size

                // Draw Pedestal (Gray/Concrete)
                using (Brush bPed = new SolidBrush(Color.FromArgb(50, 150, 150, 150)))
                {
                    gMulti.FillRectangle(bPed, cxMulti - halfP, cyMulti - halfP, 2 * halfP, 2 * halfP);
                }
                using (Pen pPed = new Pen(Color.DimGray, 2f))
                {
                    gMulti.DrawRectangle(pPed, cxMulti - halfP, cyMulti - halfP, 2 * halfP, 2 * halfP);
                }

                // Draw Base Plate (Blue/Steel)
                using (Brush bPlate = new SolidBrush(Color.FromArgb(80, 70, 130, 180)))
                {
                    gMulti.FillRectangle(bPlate, cxMulti - halfB, cyMulti - halfB, 2 * halfB, 2 * halfB);
                }
                using (Pen pPlate = new Pen(Color.Navy, 2.2f))
                {
                    gMulti.DrawRectangle(pPlate, cxMulti - halfB, cyMulti - halfB, 2 * halfB, 2 * halfB);
                }

                // Draw Pipe Column (Center Circle)
                using (Brush bPipe = new SolidBrush(Color.LightGray))
                {
                    gMulti.FillEllipse(bPipe, cxMulti - halfPipe, cyMulti - halfPipe, 2 * halfPipe, 2 * halfPipe);
                }
                using (Pen pPipe = new Pen(Color.DarkSlateGray, 2f))
                {
                    gMulti.DrawEllipse(pPipe, cxMulti - halfPipe, cyMulti - halfPipe, 2 * halfPipe, 2 * halfPipe);
                }

                // Parse number of bolts (default 4 for 4 corners)
                int nbMulti = 0;
                int.TryParse(textBox17.Text.Trim(), out nbMulti);
                if (nbMulti <= 0) nbMulti = 4;

                float boltOffset = 48f;
                PointF[] boltPositions;
                if (nbMulti == 4)
                {
                    boltPositions = new PointF[] {
                        new PointF(cxMulti - boltOffset, cyMulti - boltOffset),
                        new PointF(cxMulti + boltOffset, cyMulti - boltOffset),
                        new PointF(cxMulti + boltOffset, cyMulti + boltOffset),
                        new PointF(cxMulti - boltOffset, cyMulti + boltOffset)
                    };
                }
                else
                {
                    boltPositions = new PointF[nbMulti];
                    for (int i = 0; i < nbMulti; i++)
                    {
                        double angle = 2 * Math.PI * i / nbMulti + Math.PI / 4.0;
                        boltPositions[i] = new PointF((float)(cxMulti + boltOffset * Math.Cos(angle)), (float)(cyMulti + boltOffset * Math.Sin(angle)));
                    }
                }

                using (Brush bBolt = new SolidBrush(Color.Crimson))
                using (Pen pBolt = new Pen(Color.Black, 1.2f))
                {
                    foreach (var pt in boltPositions)
                    {
                        gMulti.FillEllipse(bBolt, pt.X - 5f, pt.Y - 5f, 10f, 10f);
                        gMulti.DrawEllipse(pBolt, pt.X - 5f, pt.Y - 5f, 10f, 10f);
                    }
                }

                // Annotations & CAD Dimension Lines (matching Page 1 of PDF)
                using (Pen pDim = new Pen(Color.Black, 1.3f))
                using (Pen pCenter = new Pen(Color.DarkSlateGray, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (Font fBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (Brush bText = new SolidBrush(Color.DarkBlue))
                using (Brush bTextRed = new SolidBrush(Color.Crimson))
                using (Brush bTextGray = new SolidBrush(Color.DimGray))
                {
                    // Centerline dashed crosses
                    gMulti.DrawLine(pCenter, cxMulti - halfP - 10f, cyMulti, cxMulti + halfP + 10f, cyMulti);
                    gMulti.DrawLine(pCenter, cxMulti, cyMulti - halfP - 10f, cxMulti, cyMulti + halfP + 10f);

                    // Top Horizontal Dimension Line (Length N)
                    float topDimY = cyMulti - halfB - 10f;
                    gMulti.DrawLine(pDim, cxMulti - halfB, topDimY, cxMulti + halfB, topDimY);
                    gMulti.DrawLine(pDim, cxMulti - halfB, topDimY - 4f, cxMulti - halfB, topDimY + 4f);
                    gMulti.DrawLine(pDim, cxMulti + halfB, topDimY - 4f, cxMulti + halfB, topDimY + 4f);
                    string nVal = textBox3.Text.Trim();
                    gMulti.DrawString(string.IsNullOrEmpty(nVal) ? "N" : $"N = {nVal}\"", fBold, bText, cxMulti - 20f, topDimY - 16f);

                    // Left Vertical Dimension Line (Width B)
                    float leftDimX = cxMulti - halfB - 10f;
                    gMulti.DrawLine(pDim, leftDimX, cyMulti - halfB, leftDimX, cyMulti + halfB);
                    gMulti.DrawLine(pDim, leftDimX - 4f, cyMulti - halfB, leftDimX + 4f, cyMulti - halfB);
                    gMulti.DrawLine(pDim, leftDimX - 4f, cyMulti + halfB, leftDimX + 4f, cyMulti + halfB);
                    string bVal = textBox2.Text.Trim();
                    gMulti.DrawString(string.IsNullOrEmpty(bVal) ? "B" : $"B = {bVal}\"", fBold, bText, 5f, cyMulti - 8f);

                    // Center Pipe Column Label
                    string dVal = textBox1.Text.Trim();
                    string pipeText = string.IsNullOrEmpty(dVal) ? "PIPE D" : $"PIPE D={dVal}\"";
                    gMulti.DrawString(pipeText, fBold, bText, cxMulti - 42f, cyMulti - 8f);

                    // Anchor Bolts Leader Line & Label
                    string dbVal = textBox7.Text.Trim();
                    string boltText = $"ANCHOR BOLTS (db={dbVal}\", Nb={nbMulti})";
                    gMulti.DrawLine(pDim, cxMulti + boltOffset + 4f, cyMulti - boltOffset - 4f, cxMulti + boltOffset + 16f, cyMulti - boltOffset - 16f);
                    gMulti.DrawLine(pDim, cxMulti + boltOffset + 16f, cyMulti - boltOffset - 16f, cxMulti + boltOffset + 125f, cyMulti - boltOffset - 16f);
                    gMulti.DrawString(boltText, fBold, bTextRed, cxMulti + boltOffset + 15f, cyMulti - boltOffset - 32f);

                    // Bottom Pedestal Label
                    gMulti.DrawString("PEDESTAL P x L", fBold, bTextGray, cxMulti - 45f, cyMulti + halfP + 4f);
                }
                return;
            }
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float cx = 105f;
            float cy = 105f;
            float rOuter = 85f;
            float rBolt = 65f;
            float rInner = 40f;

            // Draw Shaded Plate Area (between inner and outer circle)
            using (Brush bPlate = new SolidBrush(Color.FromArgb(35, 70, 130, 180)))
            {
                g.FillEllipse(bPlate, cx - rOuter, cy - rOuter, 2 * rOuter, 2 * rOuter);
            }
            using (Brush bHole = new SolidBrush(this.BackColor))
            {
                g.FillEllipse(bHole, cx - rInner, cy - rInner, 2 * rInner, 2 * rInner);
            }

            // Draw Outer Circle
            using (Pen pOuter = new Pen(Color.Navy, 2.2f))
            {
                g.DrawEllipse(pOuter, cx - rOuter, cy - rOuter, 2 * rOuter, 2 * rOuter);
            }

            // Draw Inner Circle
            using (Pen pInner = new Pen(Color.Navy, 2.2f))
            {
                g.DrawEllipse(pInner, cx - rInner, cy - rInner, 2 * rInner, 2 * rInner);
            }

            // Draw Bolt Circle (dashed)
            using (Pen pBoltCircle = new Pen(Color.DimGray, 1.2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawEllipse(pBoltCircle, cx - rBolt, cy - rBolt, 2 * rBolt, 2 * rBolt);
            }

            // Parse number of bolts
            int nb = 0;
            int.TryParse(textBox17.Text.Trim(), out nb);
            int drawNb = nb > 0 ? Math.Min(nb, 120) : 12;
            Color dotColor = nb > 0 ? Color.Crimson : Color.LightGray;

            // Draw Bolt Dots
            for (int i = 0; i < drawNb; i++)
            {
                double angle = 2 * Math.PI * i / drawNb;
                float bx = (float)(cx + rBolt * Math.Cos(angle));
                float by = (float)(cy + rBolt * Math.Sin(angle));
                float dotR = nb > 0 ? 4.5f : 3f;

                using (Brush bBolt = new SolidBrush(dotColor))
                {
                    g.FillEllipse(bBolt, bx - dotR, by - dotR, 2 * dotR, 2 * dotR);
                }
                using (Pen pDotBorder = new Pen(Color.Black, 1f))
                {
                    g.DrawEllipse(pDotBorder, bx - dotR, by - dotR, 2 * dotR, 2 * dotR);
                }
            }

            // Draw Diameter Dimension Line (Dcone)
            float leftBoltX = cx - rBolt;
            float rightBoltX = cx + rBolt;
            float yDim = 210f;

            using (Pen pDim = new Pen(Color.Black, 1.3f))
            using (Font fBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Brush bText = new SolidBrush(Color.DarkBlue))
            using (Brush bTextRed = new SolidBrush(Color.Crimson))
            using (Brush bNavy = new SolidBrush(Color.Navy))
            {
                // Vertical extension lines from middle of left and right bolts down to yDim + 22
                g.DrawLine(pDim, leftBoltX, cy, leftBoltX, yDim + 22f);
                g.DrawLine(pDim, rightBoltX, cy, rightBoltX, yDim + 22f);

                // Horizontal line connecting them
                g.DrawLine(pDim, leftBoltX, yDim, rightBoltX, yDim);

                // End ticks
                g.DrawLine(pDim, leftBoltX, yDim - 5f, leftBoltX, yDim + 5f);
                g.DrawLine(pDim, rightBoltX, yDim - 5f, rightBoltX, yDim + 5f);

                // Text label: Dcone = <Diameter> (placed above the horizontal line)
                string diamVal = textBox1.Text.Trim();
                string dconeText = string.IsNullOrEmpty(diamVal) ? "Dcone =" : $"Dcone = {diamVal} ft";
                SizeF textSize = g.MeasureString(dconeText, fBold);
                g.DrawString(dconeText, fBold, bText, cx - (textSize.Width / 2f), yDim - textSize.Height - 2f);

                // Leader line and label for Bolts (Nb)
                g.DrawLine(pDim, rightBoltX, cy, 200f, cy);
                g.FillEllipse(Brushes.Crimson, rightBoltX - 2.5f, cy - 2.5f, 5f, 5f);
                string boltLabel = nb > 0 ? $"Bolts (Nb = {nb})" : "Bolts (Nb)";
                g.DrawString(boltLabel, fBold, bTextRed, 203f, cy - 8f);

                // Line touching inside circle to outside circle for Wrw (at -45 degrees top-right)
                double wrwAngle = -Math.PI / 4.0;
                float xin = (float)(cx + rInner * Math.Cos(wrwAngle));
                float yin = (float)(cy + rInner * Math.Sin(wrwAngle));
                float xout = (float)(cx + rOuter * Math.Cos(wrwAngle));
                float yout = (float)(cy + rOuter * Math.Sin(wrwAngle));

                using (Pen pWrw = new Pen(Color.Navy, 2f))
                {
                    g.DrawLine(pWrw, xin, yin, xout, yout);
                }
                g.FillEllipse(bNavy, xin - 2.5f, yin - 2.5f, 5f, 5f);
                g.FillEllipse(bNavy, xout - 2.5f, yout - 2.5f, 5f, 5f);

                float midX = (xin + xout) / 2f;
                float midY = (yin + yout) / 2f;
                g.DrawLine(pDim, midX, midY, 200f, midY);
                g.FillEllipse(bNavy, midX - 2f, midY - 2f, 4f, 4f);

                string wrwVal = textBox16.Text.Trim();
                string wrwText = string.IsNullOrEmpty(wrwVal) ? "Wrw =" : $"Wrw = {wrwVal} ft";
                g.DrawString(wrwText, fBold, bNavy, 203f, midY - 8f);
            }
        }

        private void Pic99_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_loadedImg99 != null)
            {
                g.DrawImage(_loadedImg99, new RectangleF(10f, 0f, 126f, 220f));
            }

            // In Pic 99.png (scaled to 126x220 and drawn at X=10), the blue base plate is located around Y=34.3f to 40.3f, X=45f to 100.7f.
            float yTop = 34.3f;
            float yBottom = 40.3f;
            float xRightEdge = 100.7f;

            using (Pen pDim = new Pen(Color.Black, 1.3f))
            using (Font fBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Brush bText = new SolidBrush(Color.DarkBlue))
            {
                // Draw 2 horizontal extension lines extending to the right
                g.DrawLine(pDim, xRightEdge, yTop, 118f, yTop);
                g.DrawLine(pDim, xRightEdge, yBottom, 118f, yBottom);

                // Vertical dimension line connecting them towards the right end
                float dimX = 118f;
                g.DrawLine(pDim, dimX, yTop - 3f, dimX, yBottom + 3f);

                // Ticks at intersection
                g.DrawLine(pDim, dimX - 3f, yTop, dimX + 3f, yTop);
                g.DrawLine(pDim, dimX - 3f, yBottom, dimX + 3f, yBottom);

                // Thickness label as per user input
                string tVal = textBox5.Text.Trim();
                string tText = string.IsNullOrEmpty(tVal) ? "t =" : $"t = {tVal} in";
                g.DrawString(tText, fBold, bText, 123f, ((yTop + yBottom) / 2f) - 8f);
            }
        }

    }
}