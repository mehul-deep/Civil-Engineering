using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class AnchorBoltParameters : Form
    {
        private readonly WaterTankDbContext _context;

        public AnchorBoltEntity? SavedAnchorBolt { get; private set; }

        // holds existing row from DB if present
        private AnchorBoltEntity? _existingAnchorBolt;

        public AnchorBoltParameters()
        {
            InitializeComponent();

            _context = WaterTankDbContext.GetInstance();

            // Initialize ComboBox
            comboBox1.Items.AddRange(new string[] { "Circular Group", "Equal Distribution", "Effective Bolts" });
            comboBox1.SelectedIndex = 0; // Default

            this.Load += AnchorBoltParameters_Load;
        }

        private TextBox txtPedestalSize, txtBoltSpacing, txtWasherSize, txtDcone, txtPu, txtAb;
        private Label lblPedestalSize, lblBoltSpacing, lblWasherSize, lblDcone, lblPu, lblAb;

        private void AnchorBoltParameters_Load(object? sender, EventArgs e)
        {
            SetupMultiColumnUI();
            LoadExistingData();

            // Dynamically align info buttons right next to the labels
            infoButtonHef.Location = new Point(label19.Right + 2, label19.Top - 1);
            infoButtonEdge.Location = new Point(label14.Right + 2, label14.Top - 1);
            
            // Add Pic 4 and Pic 5 dynamically below Edge Distance on the right side
            PictureBox pic4 = new PictureBox();
            pic4.Location = new Point(350, 180);
            pic4.Size = new Size(100, 95);
            pic4.SizeMode = PictureBoxSizeMode.Zoom;
            string pic4Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 4.png");
            if (System.IO.File.Exists(pic4Path)) pic4.Image = Image.FromFile(pic4Path);
            groupBox1.Controls.Add(pic4);

            Label lblDbTop = new Label();
            lblDbTop.AutoSize = false;
            lblDbTop.Size = new Size(100, 15);
            lblDbTop.TextAlign = ContentAlignment.MiddleCenter;
            lblDbTop.Location = new Point(350, 160);
            lblDbTop.Font = new Font("Arial", 7, FontStyle.Bold);
            lblDbTop.ForeColor = Color.Red;
            lblDbTop.Text = "db = " + textBox2.Text;
            groupBox1.Controls.Add(lblDbTop);
            lblDbTop.BringToFront();

            textBox2.TextChanged += (sender, args) => {
                lblDbTop.Text = "db = " + textBox2.Text;
                groupBox1.Invalidate(); // Refresh the group box drawings when text changes
            };

            // Draw the dimension bracket for db on the GroupBox so it floats outside the image
            groupBox1.Paint += (sender, e) => {
                using (Pen redPen = new Pen(Color.Red, 1.5f))
                {
                    int boltCenterX = pic4.Left + 50; 
                    int boltWidth = 4;
                    int lineY = pic4.Top - 5; // 5 pixels above the image
                    int tickDrop = 5; // Drops down to touch the image boundary
                    
                    e.Graphics.DrawLine(redPen, boltCenterX - boltWidth, lineY, boltCenterX + boltWidth, lineY);
                    e.Graphics.DrawLine(redPen, boltCenterX - boltWidth, lineY, boltCenterX - boltWidth, lineY + tickDrop);
                    e.Graphics.DrawLine(redPen, boltCenterX + boltWidth, lineY, boltCenterX + boltWidth, lineY + tickDrop);
                }

            };

            // Draw the purple dimension bracket for hef inside the picture box so it draws over the image
            pic4.Paint += (sender, e) => {
                using (Pen purplePen = new Pen(Color.Purple, 1.5f))
                {
                    int hefLineX = 85; // Bracket placed on the right edge of the concrete block
                    int hefTopY = 26;  // Extended slightly upwards
                    int hefBottomY = 88; // Extended slightly downwards
                    int hefTick = 25; // Extended much further left towards the bolt

                    // Vertical line
                    e.Graphics.DrawLine(purplePen, hefLineX, hefTopY, hefLineX, hefBottomY);
                    
                    // Top and bottom ticks
                    e.Graphics.DrawLine(purplePen, hefLineX, hefTopY, hefLineX - hefTick, hefTopY);
                    e.Graphics.DrawLine(purplePen, hefLineX, hefBottomY, hefLineX - hefTick, hefBottomY);
                }
            };
            Label lblTotalNumberDisplay = new Label();
            lblTotalNumberDisplay.AutoSize = true;
            lblTotalNumberDisplay.Location = new Point(350, 275);
            lblTotalNumberDisplay.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            lblTotalNumberDisplay.Text = "Total Number of Bolts = " + textBox1.Text;
            groupBox1.Controls.Add(lblTotalNumberDisplay);
            lblTotalNumberDisplay.BringToFront();
            
            textBox1.TextChanged += (sender, args) => {
                lblTotalNumberDisplay.Text = "Total Number of Bolts = " + textBox1.Text;
            };

            PictureBox pic5 = new PictureBox();
            pic5.Location = new Point(350, 290);
            pic5.Size = new Size(100, 85);
            pic5.SizeMode = PictureBoxSizeMode.Zoom;
            string pic5Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 5.png");
            if (System.IO.File.Exists(pic5Path)) pic5.Image = Image.FromFile(pic5Path);
            groupBox1.Controls.Add(pic5);

            Label lblNoteBox = new Label();
            lblNoteBox.AutoSize = false;
            lblNoteBox.Size = new Size(180, 50);
            lblNoteBox.Location = new Point(495, 250); // Moved further to the right
            lblNoteBox.BorderStyle = BorderStyle.FixedSingle;
            lblNoteBox.TextAlign = ContentAlignment.MiddleCenter;
            lblNoteBox.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblNoteBox.Text = "Note: Refer to Table 1.2 ACi 318-19 to choose the hef and Edge distance";
            groupBox1.Controls.Add(lblNoteBox);
            lblNoteBox.BringToFront();

            // Paint events for dynamic annotations
            pic5.Paint += (sender, e) => {
                int centerX = pic5.Width / 2;

                // Red bracket for db (inner hole)
                using (Pen redPen = new Pen(Color.Red, 1.5f))
                using (Pen redDashPen = new Pen(Color.Red, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    int dbWidth = 8; // Reduced half-width of inner hole
                    int dbY = 14;     // Y pos of bracket horizontal line
                    int dbDrop = 22;  // Increased length of dashed vertical drop so it goes further down
                    
                    e.Graphics.DrawLine(redPen, centerX - dbWidth, dbY, centerX + dbWidth, dbY);
                    e.Graphics.DrawLine(redDashPen, centerX - dbWidth, dbY, centerX - dbWidth, dbY + dbDrop);
                    e.Graphics.DrawLine(redDashPen, centerX + dbWidth, dbY, centerX + dbWidth, dbY + dbDrop);
                }

                // Blue bracket for dh (outer ring)
                using (Pen bluePen = new Pen(Color.Blue, 1.5f))
                using (Pen blueDashPen = new Pen(Color.Blue, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    int dhWidth = 10; // Reduced just a little more
                    int dhY = 70;     // Y pos of bracket horizontal line
                    int dhRise = 27;  // Increased length to go further upwards
                    
                    e.Graphics.DrawLine(bluePen, centerX - dhWidth, dhY, centerX + dhWidth, dhY);
                    e.Graphics.DrawLine(blueDashPen, centerX - dhWidth, dhY, centerX - dhWidth, dhY - dhRise);
                    e.Graphics.DrawLine(blueDashPen, centerX + dhWidth, dhY, centerX + dhWidth, dhY - dhRise);
                }

                using (Font font = new Font("Arial", 7, FontStyle.Bold))
                {
                    StringFormat sfCenter = new StringFormat();
                    sfCenter.Alignment = StringAlignment.Center;
                    
                    string dbText = "db = " + textBox2.Text;
                    e.Graphics.DrawString(dbText, font, Brushes.Red, new PointF(centerX, 2), sfCenter);

                    string dhText = "dh = " + textBox3.Text;
                    e.Graphics.DrawString(dhText, font, Brushes.Blue, new PointF(centerX, 72), sfCenter);
                }
            };

            // Label for hef = 96 so it doesn't get clipped by the right bound of pic4
            Label lblHefTop = new Label();
            lblHefTop.AutoSize = true;
            lblHefTop.Location = new Point(pic4.Left + 88, pic4.Top + 50); // Positioned right next to the purple bracket
            lblHefTop.Font = new Font("Arial", 7, FontStyle.Bold);
            lblHefTop.ForeColor = Color.Purple;
            lblHefTop.Text = "hef = " + textBox19.Text;
            groupBox1.Controls.Add(lblHefTop);
            lblHefTop.BringToFront();

            textBox19.TextChanged += (sender, args) => {
                lblHefTop.Text = "hef = " + textBox19.Text;
            };

            textBox2.TextChanged += (sender, args) => { pic4.Invalidate(); pic5.Invalidate(); };
            textBox3.TextChanged += (sender, args) => { pic5.Invalidate(); };
            textBox19.TextChanged += (sender, args) => { pic4.Invalidate(); };



            // Adjust text boxes dynamically if the buttons overlap them
            if (infoButtonHef.Right + 5 > textBox19.Left)
            {
                int shift = (infoButtonHef.Right + 5) - textBox19.Left;
                MoveControlsRight(shift, textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8, textBox17, textBox18, textBox19, comboBox1);
            }
            if (infoButtonEdge.Right + 5 > textBox14.Left)
            {
                int shift = (infoButtonEdge.Right + 5) - textBox14.Left;
                MoveControlsRight(shift, textBox10, textBox12, textBox13, textBox14, textBox9);
                
                // Expand the GroupBox and Form so the textboxes don't get cut off on the right
                groupBox1.Width += shift;
                this.Width += shift;
            }
        }

        private void MoveControlsRight(int shift, params Control[] controls)
        {
            foreach (var ctrl in controls)
            {
                if (ctrl != null) ctrl.Left += shift;
            }
        }

        private void SetupMultiColumnUI()
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                // Hide Single-Column specific fields
                label3.Visible = textBox3.Visible = false; // Hole Diameter
                label4.Visible = textBox4.Visible = false; // Circle Radius
                label5.Visible = textBox5.Visible = false; // Start Angle
                label6.Visible = textBox6.Visible = false; // Segment Angle
                label7.Visible = textBox7.Visible = false; // Num Segments
                label8.Visible = textBox8.Visible = false; // Base Plate Thickness
                label20.Visible = comboBox1.Visible = false; // Distribution Method

                // Create new labels and textboxes
                lblAb = new Label { Text = "Bolt Area (Ab) (in²)", Location = label3.Location, AutoSize = true };
                txtAb = new TextBox { Location = textBox3.Location, Size = textBox3.Size };

                lblPedestalSize = new Label { Text = "Pedestal Size (B x L) (in)", Location = label4.Location, AutoSize = true };
                txtPedestalSize = new TextBox { Location = textBox4.Location, Size = textBox4.Size };

                lblBoltSpacing = new Label { Text = "Bolt Spacing (b x l) (in)", Location = label5.Location, AutoSize = true };
                txtBoltSpacing = new TextBox { Location = textBox5.Location, Size = textBox5.Size };

                lblWasherSize = new Label { Text = "Washer Plate Size (in)", Location = label6.Location, AutoSize = true };
                txtWasherSize = new TextBox { Location = textBox6.Location, Size = textBox6.Size };

                lblDcone = new Label { Text = "Cone Diameter (Dcone) (ft)", Location = label7.Location, AutoSize = true };
                txtDcone = new TextBox { Location = textBox7.Location, Size = textBox7.Size };

                lblPu = new Label { Text = "Factored Axial Load (Pu) (kips)", Location = label8.Location, AutoSize = true };
                txtPu = new TextBox { Location = textBox8.Location, Size = textBox8.Size };

                groupBox1.Controls.Add(lblAb);
                groupBox1.Controls.Add(txtAb);
                groupBox1.Controls.Add(lblPedestalSize);
                groupBox1.Controls.Add(txtPedestalSize);
                groupBox1.Controls.Add(lblBoltSpacing);
                groupBox1.Controls.Add(txtBoltSpacing);
                groupBox1.Controls.Add(lblWasherSize);
                groupBox1.Controls.Add(txtWasherSize);
                groupBox1.Controls.Add(lblDcone);
                groupBox1.Controls.Add(txtDcone);
                groupBox1.Controls.Add(lblPu);
                groupBox1.Controls.Add(txtPu);
            }
        }

        private void LoadExistingData()
        {
            try
            {
                // load first record
                _existingAnchorBolt = _context.AnchorBoltEntity.FirstOrDefault();

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

                if (_existingAnchorBolt == null)
                {
                    // For a new record, auto-populate Mu and Vu (and Pu if MultiColumn)
                    textBox17.Text = Mu.ToString(CultureInfo.InvariantCulture);
                    textBox12.Text = Vu.ToString(CultureInfo.InvariantCulture);
                    
                    if (AppState.CurrentTankType == TankType.MultiColumn)
                    {
                        txtPu.Text = Pu.ToString(CultureInfo.InvariantCulture);
                        txtDcone.Text = bottomDiameter.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // For single column, you can pre-fill Dcone or equivalent if mapped.
                        // Here, assuming Dcone maps to textBox7 (Num Segments) or similar based on UI setup.
                        // We will not overwrite arbitrary textboxes unless mapped clearly.
                        // But for demonstration, we have bottomDiameter available.
                    }
                    return;
                }

                // If existing record, load its values
                textBox1.Text = _existingAnchorBolt.Nb.ToString();
                textBox2.Text = _existingAnchorBolt.Db.ToString(CultureInfo.InvariantCulture);
                textBox3.Text = _existingAnchorBolt.Dh.ToString(CultureInfo.InvariantCulture);
                textBox4.Text = _existingAnchorBolt.Rb.ToString(CultureInfo.InvariantCulture);
                textBox5.Text = _existingAnchorBolt.Ab.ToString(CultureInfo.InvariantCulture);
                textBox6.Text = _existingAnchorBolt.ThetaSeg?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox7.Text = _existingAnchorBolt.Ns?.ToString() ?? "";
                textBox8.Text = _existingAnchorBolt.Tbp.ToString(CultureInfo.InvariantCulture);

                textBox9.Text = _existingAnchorBolt.Fy?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox10.Text = _existingAnchorBolt.Fu?.ToString(CultureInfo.InvariantCulture) ?? "";
                
                // Always auto-update Mu and Vu with the latest calculations from LoadService
                textBox12.Text = Vu.ToString(CultureInfo.InvariantCulture);
                textBox17.Text = Mu.ToString(CultureInfo.InvariantCulture);

                textBox13.Text = _existingAnchorBolt.Phi?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox14.Text = _existingAnchorBolt.E?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox18.Text = _existingAnchorBolt.FcPrime?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox19.Text = _existingAnchorBolt.Hef?.ToString(CultureInfo.InvariantCulture) ?? "";

                if (!string.IsNullOrEmpty(_existingAnchorBolt.DistributionMethod))
                {
                    int index = comboBox1.FindStringExact(_existingAnchorBolt.DistributionMethod);
                    if (index != -1) comboBox1.SelectedIndex = index;
                }

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    txtAb.Text = _existingAnchorBolt.Ab.ToString(CultureInfo.InvariantCulture) ?? "";
                    txtPedestalSize.Text = _existingAnchorBolt.PedestalSize?.ToString(CultureInfo.InvariantCulture) ?? "";
                    txtBoltSpacing.Text = _existingAnchorBolt.BoltSpacing?.ToString(CultureInfo.InvariantCulture) ?? "";
                    txtWasherSize.Text = _existingAnchorBolt.WasherSize?.ToString(CultureInfo.InvariantCulture) ?? "";
                    txtDcone.Text = _existingAnchorBolt.Dcone?.ToString(CultureInfo.InvariantCulture) ?? bottomDiameter.ToString(CultureInfo.InvariantCulture);
                    txtPu.Text = Pu.ToString(CultureInfo.InvariantCulture); // Always auto-update Pu
                }

                SavedAnchorBolt = _existingAnchorBolt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load anchor bolt data.\n{ex.Message}",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // if record exists -> update it
                // else -> create new one
                var entity = _existingAnchorBolt ?? new AnchorBoltEntity();

                entity.Nb = ParseIntRequired(textBox1, "Total Number of Bolts");
                entity.Db = ParseDoubleRequired(textBox2, "Nominal Diameter");

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    entity.Dh = ParseDoubleNullable(textBox3) ?? 1.0;
                    entity.Rb = ParseDoubleNullable(textBox4) ?? 1.0;
                    entity.Ab = ParseDoubleNullable(txtAb) ?? 0.0;
                    entity.Tbp = ParseDoubleNullable(textBox8) ?? 1.0;
                    
                    entity.PedestalSize = ParseDoubleNullable(txtPedestalSize);
                    entity.BoltSpacing = ParseDoubleNullable(txtBoltSpacing);
                    entity.WasherSize = ParseDoubleNullable(txtWasherSize);
                    entity.Dcone = ParseDoubleNullable(txtDcone);
                    entity.Pu = ParseDoubleNullable(txtPu);
                }
                else
                {
                    entity.Dh = ParseDoubleRequired(textBox3, "Hole Diameter");
                    entity.Rb = ParseDoubleRequired(textBox4, "Circle Radius");
                    entity.Ab = ParseDoubleRequired(textBox5, "Start Angle of first bolt");
                    entity.Tbp = ParseDoubleRequired(textBox8, "Base Plate Thickness");
                }

                entity.ThetaSeg = ParseDoubleNullable(textBox6);
                entity.Ns = ParseIntNullable(textBox7);

                entity.Fy = ParseDoubleNullable(textBox9);
                entity.Fu = ParseDoubleNullable(textBox10);
                entity.Vu = ParseDoubleRequired(textBox12, "Shear Demand");
                entity.Phi = ParseDoubleNullable(textBox13);
                entity.E = ParseDoubleNullable(textBox14);
                entity.Mu = ParseDoubleRequired(textBox17, "Governing Moment (Mu)");
                entity.FcPrime = ParseDoubleNullable(textBox18);
                entity.Hef = ParseDoubleNullable(textBox19);

                entity.DistributionMethod = comboBox1.SelectedItem?.ToString();

                if (_existingAnchorBolt == null)
                {
                    _context.AnchorBoltEntity.Add(entity);
                }
                else
                {
                    _context.AnchorBoltEntity.Update(entity);
                }

                _context.SaveChanges();

                _existingAnchorBolt = entity;
                SavedAnchorBolt = entity;

                MessageBox.Show("Anchor bolt data saved successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private Panel imagePopupPanel;

        private void infoButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (imagePopupPanel == null)
                {
                    // Find the image file
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    string imagePath = null;
                    while (!string.IsNullOrEmpty(currentDir))
                    {
                        string possiblePath = System.IO.Path.Combine(currentDir, "SS9.PNG");
                        if (System.IO.File.Exists(possiblePath)) { imagePath = possiblePath; break; }
                        string srcPath = System.IO.Path.Combine(currentDir, "src", "SS9.PNG");
                        if (System.IO.File.Exists(srcPath)) { imagePath = srcPath; break; }
                        var parent = System.IO.Directory.GetParent(currentDir);
                        currentDir = parent?.FullName;
                    }

                    if (imagePath != null)
                    {
                        Image img = Image.FromFile(imagePath);
                        int imgWidth = 250;
                        int imgHeight = (int)((double)img.Height / img.Width * imgWidth);

                        imagePopupPanel = new Panel();
                        // Panel size is inner image size + 20px padding (10px on each side)
                        imagePopupPanel.Size = new Size(imgWidth + 20, imgHeight + 20); 
                        imagePopupPanel.BorderStyle = BorderStyle.FixedSingle;
                        imagePopupPanel.BackColor = Color.White;
                        imagePopupPanel.Padding = new Padding(10);
                        
                        PictureBox pb = new PictureBox();
                        pb.Image = img;
                        pb.SizeMode = PictureBoxSizeMode.Zoom;
                        pb.Dock = DockStyle.Fill;
                        pb.Cursor = Cursors.Hand;;
                        
                        // Close on click
                        pb.Click += (s, ev) => { imagePopupPanel.Visible = false; };
                        
                        imagePopupPanel.Controls.Add(pb);
                        this.Controls.Add(imagePopupPanel);
                    }
                    else
                    {
                        MessageBox.Show("Could not find SS9.PNG.", "Information Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Toggle visibility and position
                if (imagePopupPanel.Visible)
                {
                    imagePopupPanel.Visible = false;
                }
                else
                {
                    Control clickedControl = sender as Control;
                    if (clickedControl != null)
                    {
                        // Position it near the button that was clicked
                        int targetX = clickedControl.Location.X + clickedControl.Width + 5;
                        int targetY = clickedControl.Location.Y;
                        
                        // Prevent going off screen
                        if (targetX + imagePopupPanel.Width > this.ClientSize.Width)
                            targetX = this.ClientSize.Width - imagePopupPanel.Width - 10;
                        if (targetY + imagePopupPanel.Height > this.ClientSize.Height)
                            targetY = this.ClientSize.Height - imagePopupPanel.Height - 10;

                        // Account for grouping box offset since buttons are in groupBox1
                        targetX += groupBox1.Location.X;
                        targetY += groupBox1.Location.Y;

                        imagePopupPanel.Location = new Point(targetX, targetY);
                    }
                    imagePopupPanel.BringToFront();
                    imagePopupPanel.Visible = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error displaying information: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}