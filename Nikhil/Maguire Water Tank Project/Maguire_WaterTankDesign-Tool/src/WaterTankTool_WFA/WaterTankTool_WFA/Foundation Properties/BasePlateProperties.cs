using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public partial class BasePlateProperties : Form
    {
        private readonly WaterTankDbContext _context;
        private BasePlateEntity? _entity;

        public BasePlateProperties()
        {
            InitializeComponent();
            _context = WaterTankDbContext.GetInstance();
            this.Load += BasePlateProperties_Load;
        }

        private void BasePlateProperties_Load(object? sender, EventArgs e)
        {
            _entity = _context.BasePlateEntity.FirstOrDefault();

            if (_entity != null)
            {
                DisplayCalculatedData();
                if (AppState.CurrentTankType != TankType.MultiColumn)
                {
                    PopulateBoltDropdown();
                }
            }
        }

        private void DisplayCalculatedData()
        {
            if (_entity == null) return;

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                DisplayMultiColumnCalculatedData();
                return;
            }

            var eq = new FoundationEquations.BasePlateEquations();

            // Geometric results
            double grossArea = eq.GrossArea(_entity.Ro, _entity.Ri, _entity.Theta);
            double netArea = eq.NetArea(_entity.Ro, _entity.Ri, _entity.Theta, _entity.Nh, _entity.Dh);
            double volume = eq.Volume(_entity.Ro, _entity.Ri, _entity.Theta, _entity.T);
            double weightPerSegment = eq.WeightPerSegment(_entity.Ro, _entity.Ri, _entity.Theta, _entity.T, _entity.Rs);
            double totalWeight = eq.TotalWeight(weightPerSegment, _entity.N);

            double outerArcLength = eq.OuterArcLength(_entity.Ro, _entity.Theta);
            double innerArcLength = eq.InnerArcLength(_entity.Ri, _entity.Theta);
            double radialWidth = eq.RadialWidth(_entity.Ro, _entity.Ri);
            double centroidRadius = eq.CentroidRadius(_entity.Ro, _entity.Ri, _entity.Theta);
            double centroidAngle = eq.CentroidAngle(_entity.A ?? 0, _entity.Theta);

            textBox1.Text = grossArea.ToString("F4");
            textBox2.Text = netArea.ToString("F4");
            textBox3.Text = volume.ToString("F4");
            textBox4.Text = weightPerSegment.ToString("F4");
            textBox5.Text = totalWeight.ToString("F4");

            textBox6.Text = outerArcLength.ToString("F4");
            textBox7.Text = innerArcLength.ToString("F4");
            textBox8.Text = radialWidth.ToString("F4");
            textBox9.Text = centroidRadius.ToString("F4");
            textBox10.Text = centroidAngle.ToString("F4");

            // Structural results
            double coneDiameterFt = _entity.Dbp; // cone diameter Dcone
            double momentKipFt = _entity.OverturningMoment ?? 0;
            double pu = _entity.Pu ?? 0;

            double mKipIn = eq.ConvertMomentToKipIn(momentKipFt);
            double mStrip = eq.CircumferentialMomentPerUnitStrip(mKipIn, coneDiameterFt);

            double areaA1 = grossArea * 144.0; 
            double x1 = (_entity.Ro - _entity.Ri) * 12.0; // width of base plate (in)
            double x2 = (_entity.Wrw ?? 2.0 * (_entity.Ro - _entity.Ri)) * 12.0;
            
            double fcPrimeKsi = _entity.Fc_prime > 100 ? _entity.Fc_prime / 1000.0 : _entity.Fc_prime;
            double fp = (_entity.A2.HasValue && _entity.A2.Value > 0)
                ? eq.MaximumDesignBearingStress(fcPrimeKsi, _entity.A2.Value, areaA1, 0.90)
                : eq.MaximumDesignBearingStress(fcPrimeKsi, x2, x1, 0.90);

            double eVal = eq.EquivalentEccentricity(mStrip, pu, coneDiameterFt);
            
            double designStripN = x1; // Base plate width N = Ro - Ri
            double bearingLimit = eq.BearingConditionLimit(designStripN);

            double mCritical = eq.CriticalSection(designStripN);
            double mPlu = eq.StripPlasticMoment(mStrip);
            double treq = eq.RequiredThickness(mPlu, _entity.Fy, 0.90);
            
            double compactness = eq.CompactnessRatio(designStripN, _entity.T);

            textBox11.Text = fp.ToString("F4");
            textBox12.Text = eVal.ToString("F4");
            
            textBox13.Text = treq.ToString("F4");
            bool thicknessPass = _entity.T >= treq;
            textBox13.BackColor = thicknessPass ? Color.LightGreen : Color.LightCoral;
            labelThicknessStatus.Text = thicknessPass ? "PASS" : "FAIL";
            labelThicknessStatus.ForeColor = thicknessPass ? Color.Green : Color.Red;

            textBox14.Text = compactness.ToString("F4");
            bool compactnessPass = compactness <= 11.22;
            textBox14.BackColor = compactnessPass ? Color.LightGreen : Color.LightCoral;
            labelCompactnessStatus.Text = compactnessPass ? "PASS" : "FAIL";
            labelCompactnessStatus.ForeColor = compactnessPass ? Color.Green : Color.Red;

            textBox21.Text = mStrip.ToString("F4");
            textBox22.Text = mCritical.ToString("F4");

            textBox17.Text = bearingLimit.ToString("F4");
            textBox18.Text = mPlu.ToString("F4");

            // Centroid
            double xc = eq.CentroidX(_entity.Ro, _entity.Ri, _entity.Theta, _entity.A ?? 0);
            double yc = eq.CentroidY(_entity.Ro, _entity.Ri, _entity.Theta, _entity.A ?? 0);
            textBox15.Text = xc.ToString("F4");
            textBox16.Text = yc.ToString("F4");

            // Save results back to entity
            _entity.Fp = fp;
            _entity.Phi_Pp = eVal; // Saved eccentricity temporarily
            _entity.BearingUtilization = compactness;
            _entity.L = mCritical;
            _entity.Mu = mPlu;
            _entity.T_req = treq;
            _entity.ThicknessUtilization = mStrip;

            _context.SaveChanges();
        }

        private void DisplayMultiColumnCalculatedData()
        {
            if (_entity == null) return;

            this.Text = "Multi-Leg Base Plate Design Results";

            // Hide Single-Column specific centroid and bolt dropdown controls
            label16.Visible = textBox16.Visible = false; // Centroid Y
            labelBoltSelect.Visible = comboBoxBoltSelect.Visible = false;
            labelLocationDetail.Visible = labelAngleDetail.Visible = false;
            labelXCoordDetail.Visible = labelYCoordDetail.Visible = false;
            textBoxAngleDetail.Visible = textBoxXCoordDetail.Visible = textBoxYCoordDetail.Visible = textBoxLocationDetail.Visible = false;
            groupBoxBoltDetail.Visible = false;

            // Re-align Left Column to remove gaps
            label21.Location = new System.Drawing.Point(20, 235);
            textBox21.Location = new System.Drawing.Point(210, 232);

            // Re-align Right Column to remove gaps
            label10.Visible = true;
            textBox10.Visible = true;
            label10.Location = new System.Drawing.Point(330, 148);
            textBox10.Location = new System.Drawing.Point(530, 145);

            label15.Visible = true;
            textBox15.Visible = false; // Hide the textbox, we only need the label for PASS/FAIL
            label15.Location = new System.Drawing.Point(635, 119);
            label15.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            label13.Location = new System.Drawing.Point(330, 177);
            textBox13.Location = new System.Drawing.Point(530, 174);
            labelThicknessStatus.Location = new System.Drawing.Point(635, 177);

            label14.Location = new System.Drawing.Point(330, 206);
            textBox14.Location = new System.Drawing.Point(530, 203);
            labelCompactnessStatus.Location = new System.Drawing.Point(635, 206);

            label22.Location = new System.Drawing.Point(330, 235);
            textBox22.Location = new System.Drawing.Point(530, 232);

            label17.Location = new System.Drawing.Point(330, 264);
            textBox17.Location = new System.Drawing.Point(530, 261);

            label18.Location = new System.Drawing.Point(330, 293);
            textBox18.Location = new System.Drawing.Point(530, 290);

            // Resize form to fit snugly without bottom empty space
            groupBox1.Height = 330;
            this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, 365);

            // Update Labels for Multi-Column
            label1.Text = "Base Plate Area A1 (in²)";
            label2.Text = "Pedestal Area A2 (in²)";
            label3.Text = "Plate Volume (in³)";
            label4.Text = "Weight per Plate (kips)";
            label5.Text = "Total Weight (all legs) (kips)";

            label6.Text = "Factored Mu,ped (kip-ft)";
            label7.Text = "Factored Mu,ped (kip-in)";
            label8.Text = "Load Pu,ped (kips)";
            label9.Text = "Bearing Capacity Pn (kips)";
            label10.Text = "Load Pu,comp (kips)";

            label11.Text = "Bearing Stress Fp (ksi)";
            label12.Text = "Eccentricity e (in)";
            label21.Text = "Bearing Pressure q (ksi)";
            label22.Text = "Cantilever Proj m (in)";
            label17.Text = "Bearing Limit N/6 (in)";
            label18.Text = "Plastic Moment Mplu (kip-in/in)";
            label13.Text = "Required Thickness tp (in)";
            label14.Text = "Bearing Stress Limit (ksi)";

            var eq = new FoundationEquations.MultiColumnBasePlateEquations();

            // Fetch Anchor Bolt for Pedestal Size and Number of Pedestals
            var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();
            int totalLegs = (anchorBolt != null && anchorBolt.Ns > 0) ? anchorBolt.Ns.Value : 4; // Default 4 legs

            double p = 39.0;
            double l = 39.0;
            if (anchorBolt != null && anchorBolt.PedestalSize > 0)
            {
                p = anchorBolt.PedestalSize.Value;
                l = anchorBolt.PedestalSize.Value;
            }

            double b = _entity.Ro > 0 ? _entity.Ro : 30.0;
            double n = _entity.Ri > 0 ? _entity.Ri : 30.0;
            double dpip = _entity.Dbp > 0 ? _entity.Dbp : 20.04;
            double t = _entity.T > 0 ? _entity.T : 1.50;
            double fy = _entity.Fy > 0 ? _entity.Fy : 36.0;
            double fcPrimePsi = _entity.Fc_prime > 0 ? _entity.Fc_prime : 4000.0;
            double fcPrimeKsi = fcPrimePsi > 100 ? fcPrimePsi / 1000.0 : fcPrimePsi;
            double totalMuKipFt = _entity.OverturningMoment ?? 0;
            double totalPuKips = _entity.Pu ?? 0;

            // Calculations using our new engine!
            double a1 = eq.BasePlateArea(b, n);
            double a2 = eq.PedestalArea(p, l);
            double fp = eq.MaximumBearingStress(fcPrimeKsi, a2, a1, 0.65);
            double fpLimit = eq.BearingStressLimit(fcPrimeKsi, 0.65);
            double pn = eq.BearingCapacity(fp, a1);

            double muPedFt = eq.FactoredMomentPerPedestal(totalMuKipFt, totalLegs);
            double muPedIn = eq.ConvertMomentToKipIn(muPedFt);
            double puPed = totalLegs > 0 ? Math.Round(totalPuKips / totalLegs, 2) : totalPuKips;
            double puComp = eq.AppliedLoadPerCompressionPedestal(totalPuKips, totalLegs);
            double eVal = eq.EquivalentEccentricity(muPedIn, puPed);
            double nLimit = eq.BearingConditionLimit(n);

            double colArea = eq.ColumnBearingArea(dpip);
            double qVal = eq.BearingPressure(puPed, colArea);
            double mVal = eq.PlateProjection(n, dpip);
            double mplu = eq.StripPlasticMoment(qVal, mVal);
            double treq = eq.RequiredThickness(mplu, fy, 0.90);

            // Weights
            double volPerPlate = a1 * t; // cu. in.
            double weightPerPlate = (volPerPlate / 1728.0) * 0.490; // kips
            double totalWeight = weightPerPlate * totalLegs;

            // Display results in TextBoxes
            textBox1.Text = a1.ToString("F2", CultureInfo.InvariantCulture);
            textBox2.Text = a2.ToString("F2", CultureInfo.InvariantCulture);
            textBox3.Text = volPerPlate.ToString("F2", CultureInfo.InvariantCulture);
            textBox4.Text = weightPerPlate.ToString("F4", CultureInfo.InvariantCulture);
            textBox5.Text = totalWeight.ToString("F4", CultureInfo.InvariantCulture);

            textBox6.Text = muPedFt.ToString("F2", CultureInfo.InvariantCulture);
            textBox7.Text = muPedIn.ToString("F2", CultureInfo.InvariantCulture);
            textBox8.Text = puPed.ToString("F2", CultureInfo.InvariantCulture);
            textBox9.Text = pn.ToString("F2", CultureInfo.InvariantCulture);
            textBox10.Text = puComp.ToString("F2", CultureInfo.InvariantCulture);
            
            bool capacityPass = pn >= puComp;
            textBox9.BackColor = capacityPass ? Color.LightGreen : Color.LightCoral;
            label15.Text = capacityPass ? "PASS" : "FAIL";
            label15.ForeColor = capacityPass ? Color.Green : Color.Red;

            textBox11.Text = fp.ToString("F4", CultureInfo.InvariantCulture);
            textBox12.Text = eVal.ToString("F2", CultureInfo.InvariantCulture);
            textBox21.Text = qVal.ToString("F4", CultureInfo.InvariantCulture);
            textBox22.Text = mVal.ToString("F4", CultureInfo.InvariantCulture);
            textBox17.Text = nLimit.ToString("F4", CultureInfo.InvariantCulture);
            textBox18.Text = mplu.ToString("F4", CultureInfo.InvariantCulture);

            textBox13.Text = treq.ToString("F4", CultureInfo.InvariantCulture);
            bool thicknessPass = t >= treq;
            textBox13.BackColor = thicknessPass ? Color.LightGreen : Color.LightCoral;
            labelThicknessStatus.Text = thicknessPass ? "PASS" : "FAIL";
            labelThicknessStatus.ForeColor = thicknessPass ? Color.Green : Color.Red;

            textBox14.Text = fpLimit.ToString("F4", CultureInfo.InvariantCulture);
            bool bearingPass = fp <= fpLimit;
            textBox14.BackColor = bearingPass ? Color.LightGreen : Color.LightCoral;
            labelCompactnessStatus.Text = bearingPass ? "PASS" : "FAIL";
            labelCompactnessStatus.ForeColor = bearingPass ? Color.Green : Color.Red;

            // Save results back to entity
            _entity.Fp = fp;
            _entity.Phi_Pp = eVal;
            _entity.BearingUtilization = fp / (fpLimit > 0 ? fpLimit : 1.0);
            _entity.L = mVal;
            _entity.Mu = mplu;
            _entity.T_req = treq;
            _entity.ThicknessUtilization = qVal;

            _context.SaveChanges();
        }

        private void PopulateBoltDropdown()
        {
            if (_entity == null) return;

            comboBoxBoltSelect.Items.Clear();
            int totalBolts = _entity.Nb ?? (_entity.N * _entity.Nh);
            if (totalBolts <= 0) totalBolts = _entity.Nh;

            for (int i = 1; i <= totalBolts; i++)
            {
                comboBoxBoltSelect.Items.Add($"Bolt {i}");
            }

            if (comboBoxBoltSelect.Items.Count > 0)
            {
                comboBoxBoltSelect.SelectedIndex = 0;
            }
        }

        private void comboBoxBoltSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_entity == null) return;

            int boltIndexInSegment = comboBoxBoltSelect.SelectedIndex;
            var boltEq = new FoundationEquations.AnchorBoltEquations();
            
            double rb = _entity.Rb ?? (_entity.Ro + _entity.Ri) / 2.0;
            double startAngle = _entity.A ?? 0;
            
            // If there's only 1 bolt, it's at startAngle. 
            // If multiple, they are spaced across Theta.
            double totalBolts = _entity.Nb ?? (_entity.N * _entity.Nh);
            double step = totalBolts > 0 ? 360.0 / totalBolts : (_entity.Nh > 1 ? _entity.Theta / (_entity.Nh - 1) : 0);
            double angle = startAngle + (boltIndexInSegment * step);

            double x = boltEq.BoltXCoordinate(rb, angle);
            double y = boltEq.BoltYCoordinate(rb, angle);

            textBoxAngleDetail.Text = angle.ToString("F2");
            textBoxXCoordDetail.Text = x.ToString("F2");
            textBoxYCoordDetail.Text = y.ToString("F2");
            textBoxLocationDetail.Text = GetBoltLocationDescription(angle);
        }

        private string GetBoltLocationDescription(double angle)
        {
            double normAngle = angle % 360;
            if (normAngle < 0) normAngle += 360;

            if (normAngle == 0 || normAngle == 360) return "Right";
            if (normAngle > 0 && normAngle < 90) return "Upper-right";
            if (normAngle == 90) return "Top";
            if (normAngle > 90 && normAngle < 180) return "Upper-left";
            if (normAngle == 180) return "Left";
            if (normAngle > 180 && normAngle < 270) return "Lower-left";
            if (normAngle == 270) return "Bottom";
            if (normAngle > 270 && normAngle < 360) return "Lower-right";

            return "Unknown";
        }
    }
}
