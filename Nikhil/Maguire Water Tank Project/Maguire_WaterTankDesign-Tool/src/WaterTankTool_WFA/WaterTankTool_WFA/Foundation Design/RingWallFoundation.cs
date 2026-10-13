using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class RingWallFoundation : Form
    {
        private readonly WaterTankDbContext _context;
        private RingWallEntity? _existingEntity;
        
        private TextBox txtFcPrime, txtFy, txtHef, txtHp, txtGammaC, txtPedestalB, txtPedestalL, txtPedestalAs, txtFootingAs;
        private TextBox? txtPu, txtMu, txtVu;
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
                this.Size = new Size(640, 420);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;

                // --- GroupBox 1: Governing Loads ---
                groupBoxLoads = new GroupBox
                {
                    Text = "Governing Loads (From Tank Analysis)",
                    Location = new Point(12, 12),
                    Size = new Size(290, 320)
                };
                this.Controls.Add(groupBoxLoads);

                int yLoad = 35;
                txtPu = AddFieldToGroup(groupBoxLoads, "Axial Pu (kips):", yLoad, true);
                txtMu = AddFieldToGroup(groupBoxLoads, "Moment Mu (k-ft):", yLoad + 28, true);
                txtVu = AddFieldToGroup(groupBoxLoads, "Shear Vu (kips):", yLoad + 56, true);

                Label lblNote = new Label
                {
                    Text = "Note: Loads are automatically\ninherited from tank analysis\n(Wind/Seismic & Dead Load)\nand Anchor Bolt calculation.",
                    Location = new Point(15, yLoad + 95),
                    Size = new Size(255, 70),
                    ForeColor = Color.DimGray
                };
                groupBoxLoads.Controls.Add(lblNote);

                // --- GroupBox 2: Pedestal Geometry & Material ---
                groupBox2.Text = "Pedestal Geometry & Material";
                groupBox2.Location = new Point(315, 12);
                groupBox2.Size = new Size(295, 320);

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

                int startY = 25;
                int gapY = 28;
                
                txtFcPrime = AddFieldToGroup(groupBox2, "Concrete f'c (ksi):", startY, false);
                txtFy = AddFieldToGroup(groupBox2, "Steel fy (ksi):", startY + gapY * 1, false);
                txtHef = AddFieldToGroup(groupBox2, "Embedment Depth hef (in):", startY + gapY * 2, false);
                txtHp = AddFieldToGroup(groupBox2, "Pedestal Clear Height Hp (ft):", startY + gapY * 3, false);
                txtGammaC = AddFieldToGroup(groupBox2, "Concrete Unit Wt (pcf):", startY + gapY * 4, false);
                txtPedestalB = AddFieldToGroup(groupBox2, "Pedestal Size B (in):", startY + gapY * 5, false);
                txtPedestalL = AddFieldToGroup(groupBox2, "Pedestal Size L (in):", startY + gapY * 6, false);
                txtPedestalAs = AddFieldToGroup(groupBox2, "Provided Pedestal As (in2):", startY + gapY * 7, false);
                txtFootingAs = AddFieldToGroup(groupBox2, "Provided Footing As (in2):", startY + gapY * 8, false);
                
                button1.Location = new Point(220, 345);
                button2.Location = new Point(320, 345);
            }
        }
        
        private TextBox AddFieldToGroup(GroupBox grp, string labelText, int yPos, bool isReadOnly)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(12, yPos + 2), AutoSize = true };
            TextBox txt = new TextBox
            {
                Location = new Point(155, yPos),
                Size = new Size(115, 23),
                ReadOnly = isReadOnly,
                BackColor = isReadOnly ? Color.FromArgb(240, 240, 240) : Color.White
            };
            grp.Controls.Add(lbl);
            grp.Controls.Add(txt);
            return txt;
        }

        private void LoadExistingData()
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
                var basePlate = _context.BasePlateEntity?.FirstOrDefault();
                var loadService = new Services.LoadService();
                var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

                double pu = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
                double mu = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                            ? basePlate.OverturningMoment.Value
                            : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
                double vu = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;

                if (txtPu != null) txtPu.Text = pu.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                if (txtMu != null) txtMu.Text = mu.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                if (txtVu != null) txtVu.Text = vu.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            }

            _existingEntity = _context.Set<RingWallEntity>().FirstOrDefault();
            if (_existingEntity == null)
            {
                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    txtFcPrime.Text = "4.0";
                    txtFy.Text = "60";
                    txtHef.Text = "40";
                    txtHp.Text = "7.417";
                    txtGammaC.Text = "150";
                    txtPedestalB.Text = "39";
                    txtPedestalL.Text = "39";
                    txtPedestalAs.Text = "7.92"; // 18-#6 bars
                    txtFootingAs.Text = "5.28"; // 12-#6 bars
                }
                return;
            }

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                txtFcPrime.Text = _existingEntity.FcPrime?.ToString() ?? "4.0";
                txtFy.Text = _existingEntity.Fy?.ToString() ?? "60";
                txtHef.Text = _existingEntity.Hef?.ToString() ?? "40";
                txtHp.Text = _existingEntity.Hp?.ToString() ?? "7.417";
                txtGammaC.Text = _existingEntity.GammaC?.ToString() ?? "150";
                txtPedestalB.Text = _existingEntity.PedestalSizeB?.ToString() ?? "39";
                txtPedestalL.Text = _existingEntity.PedestalSizeL?.ToString() ?? "39";
                txtPedestalAs.Text = _existingEntity.PedestalAsProv?.ToString() ?? "7.92";
                txtFootingAs.Text = _existingEntity.FootingAsProv?.ToString() ?? "5.28";
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
                double.TryParse(txtFcPrime.Text, out double fc); _existingEntity.FcPrime = fc;
                double.TryParse(txtFy.Text, out double fy); _existingEntity.Fy = fy;
                double.TryParse(txtHef.Text, out double hef); _existingEntity.Hef = hef;
                double.TryParse(txtHp.Text, out double hp); _existingEntity.Hp = hp;
                double.TryParse(txtGammaC.Text, out double gc); _existingEntity.GammaC = gc;
                double.TryParse(txtPedestalB.Text, out double b); _existingEntity.PedestalSizeB = b;
                double.TryParse(txtPedestalL.Text, out double l); _existingEntity.PedestalSizeL = l;
                double.TryParse(txtPedestalAs.Text, out double asProv); _existingEntity.PedestalAsProv = asProv;
                double.TryParse(txtFootingAs.Text, out double fAsProv); _existingEntity.FootingAsProv = fAsProv;
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
