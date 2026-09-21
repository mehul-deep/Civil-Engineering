using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaterTankTool_WFA.Foundation_Properties;

namespace WaterTankTool_WFA.Foundation_Design
{
    public partial class OutputModal : Form
    {
        public OutputModal()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AnchorBoltProperties anchorBoltProperties = new AnchorBoltProperties();
            anchorBoltProperties.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BasePlateProperties basePlateProperties = new BasePlateProperties();
            basePlateProperties.ShowDialog();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            RingWallProperties ringWallProperties = new RingWallProperties();
            ringWallProperties.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                FootingProperties footingProperties = new FootingProperties();
                footingProperties.ShowDialog();
            }
            else
            {
                MessageBox.Show("Footing Analysis for Single-Leg is integrated into Ring Wall.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
