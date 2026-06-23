using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WaterTankTool_WFA.Foundation_Design
{
    public class AnchorBoltSelectionForm : Form
    {
        private PictureBox pb1, pb2, pb3;
        private RadioButton rb1, rb2, rb3;
        private Button btnOk, btnCancel;
        private Label lblTitle;

        public AnchorBoltSelectionForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Anchor Bolt Type";
            this.Size = new Size(800, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle = new Label();
            lblTitle.Text = "Please select an Anchor Bolt type:";
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Images
            string img1Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 1.png");
            string img2Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 2.png");
            string img3Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Pic 3.png");

            pb1 = CreatePictureBox(img1Path, 30, 60);
            pb2 = CreatePictureBox(img2Path, 280, 60);
            pb3 = CreatePictureBox(img3Path, 530, 60);

            this.Controls.Add(pb1);
            this.Controls.Add(pb2);
            this.Controls.Add(pb3);

            // Radio Buttons
            rb1 = new RadioButton();
            rb1.Text = "Hooked Bar";
            rb1.Location = new Point(30, 320);
            rb1.Enabled = false; // Only Bolt is selectable
            rb1.AutoSize = true;

            rb2 = new RadioButton();
            rb2.Text = "Bolt";
            rb2.Location = new Point(280, 320);
            rb2.Checked = true;
            rb2.AutoSize = true;

            rb3 = new RadioButton();
            rb3.Text = "Threaded Bar with Nut";
            rb3.Location = new Point(530, 320);
            rb3.Enabled = false;
            rb3.AutoSize = true;

            this.Controls.Add(rb1);
            this.Controls.Add(rb2);
            this.Controls.Add(rb3);

            // Buttons
            btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new Point(600, 370);
            btnOk.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(690, 370);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private PictureBox CreatePictureBox(string imagePath, int x, int y)
        {
            PictureBox pb = new PictureBox();
            pb.Location = new Point(x, y);
            pb.Size = new Size(220, 250);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.BorderStyle = BorderStyle.FixedSingle;
            
            if (File.Exists(imagePath))
            {
                pb.Image = Image.FromFile(imagePath);
            }
            
            return pb;
        }
    }
}
