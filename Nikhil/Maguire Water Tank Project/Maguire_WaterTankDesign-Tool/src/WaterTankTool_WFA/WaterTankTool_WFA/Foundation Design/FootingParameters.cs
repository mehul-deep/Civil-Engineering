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
        private WaterTankDbContext _context;
        
        // Governing Load display fields (Auto-populated / Read-only)
        private TextBox txtPu;
        private TextBox txtMu;
        private TextBox txtVu;

        // Footing input fields
        private TextBox txtB;
        private TextBox txtL;
        private TextBox txtH;
        private TextBox txtCover;
        private TextBox txtQallow;
        private TextBox txtFriction;
        private TextBox txtBotAs;
        private TextBox txtTopAs;
        private TextBox txtDb;
        private Button btnAccept;
        private Button btnCancel;

        public FootingParameters()
        {
            this.Text = "Multi-Leg Footing Parameters";
            this.Size = new Size(640, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _context = WaterTankDbContext.GetInstance();

            SetupUI();
            LoadData();
        }

        private void SetupUI()
        {
            // --- GroupBox 1: Governing Loads ---
            GroupBox grpLoads = new GroupBox
            {
                Text = "Governing Loads (From Tank Analysis)",
                Location = new Point(15, 15),
                Size = new Size(290, 310)
            };
            this.Controls.Add(grpLoads);

            int yLoad = 35;
            txtPu = AddRowToGroup(grpLoads, "Axial Pu (kips):", ref yLoad, true);
            txtMu = AddRowToGroup(grpLoads, "Moment Mu (k-ft):", ref yLoad, true);
            txtVu = AddRowToGroup(grpLoads, "Shear Vu (kips):", ref yLoad, true);

            // Informational label
            Label lblNote = new Label
            {
                Text = "Note: Loads are automatically\ninherited from tank analysis\n(Wind/Seismic & Dead Load)\nand Anchor Bolt calculation.",
                Location = new Point(15, yLoad + 20),
                Size = new Size(255, 70),
                ForeColor = Color.DimGray
            };
            grpLoads.Controls.Add(lblNote);

            // --- GroupBox 2: Footing Geometry & Soil ---
            GroupBox grpGeom = new GroupBox
            {
                Text = "Footing Geometry & Soil Properties",
                Location = new Point(315, 15),
                Size = new Size(295, 310)
            };
            this.Controls.Add(grpGeom);

            int yGeom = 25;
            txtB = AddRowToGroup(grpGeom, "Footing Size B (ft):", ref yGeom, false);
            txtL = AddRowToGroup(grpGeom, "Footing Size L (ft):", ref yGeom, false);
            txtH = AddRowToGroup(grpGeom, "Footing Thickness (in):", ref yGeom, false);
            txtCover = AddRowToGroup(grpGeom, "Concrete Cover (in):", ref yGeom, false);
            txtQallow = AddRowToGroup(grpGeom, "Allowable Bearing q (ksf):", ref yGeom, false);
            txtFriction = AddRowToGroup(grpGeom, "Friction Coefficient:", ref yGeom, false);
            txtBotAs = AddRowToGroup(grpGeom, "Bottom Rebar As (in2/ft):", ref yGeom, false);
            txtTopAs = AddRowToGroup(grpGeom, "Dowel Area (in2):", ref yGeom, false);
            txtDb = AddRowToGroup(grpGeom, "Rebar Diameter db (in):", ref yGeom, false);

            // --- Action Buttons ---
            btnAccept = new Button { Text = "Accept", Location = new Point(220, 340), Size = new Size(85, 28) };
            btnAccept.Click += BtnAccept_Click;
            this.Controls.Add(btnAccept);

            btnCancel = new Button { Text = "Cancel", Location = new Point(320, 340), Size = new Size(85, 28) };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private TextBox AddRowToGroup(GroupBox grp, string labelText, ref int y, bool isReadOnly)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(12, y + 2), AutoSize = true };
            grp.Controls.Add(lbl);

            TextBox txt = new TextBox
            {
                Location = new Point(155, y),
                Width = 115,
                ReadOnly = isReadOnly,
                BackColor = isReadOnly ? Color.FromArgb(240, 240, 240) : Color.White
            };
            grp.Controls.Add(txt);

            y += 28;
            return txt;
        }

        private void LoadData()
        {
            // Auto-populate governing loads from AnchorBolt / BasePlate / LoadService
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();
            var loadService = new LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            double pu = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
            double mu = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                        ? basePlate.OverturningMoment.Value
                        : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
            double vu = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;

            txtPu.Text = pu.ToString("F4", CultureInfo.InvariantCulture);
            txtMu.Text = mu.ToString("F4", CultureInfo.InvariantCulture);
            txtVu.Text = vu.ToString("F4", CultureInfo.InvariantCulture);

            // Footing geometric/material properties
            var entity = _context.FootingEntity?.FirstOrDefault();
            if (entity != null)
            {
                txtB.Text = (entity.FootingSizeB ?? 13.5).ToString();
                txtL.Text = (entity.FootingSizeL ?? 13.5).ToString();
                txtH.Text = (entity.FootingThickness ?? 30.0).ToString();
                txtCover.Text = (entity.ConcreteCover ?? 3.0).ToString();
                txtQallow.Text = (entity.Qallow ?? 3.0).ToString();
                txtFriction.Text = (entity.FrictionCoeff ?? 0.50).ToString();
                txtBotAs.Text = (entity.BottomRebarArea ?? 0.0).ToString();
                txtTopAs.Text = (entity.TopRebarArea ?? 0.0).ToString();
                txtDb.Text = (entity.RebarDiameter ?? 1.0).ToString();
            }
            else
            {
                txtB.Text = "13.5";
                txtL.Text = "13.5";
                txtH.Text = "30.0";
                txtCover.Text = "3.0";
                txtQallow.Text = "3.0";
                txtFriction.Text = "0.50";
                txtBotAs.Text = "1.185";
                txtTopAs.Text = "7.92";
                txtDb.Text = "1.0";
            }
        }

        private void BtnAccept_Click(object sender, EventArgs e)
        {
            var entity = _context.FootingEntity?.FirstOrDefault();
            if (entity == null)
            {
                entity = new FootingEntity();
                _context.FootingEntity.Add(entity);
            }

            double.TryParse(txtB.Text, out double b); entity.FootingSizeB = b;
            double.TryParse(txtL.Text, out double l); entity.FootingSizeL = l;
            double.TryParse(txtH.Text, out double h); entity.FootingThickness = h;
            double.TryParse(txtCover.Text, out double cover); entity.ConcreteCover = cover;
            double.TryParse(txtQallow.Text, out double q); entity.Qallow = q;
            double.TryParse(txtFriction.Text, out double fric); entity.FrictionCoeff = fric;
            double.TryParse(txtBotAs.Text, out double bot); entity.BottomRebarArea = bot;
            double.TryParse(txtTopAs.Text, out double top); entity.TopRebarArea = top;
            double.TryParse(txtDb.Text, out double db); entity.RebarDiameter = db;

            _context.SaveChanges();
            MessageBox.Show("Footing parameters saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
