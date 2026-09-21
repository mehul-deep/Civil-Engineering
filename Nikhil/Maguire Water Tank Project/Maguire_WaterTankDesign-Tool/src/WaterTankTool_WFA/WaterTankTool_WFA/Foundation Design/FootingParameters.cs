using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Foundation_Design
{
    public class FootingParameters : Form
    {
        private WaterTankDbContext _context;
        
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
            this.Size = new Size(350, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _context = WaterTankDbContext.GetInstance();

            SetupUI();
            LoadData();
        }

        private void SetupUI()
        {
            int y = 20;
            txtB = AddInputRow("Footing Size B (ft):", ref y);
            txtL = AddInputRow("Footing Size L (ft):", ref y);
            txtH = AddInputRow("Footing Thickness (in):", ref y);
            txtCover = AddInputRow("Concrete Cover (in):", ref y);
            txtQallow = AddInputRow("Allowable Bearing q (ksf):", ref y);
            txtFriction = AddInputRow("Friction Coefficient:", ref y);
            txtBotAs = AddInputRow("Bottom Rebar As (in2):", ref y);
            txtTopAs = AddInputRow("Top Rebar As (in2):", ref y);
            txtDb = AddInputRow("Rebar Diameter db (in):", ref y);

            btnAccept = new Button { Text = "Accept", Location = new Point(160, y + 10) };
            btnAccept.Click += BtnAccept_Click;
            this.Controls.Add(btnAccept);

            btnCancel = new Button { Text = "Cancel", Location = new Point(250, y + 10) };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private TextBox AddInputRow(string labelText, ref int y)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(20, y), AutoSize = true };
            this.Controls.Add(lbl);

            TextBox txt = new TextBox { Location = new Point(180, y - 3), Width = 100 };
            this.Controls.Add(txt);

            y += 30;
            return txt;
        }

        private void LoadData()
        {
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
                txtBotAs.Text = "6.32";
                txtTopAs.Text = "4.40";
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
