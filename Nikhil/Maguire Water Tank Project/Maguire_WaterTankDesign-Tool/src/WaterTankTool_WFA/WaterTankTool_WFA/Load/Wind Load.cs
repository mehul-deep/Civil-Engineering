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
using WaterTankTool_WFA.Solver_Equation;
using WaterTankTool_WFA.Tanks;

namespace WaterTankTool_WFA.Load
{

    public partial class Wind_Load : Form
    {
        private WaterTankDbContext _context;
        private Double Q;
        private double calculatedWindShear = 0;
        private double calculatedWindMoment = 0;

        public Wind_Load()
        {
            InitializeComponent();
            var context = WaterTankDbContext.GetInstance();
            _context = context;
            richTextBox1.Text = NotesManager.Notes.WindLoadNotes ?? "q doesnot include velocity pressure exosure coefficient(Kz). The effect will be added for each segment individually.";

            LoadInputBox();
            FillTextBox();
            FillCalculatedValue();
        }

        private void FillTextBox()
        {
            if (comboBox1.Text != null && comboBox1.Text == "C")
            {
                textBox7.Text = WindLoadExposure_C.Zg.ToString();
                textBox8.Text = WindLoadExposure_C.Alpha.ToString();
            }
            else if (comboBox1.Text != null && comboBox1.Text == "D")
            {
                textBox7.Text = WindLoadExposure_D.Zg.ToString();
                textBox8.Text = WindLoadExposure_D.Alpha.ToString();
            }
        }

        private void LoadInputBox()
        {

            var windData = _context.WindLoadEntity.FirstOrDefault();
            if (windData != null)
            {
                comboBox1.Text = windData.Exposure.ToString();
                textBox1.Text = windData.Kzt.ToString();
                //textBox2.Text = windData.Ke.ToString();
                textBox3.Text = windData.Kd.ToString();
                textBox4.Text = windData.G.ToString();
                //textBox5.Text = windData.I.ToString();
                textBox6.Text = windData.V.ToString();
                textBox7.Text = windData.Zg.ToString();
                textBox8.Text = windData.alpha.ToString();
                textBox9.Text = windData.lambda.ToString();
                textBox11.Text = windData.Cf.ToString();

                Q = Lambda.Y * 0.00256 * windData.Kzt * windData.Kd * windData.I * Math.Pow(windData.V, 2);

                richTextBox2.Text = "q = " + Q.ToString("F5");

                UpdateWindLoadsDisplay();
            }
        }

        private void FillCalculatedValue()
        {
            if (textBox1.Text != string.Empty && textBox3.Text != string.Empty && textBox4.Text != string.Empty && textBox6.Text != string.Empty && textBox7.Text != string.Empty && textBox8.Text != string.Empty)
            {
                var Kzt = Double.Parse(textBox1.Text);
                var Kd = Double.Parse(textBox3.Text);
                //var I = Double.Parse(textBox5.Text);
                var V = Double.Parse(textBox6.Text);

                Q = Lambda.Y * 0.00256 * Kzt * Kd * Math.Pow(V, 2);
                richTextBox2.Text = "q = " + Q.ToString("F5");

                UpdateWindLoadsDisplay();
            }
        }

        private void UpdateWindLoadsDisplay()
        {
            try
            {
                var segments = _context.SegmentProperties.ToList();
                var tankProperties = _context.TankProperties.FirstOrDefault();

                if (segments == null || segments.Count == 0 || tankProperties == null)
                {
                    textBoxWindShear.Text = "Awaiting Geom";
                    textBoxWindMoment.Text = "Awaiting Geom";
                    calculatedWindShear = 0;
                    calculatedWindMoment = 0;
                    return;
                }

                double kzt = double.TryParse(textBox1.Text, out double _kzt) ? _kzt : 1.0;
                double kd = double.TryParse(textBox3.Text, out double _kd) ? _kd : 1.0;
                double g = double.TryParse(textBox4.Text, out double _g) ? _g : 1.0;
                double v = double.TryParse(textBox6.Text, out double _v) ? _v : 90.0;
                double zg = double.TryParse(textBox7.Text, out double _zg) ? _zg : 900.0;
                double alpha = double.TryParse(textBox8.Text, out double _a) ? _a : 9.5;
                double lambda = double.TryParse(textBox9.Text, out double _l) ? _l : 0.6;
                double cf = double.TryParse(textBox11.Text, out double _cf) ? _cf : 0.5;

                var tempWind = new WindLoadEntity
                {
                    Exposure = comboBox1.Text,
                    Kzt = kzt,
                    Ke = 1.0,
                    Kd = kd,
                    G = g,
                    I = 1.0,
                    V = v,
                    Zg = zg,
                    alpha = alpha,
                    lambda = lambda,
                    Cf = cf,
                    Q = Q
                };

                double totalWindV = 0;
                double totalWindM = 0;

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    if (AppState.NoOfColumns == 0)
                    {
                        AppState.Load();
                    }

                    var multileg = new Multileg_Cylinders(tempWind);

                    var tankSegment = segments.FirstOrDefault(x => x.SegmentType == "Tanks");
                    if (tankSegment != null && double.TryParse(tankProperties.ProjectedArea, out double projArea))
                    {
                        double fTank = multileg.F_Tank(tankSegment.HeightInitial, tankSegment.HeightFinal, tankSegment.Diameter, projArea);
                        totalWindV += fTank;
                        if (double.TryParse(tankProperties.Centroid, out double centroid))
                        {
                            totalWindM += fTank * (centroid + tankSegment.HeightInitial);
                        }
                    }

                    foreach (var s in segments)
                    {
                        if (s.SegmentType == "Cylinder" || s.SegmentType == "Riser")
                        {
                            double f = multileg.F(s.HeightInitial, s.HeightFinal, s.Diameter, s.SegmentType);
                            double m = multileg.Mbase(s.HeightInitial, s.HeightFinal, s.Diameter, s.SegmentType);
                            totalWindV += f;
                            totalWindM += m;
                        }
                    }
                }
                else
                {
                    var cylinderEquations = new Segment_Cylinder_Equations(tempWind);
                    var conicalEquations = new Segment_Conical_Equations(tempWind);
                    var multileg = new Multileg_Cylinders(tempWind);

                    var tankSegment = segments.FirstOrDefault(x => x.SegmentType == "Tanks");
                    if (tankSegment != null && double.TryParse(tankProperties.ProjectedArea, out double projArea))
                    {
                        double fTank = multileg.F_Tank(tankSegment.HeightInitial, tankSegment.HeightFinal, tankSegment.Diameter, projArea);
                        totalWindV += fTank;
                        if (double.TryParse(tankProperties.Centroid, out double centroid))
                        {
                            totalWindM += fTank * (centroid + tankSegment.HeightInitial);
                        }
                    }

                    foreach (var s in segments)
                    {
                        double f = 0;
                        double m = 0;
                        if (s.SegmentType == "Cylinder")
                        {
                            f = cylinderEquations.F(s.HeightInitial, s.HeightFinal, s.Diameter);
                            m = cylinderEquations.Mbase(s.HeightInitial, s.HeightFinal, s.Diameter);
                        }
                        else if (s.SegmentType == "Base")
                        {
                            var avgDia = ((double)s.DiameterFinal + (double)s.DiameterInitial) / 2;
                            f = conicalEquations.F(s.HeightInitial, s.HeightFinal, avgDia);
                            m = conicalEquations.Mbase(s.HeightInitial, s.HeightFinal, avgDia);
                        }
                        totalWindV += f;
                        totalWindM += m;
                    }
                }

                calculatedWindShear = Math.Round(totalWindV, 4);
                calculatedWindMoment = Math.Round(totalWindM, 4);
                textBoxWindShear.Text = calculatedWindShear.ToString("F2");
                textBoxWindMoment.Text = calculatedWindMoment.ToString("F2");
            }
            catch
            {
                textBoxWindShear.Text = "0.00";
                textBoxWindMoment.Text = "0.00";
                calculatedWindShear = 0;
                calculatedWindMoment = 0;
            }
        }

        private void UpdateWindShearDisplay() => UpdateWindLoadsDisplay();

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillTextBox();
            FillCalculatedValue();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var windLoad = new WindLoadEntity
            {
                Exposure = comboBox1.Text,
                Kzt = double.Parse(textBox1.Text),
                Ke = 1.0,
                Kd = double.Parse(textBox3.Text),
                G = double.Parse(textBox4.Text),
                I = 1.0,
                V = double.Parse(textBox6.Text),
                Zg = double.Parse(textBox7.Text),
                alpha = double.Parse(textBox8.Text),
                lambda = double.Parse(textBox9.Text),
                Cf = double.Parse(textBox11.Text),
                Q = Q,
                WindShear = calculatedWindShear,
                WindMoment = calculatedWindMoment
            };


            AddOrUpdateWindLoad(windLoad);
            DialogResult result = MessageBox.Show("Data saved successfully!", "Confirmation", MessageBoxButtons.OK);
            if(result == DialogResult.OK)
            {
                this.Close();
            }

        }

        public void AddOrUpdateWindLoad(WindLoadEntity windLoad)
        {
            //Check if any WindLoadEntity data already exists in the table
            var existingData = _context.WindLoadEntity.FirstOrDefault();

            if (existingData == null)
            {
                // If no data exists, add the new WindLoadEntity to the table
                _context.WindLoadEntity.Add(windLoad);
            }
            else
            {
                // If data exists, update the existing data with new values
                existingData.Exposure = windLoad.Exposure;
                existingData.Kzt = windLoad.Kzt;
                existingData.Ke = windLoad.Ke;
                existingData.Kd = windLoad.Kd;
                existingData.G = windLoad.G;
                existingData.I = windLoad.I;
                existingData.V = windLoad.V;
                existingData.Zg = windLoad.Zg;
                existingData.alpha = windLoad.alpha;
                existingData.lambda = windLoad.lambda;
                existingData.Cf = windLoad.Cf;
                existingData.Q = windLoad.Q;
                existingData.WindShear = windLoad.WindShear;
            }

            // Save changes to the database
            _context.SaveChanges();
        }


        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();
        }






        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            NotesManager.Notes.WindLoadNotes = richTextBox1.Text;


            NotesManager.SaveNotes();
        }

        private void textBox6_TextChanged_1(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            FillCalculatedValue();

        }
    }
}
