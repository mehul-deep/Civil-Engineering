using System;
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
                PopulateBoltDropdown();
            }
        }

        private void DisplayCalculatedData()
        {
            if (_entity == null) return;

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
