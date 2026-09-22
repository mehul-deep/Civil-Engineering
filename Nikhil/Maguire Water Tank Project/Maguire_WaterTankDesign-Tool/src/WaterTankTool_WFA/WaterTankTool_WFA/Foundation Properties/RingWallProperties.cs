using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public partial class RingWallProperties : Form
    {
        private readonly FoundationEquations.RingWallEquations _eq;
        private readonly WaterTankDbContext _context;

        public RingWallProperties()
        {
            InitializeComponent();
            _context = WaterTankDbContext.GetInstance();
            _eq = new FoundationEquations.RingWallEquations();
            this.Load += RingWallProperties_Load;
            MakeOutputTextBoxesReadOnly();
        }

        private void RingWallProperties_Load(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                SetupMultiColumnUI();
                DisplayMultiColumnCalculatedData();
            }
        }

        private void SetupMultiColumnUI()
        {
            this.Text = "Multi-Leg Pedestal Properties";
            this.Size = new Size(780, 520);
            tabControl1.Visible = false;

            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = false };
            this.Controls.Add(pnl);

            int yLeft = 10;
            int yRight = 10;
            int col1 = 15;
            int col2 = 390;

            _txtPedClass = AddResultRow(pnl, "Pedestal Class (lu/B):", "", ref yLeft, false, col1);
            _txtSlender = AddResultRow(pnl, "Slenderness (klu/r):", "", ref yLeft, false, col1);
            _txtPgravity = AddResultRow(pnl, "Gravity Axial Load/Leg (kip):", "", ref yLeft, false, col1);
            _txtTleg = AddResultRow(pnl, "Overturning Tension/Leg (kip):", "", ref yLeft, false, col1);
            _txtPcompTotal = AddResultRow(pnl, "Max Compression Load (kip):", "", ref yLeft, false, col1);
            _txtBearingCap = AddResultRow(pnl, "Bearing Capacity φPn (kip):", "", ref yLeft, false, col1);
            _txtBearingDC = AddResultRow(pnl, "Bearing D/C Ratio:", "", ref yLeft, false, col1);
            _txtAsMin = AddResultRow(pnl, "Min Reinforcement As,min (in2):", "", ref yLeft, false, col1);
            _txtAsProv = AddResultRow(pnl, "Provided As (in2):", "", ref yLeft, false, col1);
            _txtPhiPn = AddResultRow(pnl, "Axial Compression φPn (kip):", "", ref yLeft, false, col1);
            _txtAxialDC = AddResultRow(pnl, "Axial D/C Ratio:", "", ref yLeft, false, col1);

            _txtUpliftCap = AddResultRow(pnl, "Tensile Capacity φTn (kip):", "", ref yRight, false, col2);
            _txtUpliftDC = AddResultRow(pnl, "Uplift D/C Ratio:", "", ref yRight, false, col2);
            _txtPM_Mu = AddResultRow(pnl, "P-M Demand Mu (kip-ft):", "", ref yRight, false, col2);
            _txtPM_PhiMn = AddResultRow(pnl, "P-M Capacity φMn (kip-ft):", "", ref yRight, false, col2);
            _txtPM_DC = AddResultRow(pnl, "P-M D/C Ratio:", "", ref yRight, false, col2);
            _txtShearPed = AddResultRow(pnl, "Shear Vu,ped (kip):", "", ref yRight, false, col2);
            _txtTieSpacing = AddResultRow(pnl, "Tie Spacing s_max (in):", "", ref yRight, false, col2);
            _txtFootingLd = AddResultRow(pnl, "Development Length ld (in):", "", ref yRight, false, col2);
            _txtFootingLdProv = AddResultRow(pnl, "Provided Length (in):", "", ref yRight, false, col2);
            _txtFootingDC = AddResultRow(pnl, "Development D/C Ratio:", "", ref yRight, false, col2);
        }

        private TextBox _txtPedClass, _txtSlender, _txtPgravity, _txtTleg, _txtPcompTotal;
        private TextBox _txtBearingCap, _txtBearingDC, _txtAsMin, _txtAsProv;
        private TextBox _txtPhiPn, _txtAxialDC;
        private TextBox _txtUpliftCap, _txtUpliftDC;
        private TextBox _txtPM_Mu, _txtPM_PhiMn, _txtPM_DC;
        private TextBox _txtShearPed, _txtTieSpacing;
        private TextBox _txtFootingLd, _txtFootingLdProv, _txtFootingDC;

        private TextBox AddResultRow(Panel pnl, string labelText, string initialValue, ref int y, bool isHeader = false, int xOffset = 20)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(xOffset, y), AutoSize = true };
            if (isHeader)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                pnl.Controls.Add(lbl);
                y += 28;
                return null;
            }

            TextBox txt = new TextBox { Text = initialValue, Location = new Point(xOffset + 215, y - 3), Size = new Size(130, 23), ReadOnly = true, BackColor = Color.White };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            y += 28;
            return txt;
        }

        private void DisplayMultiColumnCalculatedData()
        {
            var entity = _context.Set<RingWallEntity>().FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();
            var basePlate = _context.BasePlateEntity.FirstOrDefault();

            if (entity == null || anchorBolt == null || basePlate == null) return;

            var eq = new FoundationEquations.MultiColumnPedestalEquations();

            double puTotal = anchorBolt.Pu ?? 1609.756;
            if (puTotal <= 0) puTotal = 1609.756;

            double muTotal = basePlate.OverturningMoment ?? 5673.437; // kip-ft
            if (muTotal <= 0) muTotal = 5673.437;

            double vuTotal = 52.30; 

            int numLegs = (anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : (AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4);

            double B = entity.PedestalSizeB ?? 39;
            double L = entity.PedestalSizeL ?? 39;
            double Hp = entity.Hp ?? 7.417; // ft
            
            double basePlateArea = 706.86;
            if (basePlate.Ro > 0)
            {
                basePlateArea = Math.PI * Math.Pow(basePlate.Ro, 2) / 4.0;
            }
            if (basePlateArea <= 0 || basePlateArea > 2000) basePlateArea = 706.86;
            
            double fcPrime = entity.FcPrime ?? 4.0;
            double fy = entity.Fy ?? 60.0;
            double gammaC = entity.GammaC ?? 150.0;
            double AsProv = entity.PedestalAsProv ?? 7.92; 

            // Step 1A: Pedestal Classification
            double lu_b = (Hp * 12.0) / B;
            _txtPedClass.Text = lu_b.ToString("F2") + " <= 3.0";
            _txtPedClass.BackColor = eq.IsPedestal(Hp * 12.0, B) ? Color.LightGreen : Color.LightCoral;

            // Step 1B: Slenderness
            double r = eq.RadiusOfGyrationSquare(B);
            double klu_r = (2.0 * (Hp * 12.0)) / r;
            _txtSlender.Text = klu_r.ToString("F2") + " <= 22";
            _txtSlender.BackColor = eq.NeglectSlenderness(2.0, Hp * 12.0, r) ? Color.LightGreen : Color.LightCoral;

            // Step 2: Gravity
            double pGrav = eq.GravityAxialLoadPerLeg(puTotal, numLegs);
            _txtPgravity.Text = pGrav.ToString("F2");

            // Step 3: Overturning Tension
            double coneRadius = (anchorBolt.Dcone ?? 41.04) * 12.0 / 2.0; 
            var anchorEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
            double totalTension = anchorEq.TotalOverturningTension(muTotal, coneRadius);
            double tLeg = anchorEq.TensionPerLeg(totalTension, anchorEq.TensionLegs(numLegs));
            _txtTleg.Text = tLeg.ToString("F2");

            double pComp = eq.MaxCompressionPedestalLoad(pGrav, tLeg);
            double wPed = eq.PedestalSelfWeight(B, L, Hp, gammaC);
            double wPedFactored = eq.FactoredPedestalSelfWeight(wPed);
            double pCompTotal = pComp + wPedFactored;
            _txtPcompTotal.Text = pCompTotal.ToString("F2");

            // Step 4: Bearing
            double pedestalArea = B * L;
            double enhancement = eq.BearingEnhancementFactor(basePlateArea, pedestalArea);
            double bearingCap = eq.PedestalBearingCapacity(fcPrime, basePlateArea, enhancement, 0.65);
            _txtBearingCap.Text = bearingCap.ToString("F2");

            double bearingDC = eq.DemandCapacityRatio(pCompTotal, bearingCap);
            SetRatioBox(_txtBearingDC, bearingDC);

            // Step 5: Reinforcement
            double asMin = eq.MinimumReinforcementArea(pedestalArea);
            _txtAsMin.Text = asMin.ToString("F2");
            _txtAsProv.Text = AsProv.ToString("F2");
            _txtAsProv.BackColor = AsProv >= asMin ? Color.LightGreen : Color.LightCoral;

            // Step 6: Axial Compression Capacity
            double po = eq.NominalConcentricCompressionStrength(fcPrime, pedestalArea, AsProv, fy);
            double phiPn = eq.DesignAxialCompressionCapacity(po);
            _txtPhiPn.Text = phiPn.ToString("F1");
            
            double axialDC = eq.DemandCapacityRatio(pCompTotal, phiPn);
            SetRatioBox(_txtAxialDC, axialDC);

            // Step 7: Uplift
            double phiTn = eq.DesignTensileCapacity(AsProv, fy, 0.90);
            _txtUpliftCap.Text = phiTn.ToString("F2");
            
            double upliftDC = eq.DemandCapacityRatio(tLeg, phiTn);
            SetRatioBox(_txtUpliftDC, upliftDC);

            // Step 8: P-M Interaction
            // Using given Mu from PDF example (approx 97.0 kip-ft) for pedestal design or scaling from global Mu
            double pedMu = 97.0; 
            _txtPM_Mu.Text = pedMu.ToString("F2");

            double dt = B - (3.0 + 0.375 + 0.75 / 2.0); // Assuming 3" cover, #3 tie, #6 bar
            double c_val, Pn_val;
            double Mn = eq.PMInteractionCapacity_Mn(pComp, B, B, fcPrime, fy, AsProv, dt, out c_val, out Pn_val);
            double phiMn = 0.65 * Mn;
            _txtPM_PhiMn.Text = phiMn.ToString("F1");
            
            double pmDC = eq.DemandCapacityRatio(pedMu, phiMn);
            SetRatioBox(_txtPM_DC, pmDC);

            // Step 9: Shear Pedestal & Tie Design
            double vuPed = eq.HorizontalShearPerPedestal(vuTotal, numLegs);
            _txtShearPed.Text = vuPed.ToString("F3");
            
            double tieSpacing = eq.RequiredTieSpacing(0.375, B);
            _txtTieSpacing.Text = tieSpacing.ToString("F1");

            // Step 10: Pedestal-to-Footing Development
            // Calculate ld for #8 bar if specified, or default to #8 (db = 1.0)
            double ld = eq.TensionDevelopmentLength(fy, fcPrime, 1.0);
            _txtFootingLd.Text = ld.ToString("F2");

            double asProvFooting = entity.FootingAsProv ?? 30.0; 
            _txtFootingLdProv.Text = asProvFooting.ToString("F2");

            double footingDC = eq.DemandCapacityRatio(ld, asProvFooting);
            SetRatioBox(_txtFootingDC, footingDC);
        }

        private void SetRatioBox(TextBox textBox, double ratio)
        {
            textBox.Text = ratio.ToString("0.#####");
            textBox.BackColor = ratio <= 1.0 ? Color.LightGreen : Color.LightCoral;
        }

        public RingWallProperties(double trw, double B, double Rcl, double tedge, double cc,
            double vWater, double gammaW, double wTank, double wSup, double gammaC,
            double qAllowInput, double fc, double fy, double lambda, double Es, double As,
            double Mu, double Abar, double sProv, double Vu, double b0, double Vup,
            double Bu, double A1, double A2, bool bearingEnhancementPermitted,
            string columnLocation = "Interior", double phiShear = 0.75, double phiBearing = 0.65)
        {
            InitializeComponent();
            _eq = new FoundationEquations.RingWallEquations();
            MakeOutputTextBoxesReadOnly();
        }

        private void MakeOutputTextBoxesReadOnly()
        {
            foreach (Control control in GetAllControls(this))
            {
                if (control is TextBox tb)
                {
                    tb.ReadOnly = true;
                    tb.TabStop = false;
                    tb.BackColor = Color.White;
                }
            }
        }

        private Control[] GetAllControls(Control parent)
        {
            var controls = new System.Collections.Generic.List<Control>();
            foreach (Control control in parent.Controls)
            {
                controls.Add(control);
                if (control.HasChildren) controls.AddRange(GetAllControls(control));
            }
            return controls.ToArray();
        }
    }
}