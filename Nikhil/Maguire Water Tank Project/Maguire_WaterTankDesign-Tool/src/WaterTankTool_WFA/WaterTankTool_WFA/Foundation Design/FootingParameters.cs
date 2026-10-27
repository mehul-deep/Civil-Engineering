using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Services;

namespace WaterTankTool_WFA.Foundation_Design
{
    public class FootingParameters : Form
    {
        private readonly WaterTankDbContext _context;

        // Governing Load & Pedestal display fields (Read-only)
        private TextBox txtNumLegs = null!;
        private TextBox txtPu = null!;
        private TextBox txtMu = null!;
        private TextBox txtVu = null!;
        private TextBox txtPedSize = null!;

        // Editable Material / Pedestal / Location fields (Left panel bottom)
        private TextBox txtHp = null!;
        private TextBox txtFcPrime = null!;
        private TextBox txtFy = null!;
        private ComboBox cmbLocation = null!;

        // Footing Geometry, Soil & Rebar input fields (Right panel)
        private TextBox txtB = null!;
        private TextBox txtL = null!;
        private TextBox txtH = null!;
        private TextBox txtCover = null!;
        private TextBox txtQallow = null!;
        private TextBox txtFriction = null!;
        private TextBox txtBotAs = null!;
        private TextBox txtDb = null!;
        private TextBox txtTopAs = null!;
        private TextBox txtDowelDb = null!;

        private Button btnAccept = null!;
        private Button btnCancel = null!;

        public FootingParameters()
        {
            this.Text = "Multi-Leg Footing Parameters";
            this.Size = new Size(690, 465);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _context = WaterTankDbContext.GetInstance();

            SetupUI();
            LoadData();
        }

        private void SetupUI()
        {
            // --- GroupBox 1: Governing Loads & Pedestal Properties ---
            GroupBox grpLoads = new GroupBox
            {
                Text = "Governing Loads & Pedestal Properties",
                Location = new Point(12, 12),
                Size = new Size(315, 360)
            };
            this.Controls.Add(grpLoads);

            int yLoad = 26;
            txtNumLegs = AddRowToGroup(grpLoads, "Number of Legs:", ref yLoad, true);
            txtPu = AddRowToGroup(grpLoads, "Axial Pu (kips):", ref yLoad, true);
            txtMu = AddRowToGroup(grpLoads, "Moment Mu (k-ft):", ref yLoad, true);
            txtVu = AddRowToGroup(grpLoads, "Shear Vu (kips):", ref yLoad, true);
            txtPedSize = AddRowToGroup(grpLoads, "Pedestal Bp x Lp (in):", ref yLoad, true);

            yLoad += 6;
            txtHp = AddRowToGroup(grpLoads, "Pedestal Height Hp (ft):", ref yLoad, false);
            txtFcPrime = AddRowToGroup(grpLoads, "Concrete f'c (ksi):", ref yLoad, false);
            txtFy = AddRowToGroup(grpLoads, "Steel fy (ksi):", ref yLoad, false);

            Label lblLoc = new Label { Text = "Pedestal Location (αs):", Location = new Point(12, yLoad + 3), AutoSize = true };
            cmbLocation = new ComboBox
            {
                Location = new Point(172, yLoad),
                Size = new Size(128, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbLocation.Items.AddRange(new object[] { "Interior (αs = 40)", "Edge (αs = 30)", "Corner (αs = 20)" });
            cmbLocation.SelectedIndex = 0;
            grpLoads.Controls.Add(lblLoc);
            grpLoads.Controls.Add(cmbLocation);

            // --- GroupBox 2: Footing Geometry, Soil & Rebar ---
            GroupBox grpGeom = new GroupBox
            {
                Text = "Footing Geometry, Soil & Reinforcement",
                Location = new Point(338, 12),
                Size = new Size(324, 360)
            };
            this.Controls.Add(grpGeom);

            int yGeom = 26;
            txtB = AddRowToGroup(grpGeom, "Footing Size B (ft):", ref yGeom, false, 182);
            txtL = AddRowToGroup(grpGeom, "Footing Size L (ft):", ref yGeom, false, 182);
            txtH = AddRowToGroup(grpGeom, "Footing Thickness h (in):", ref yGeom, false, 182);
            txtCover = AddRowToGroup(grpGeom, "Concrete Cover cc (in):", ref yGeom, false, 182);
            txtQallow = AddRowToGroup(grpGeom, "Allowable Soil q_allow (ksf):", ref yGeom, false, 182);
            txtFriction = AddRowToGroup(grpGeom, "Friction Coefficient:", ref yGeom, false, 182);
            txtBotAs = AddRowToGroup(grpGeom, "Bottom Rebar As (in2/ft):", ref yGeom, false, 182);
            txtDb = AddRowToGroup(grpGeom, "Bottom Bar Diam db (in):", ref yGeom, false, 182);
            txtTopAs = AddRowToGroup(grpGeom, "Provided Dowel As (in2):", ref yGeom, false, 182);
            txtDowelDb = AddRowToGroup(grpGeom, "Dowel Bar Diam (in):", ref yGeom, false, 182);

            // --- Action Buttons ---
            btnAccept = new Button { Text = "Accept", Location = new Point(240, 385), Size = new Size(90, 28) };
            btnAccept.Click += BtnAccept_Click;
            this.Controls.Add(btnAccept);

            btnCancel = new Button { Text = "Cancel", Location = new Point(345, 385), Size = new Size(90, 28) };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private TextBox AddRowToGroup(GroupBox grp, string labelText, ref int y, bool isReadOnly, int txtX = 172)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(12, y + 3), AutoSize = true };
            grp.Controls.Add(lbl);

            TextBox txt = new TextBox
            {
                Location = new Point(txtX, y),
                Width = 128,
                ReadOnly = isReadOnly,
                BackColor = isReadOnly ? Color.FromArgb(240, 240, 240) : Color.White
            };
            grp.Controls.Add(txt);
            txt.BringToFront();

            y += 32;
            return txt;
        }

        private void LoadData()
        {
            AppState.Load();
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();
            var ringWall = _context.RingWallEntity?.FirstOrDefault();
            var loadService = new LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            int numLegs = AppState.NoOfColumns > 1
                ? AppState.NoOfColumns
                : ((anchorBolt?.Ns.HasValue == true && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);

            double pu = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
            double mu = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                        ? basePlate.OverturningMoment.Value
                        : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
            double vu = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;

            double bp = (ringWall?.PedestalSizeB.HasValue == true && ringWall.PedestalSizeB.Value > 0)
                ? ringWall.PedestalSizeB.Value
                : ((anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0);
            double lp = (ringWall?.PedestalSizeL.HasValue == true && ringWall.PedestalSizeL.Value > 0)
                ? ringWall.PedestalSizeL.Value
                : ((anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : bp);

            txtNumLegs.Text = numLegs.ToString(CultureInfo.InvariantCulture);
            txtPu.Text = pu.ToString("F4", CultureInfo.InvariantCulture);
            txtMu.Text = mu.ToString("F4", CultureInfo.InvariantCulture);
            txtVu.Text = vu.ToString("F4", CultureInfo.InvariantCulture);
            txtPedSize.Text = $"{bp.ToString(CultureInfo.InvariantCulture)} x {lp.ToString(CultureInfo.InvariantCulture)}";

            double autoHp = (ringWall?.Hp.HasValue == true && ringWall.Hp.Value > 0) ? ringWall.Hp.Value : 5.5;
            double autoFc = (ringWall?.FcPrime.HasValue == true && ringWall.FcPrime.Value > 0) ? ringWall.FcPrime.Value : 4.0;
            double autoFy = (ringWall?.Fy.HasValue == true && ringWall.Fy.Value > 0) ? ringWall.Fy.Value : 60.0;
            double autoDowelAs = (ringWall?.PedestalAsProv.HasValue == true && ringWall.PedestalAsProv.Value > 0) ? ringWall.PedestalAsProv.Value : 7.92;

            var entity = _context.FootingEntity?.FirstOrDefault();
            if (entity != null)
            {
                txtHp.Text = (entity.PedestalHeightHp ?? autoHp).ToString(CultureInfo.InvariantCulture);
                txtFcPrime.Text = (entity.FcPrime ?? autoFc).ToString(CultureInfo.InvariantCulture);
                txtFy.Text = (entity.Fy ?? autoFy).ToString(CultureInfo.InvariantCulture);

                double alphaSaved = entity.AlphaS ?? 40.0;
                cmbLocation.SelectedIndex = Math.Abs(alphaSaved - 30.0) < 0.1 ? 1 : (Math.Abs(alphaSaved - 20.0) < 0.1 ? 2 : 0);

                txtB.Text = (entity.FootingSizeB ?? 13.5).ToString(CultureInfo.InvariantCulture);
                txtL.Text = (entity.FootingSizeL ?? 13.5).ToString(CultureInfo.InvariantCulture);
                txtH.Text = (entity.FootingThickness ?? 30.0).ToString(CultureInfo.InvariantCulture);
                txtCover.Text = (entity.ConcreteCover ?? 3.0).ToString(CultureInfo.InvariantCulture);
                txtQallow.Text = (entity.Qallow ?? 3.0).ToString(CultureInfo.InvariantCulture);
                txtFriction.Text = (entity.FrictionCoeff ?? 0.50).ToString(CultureInfo.InvariantCulture);
                txtBotAs.Text = (entity.BottomRebarArea ?? 0.677).ToString(CultureInfo.InvariantCulture);
                txtDb.Text = (entity.RebarDiameter ?? 1.0).ToString(CultureInfo.InvariantCulture);
                txtTopAs.Text = (entity.TopRebarArea ?? autoDowelAs).ToString(CultureInfo.InvariantCulture);
                txtDowelDb.Text = (entity.DowelDiameter ?? 0.75).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                txtHp.Text = autoHp.ToString(CultureInfo.InvariantCulture);
                txtFcPrime.Text = autoFc.ToString(CultureInfo.InvariantCulture);
                txtFy.Text = autoFy.ToString(CultureInfo.InvariantCulture);
                cmbLocation.SelectedIndex = 0;

                txtB.Text = "13.5";
                txtL.Text = "13.5";
                txtH.Text = "30.0";
                txtCover.Text = "3.0";
                txtQallow.Text = "3.0";
                txtFriction.Text = "0.50";
                txtBotAs.Text = "0.677"; // #8 @ 14" o.c. each way
                txtDb.Text = "1.0";      // #8 bar
                txtTopAs.Text = autoDowelAs.ToString(CultureInfo.InvariantCulture);
                txtDowelDb.Text = "0.75"; // #6 dowel
            }
        }

        private void BtnAccept_Click(object? sender, EventArgs e)
        {
            var entity = _context.FootingEntity?.FirstOrDefault();
            if (entity == null)
            {
                entity = new FootingEntity();
                _context.FootingEntity!.Add(entity);
            }

            double.TryParse(txtHp.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double hp); entity.PedestalHeightHp = hp;
            double.TryParse(txtFcPrime.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fc); entity.FcPrime = fc;
            double.TryParse(txtFy.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fy); entity.Fy = fy;
            entity.AlphaS = cmbLocation.SelectedIndex == 1 ? 30.0 : (cmbLocation.SelectedIndex == 2 ? 20.0 : 40.0);

            double.TryParse(txtB.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double b); entity.FootingSizeB = b;
            double.TryParse(txtL.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double l); entity.FootingSizeL = l;
            double.TryParse(txtH.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h); entity.FootingThickness = h;
            double.TryParse(txtCover.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double cover); entity.ConcreteCover = cover;
            double.TryParse(txtQallow.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double q); entity.Qallow = q;
            double.TryParse(txtFriction.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fric); entity.FrictionCoeff = fric;
            double.TryParse(txtBotAs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double bot); entity.BottomRebarArea = bot;
            double.TryParse(txtDb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double db); entity.RebarDiameter = db;
            double.TryParse(txtTopAs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double top); entity.TopRebarArea = top;
            double.TryParse(txtDowelDb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dowelDb); entity.DowelDiameter = dowelDb;

            _context.SaveChanges();
            MessageBox.Show("Footing parameters saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
