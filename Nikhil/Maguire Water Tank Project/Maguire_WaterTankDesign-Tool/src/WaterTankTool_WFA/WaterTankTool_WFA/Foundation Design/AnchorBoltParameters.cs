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
                label14.Visible = textBox14.Visible = false; // Edge Distance
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

                entity.Nb = ParseIntRequired(textBox1, "Total Number");
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
    }
}