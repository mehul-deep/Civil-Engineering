using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class BasePlateParameters : Form
    {
        private readonly WaterTankDbContext _context;

        public BasePlateEntity? SavedBasePlate { get; private set; }

        // existing row from DB
        private BasePlateEntity? _existingBasePlate;

        public BasePlateParameters()
        {
            InitializeComponent();

            _context = WaterTankDbContext.GetInstance();

            this.Load += BasePlateParameters_Load;
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

                if (_existingBasePlate == null)
                {
                    // Leave fields blank for user input as requested
                    return;
                }

                textBox1.Text = _existingBasePlate.Dbp.ToString(CultureInfo.InvariantCulture);
                textBox2.Text = _existingBasePlate.Ro.ToString(CultureInfo.InvariantCulture);
                textBox3.Text = _existingBasePlate.Ri.ToString(CultureInfo.InvariantCulture);
                textBox4.Text = _existingBasePlate.Theta.ToString(CultureInfo.InvariantCulture);
                textBox5.Text = _existingBasePlate.T.ToString(CultureInfo.InvariantCulture);

                textBox6.Text = _existingBasePlate.N.ToString();
                textBox7.Text = _existingBasePlate.Dh.ToString(CultureInfo.InvariantCulture);
                textBox8.Text = _existingBasePlate.A?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox9.Text = _existingBasePlate.Rb?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox10.Text = _existingBasePlate.Nh.ToString();

                // Structural Parameters
                textBox11.Text = _existingBasePlate.Fy.ToString(CultureInfo.InvariantCulture);
                textBox12.Text = _existingBasePlate.Fc_prime.ToString(CultureInfo.InvariantCulture);
                textBox13.Text = _existingBasePlate.A2?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox14.Text = _existingBasePlate.Pu?.ToString(CultureInfo.InvariantCulture) ?? "";
                textBox15.Text = _existingBasePlate.OverturningMoment?.ToString(CultureInfo.InvariantCulture) ?? "";

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

                // Structural Parameters
                entity.Fy = ParseDoubleRequired(textBox11, "Steel Yield (Fy)");
                entity.Fc_prime = ParseDoubleRequired(textBox12, "Concrete Strength (f'c)");
                entity.A2 = ParseDoubleNullable(textBox13);
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

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

    }
}