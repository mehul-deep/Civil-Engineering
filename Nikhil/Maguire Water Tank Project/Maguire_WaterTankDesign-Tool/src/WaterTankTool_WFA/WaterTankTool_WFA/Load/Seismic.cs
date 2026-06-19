using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaterTankTool_WFA.Constants;
using WaterTankTool_WFA.Designer_Notes;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Migrations;

namespace WaterTankTool_WFA.Load
{
    public partial class Seismic : Form
    {
        private WaterTankDbContext _context;

        public WaterTank _waterTankForm;
        public Seismic(WaterTank waterTank)
        {
            InitializeComponent();
            var context = WaterTankDbContext.GetInstance();
            _context = context;
            _waterTankForm = waterTank;
            richTextBox1.Text = NotesManager.Notes.SeismicLoadNotes ?? "";

            ShowInputField();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        private void ShowInputField()
        {
            double seismic_base_moment = 0.0;
            try 
            {
                Load_Combinations load_Combinations = new Load_Combinations();
                seismic_base_moment = load_Combinations.seismicBaseMoment;
            }
            catch 
            {
                // Safely ignore if Load_Combinations fails due to missing external data
            }

            var existingData = _context.SeismicLoadEntity.FirstOrDefault();
            if (existingData != null)
            {
                textBox14.Text = existingData.Ss.ToString();
                textBox15.Text = existingData.S1.ToString();
                comboBox1.Text = existingData.SiteClass.ToString();
                textBox5.Text = existingData.Fa.ToString();
                textBox4.Text = existingData.Fv.ToString();
                textBox7.Text = existingData.Sds.ToString();
                textBox6.Text = existingData.Sd1.ToString();
                textBox3.Text = existingData.Ri.ToString();
                textBox11.Text = existingData.Ie.ToString();
                textBox12.Text = existingData.Tl.ToString();
                textBox8.Text = existingData.Ti.ToString();
                textBox2.Text = existingData.Ts.ToString();
                textBox9.Text = existingData.Sa.ToString();
                textBox1.Text = existingData.Lambda.ToString();
                textBox10.Text = existingData.Ai.ToString();
            }
            textBox16.Text = seismic_base_moment.ToString("F5");
        }


        private double ParseDoubleSafe(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            if (double.TryParse(value, out double result)) return result;
            return 0.0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<string> allowedValues = new List<string> { "A", "B", "C", "D", "E", "F" };

            if (string.IsNullOrWhiteSpace(textBox14.Text) ||
                 string.IsNullOrWhiteSpace(textBox15.Text) ||
                 string.IsNullOrWhiteSpace(comboBox1.Text) || string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text) || string.IsNullOrWhiteSpace(textBox3.Text) ||
                string.IsNullOrWhiteSpace(textBox4.Text) || string.IsNullOrWhiteSpace(textBox5.Text) || string.IsNullOrWhiteSpace(textBox6.Text) || string.IsNullOrWhiteSpace(textBox7.Text) ||
                string.IsNullOrWhiteSpace(textBox8.Text) || string.IsNullOrWhiteSpace(textBox9.Text) || string.IsNullOrWhiteSpace(textBox10.Text) || string.IsNullOrWhiteSpace(textBox11.Text) || string.IsNullOrWhiteSpace(textBox12.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(textBox1.Text, out double live_Load) || !double.TryParse(textBox5.Text, out double area) || !double.TryParse(textBox9.Text, out double a) || !double.TryParse(textBox12.Text, out double b) ||
                !double.TryParse(textBox2.Text, out double c) || !double.TryParse(textBox6.Text, out double f) || !double.TryParse(textBox10.Text, out double g) ||
                !double.TryParse(textBox3.Text, out double total) || !double.TryParse(textBox7.Text, out double arfea) || !double.TryParse(textBox11.Text, out double h) ||
                !double.TryParse(textBox4.Text, out double d) || !double.TryParse(textBox8.Text, out double i) || double.TryParse(comboBox1.Text, out double ggwp))
            {
                MessageBox.Show("Please enter valid numbers.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!(allowedValues.Contains(comboBox1.Text)))
            {
                MessageBox.Show("Please enter valid Site class from Dropdown.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var seismicLoad = new SeismicLoadEntity
            {
                Ss = double.Parse(textBox14.Text),
                S1 = double.Parse(textBox15.Text),
                SiteClass = comboBox1.Text,
                Fa = double.Parse(textBox5.Text),
                Fv = double.Parse(textBox4.Text),
                Sds = double.Parse(textBox7.Text),
                Sd1 = double.Parse(textBox6.Text),
                Ri = double.Parse(textBox3.Text),
                Ie = double.Parse(textBox11.Text),
                Tl = double.Parse(textBox12.Text),
                Ti = double.Parse(textBox8.Text),
                Ts = double.Parse(textBox2.Text),
                Sa = double.Parse(textBox9.Text),
                Lambda = double.Parse(textBox1.Text),
                Ai = double.Parse(textBox10.Text),
                V = double.Parse(textBox13.Text)
            };



            AddOrUpdateSeismicLoad(seismicLoad);
            _waterTankForm.UpdateLoadStatus();
            DialogResult result = MessageBox.Show("Data saved successfully!", "Confirmation", MessageBoxButtons.OK);

            if(result == DialogResult.OK)
            {
                this.Close();
            }
        }

        public void AddOrUpdateSeismicLoad(SeismicLoadEntity seismicLoad)
        {
            //Check if any WindLoadEntity data already exists in the table
            var existingData = _context.SeismicLoadEntity.FirstOrDefault();

            if (existingData == null)
            {
                // If no data exists, add the new WindLoadEntity to the table
                _context.SeismicLoadEntity.Add(seismicLoad);
            }
            else
            {
                // If data exists, update the existing data with new values
                existingData.Ss = seismicLoad.Ss;
                existingData.S1 = seismicLoad.S1;
                existingData.SiteClass = seismicLoad.SiteClass;
                existingData.Fa = seismicLoad.Fa;
                existingData.Fv = seismicLoad.Fv;
                existingData.Sds = seismicLoad.Sds;
                existingData.Sd1 = seismicLoad.Sd1;
                existingData.Ri = seismicLoad.Ri;
                existingData.Ie = seismicLoad.Ie;
                existingData.Tl = seismicLoad.Tl;
                existingData.Ti = seismicLoad.Ti;
                existingData.Ts = seismicLoad.Ts;
                existingData.Sa = seismicLoad.Sa;
                existingData.Lambda = seismicLoad.Lambda;
                existingData.Ai = seismicLoad.Ai;
                existingData.V = seismicLoad.V;
            }

            // Save changes to the database
            _context.SaveChanges();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            setFaFvValues();
            Sd1_textchange();
            Sds_textChange();
            Ts_TextChanged();
            Ai_TextChanged();

        }

        private void setFaFvValues()
        {
            if (textBox15 != null && textBox14 != null)
            {
                double fvValue = SiteClassTable.GetFvValue(comboBox1.Text, double.Parse(textBox15.Text));
                double faValue = SiteClassTable.GetFaValue(comboBox1.Text, double.Parse(textBox14.Text));

                textBox4.Text = fvValue.ToString();
                textBox5.Text = faValue.ToString();
            }
        }


        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            setFaFvValues();
            Sds_textChange();
            Ts_TextChanged();
            Ai_TextChanged();


        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            setFaFvValues();
            Sd1_textchange();
            Ts_TextChanged();
            Ai_TextChanged();


        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            Sa_TextChanged();

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            Sds_textChange();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Sd1_textchange();
        }

        private void Sd1_textchange()
        {
            if (textBox4 != null && textBox4.Text != "NaN")
            {
                var Sm1 = double.Parse(textBox4.Text) * double.Parse(textBox15.Text);
                double Sd1 = Math.Round(((double)2 / 3) * Sm1, 4);

                textBox6.Text = Sd1.ToString();
            }
            else if (textBox4?.Text == "NaN")
            {
                textBox6.Text = "";
            }
        }

        private void Sds_textChange()
        {
            if (textBox5 != null && textBox5.Text != "NaN")
            {
                var Sms = double.Parse(textBox5.Text) * double.Parse(textBox14.Text);
                double Sds = Math.Round(((double)2 / 3) * Sms, 4);
                textBox7.Text = Sds.ToString();
                //textBox9.Text = Sds.ToString();
            }
            else if (textBox5?.Text == "NaN")
            {
                textBox7.Text = "";
                //textBox9.Text = "";
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Ts_TextChanged();
            Sa_TextChanged();

        }

        private void Ts_TextChanged()
        {
            if (textBox6 != null && textBox6.Text != "NaN" && textBox7 != null && textBox7.Text != "NaN" && textBox6.Text != "" && textBox7.Text != "")
            {
                var res = double.Parse(textBox6.Text) / double.Parse(textBox7.Text);
                textBox2.Text = Math.Round(res, 4).ToString();
            }
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            Ai_TextChanged();
        }

        private void Ai_TextChanged()
        {
            if (validFields())
            {

                var lambda = double.Parse(textBox1.Text);
                var Sa = double.Parse(textBox9.Text);
                var Ie = double.Parse(textBox11.Text);
                var Ri = double.Parse(textBox3.Text);
                var S1 = double.Parse(textBox15.Text);

                var val1 = (lambda * Sa * Ie) / Ri;

                var val2 = (0.36 * S1 * Ie) / Ri;

                var res = Math.Max(val1, val2);
                textBox10.Text = Math.Round((double)res, 5).ToString();


                List<SegmentProperties> segmentData = _context.SegmentProperties.ToList();


                Load_Combinations lc = new Load_Combinations();

                var totalLoad = lc.GetTotalSegmentLoad(segmentData);

                var v = Math.Round(res * totalLoad, 4);

                textBox13.Text = v.ToString();


            }

        }

        private bool validFields()
        {
            var res = false;
            if (textBox1.Text != "" && textBox9.Text != "" && textBox11.Text != "" && textBox3.Text != "" && textBox3.Text != "" && textBox15.Text != "" && textBox1.Text != null && textBox9.Text != null && textBox11.Text != null && textBox3.Text != null && textBox3.Text != null && textBox15.Text != null)
            {
                res = true;
            }
            return res;
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            Sa_TextChanged();
            Ai_TextChanged();
        }

        private void Sa_TextChanged()
        {
            var abc = false;
            if (textBox8.Text != "" && textBox2.Text != "" && textBox7.Text != "" && textBox6.Text != "" && textBox12.Text != "" && textBox8.Text != null && textBox2.Text != null && textBox7.Text != null && textBox6.Text != null && textBox12.Text != null)
            {
                abc = true;
            }

            if (abc)
            {
                var Ti = double.Parse(textBox8.Text);
                var Ts = double.Parse(textBox2.Text);
                var Sds = double.Parse(textBox7.Text);
                var Sd1 = double.Parse(textBox6.Text);
                var Tl = double.Parse(textBox12.Text);



                if (Ti >= 0 && Ti <= Ts)
                {
                    textBox9.Text = Math.Round(Sds, 4).ToString();
                }

                else if (Ti > Ts && Ti <= Tl)
                {
                    var res = Sd1 / Ti;
                    textBox9.Text = Math.Round(res, 4).ToString();
                }
                else if (Ti > Tl)
                {
                    var result = (Sd1 * Tl) / Math.Pow(Ti, 2);
                    textBox9.Text = Math.Round(result, 4).ToString();
                }
            }


        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            Sa_TextChanged();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            NotesManager.Notes.SeismicLoadNotes = richTextBox1.Text;

            // Immediately save changes to the single JSON file
            NotesManager.SaveNotes();
        }

        private void textBox13_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
