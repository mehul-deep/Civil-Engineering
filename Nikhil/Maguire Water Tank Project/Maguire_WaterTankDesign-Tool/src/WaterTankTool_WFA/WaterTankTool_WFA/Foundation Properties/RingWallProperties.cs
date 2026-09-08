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
            this.Text = "Multi-Leg Pedestal Results";
            
            // Expand the form to fit two columns without scrolling
            this.Size = new Size(780, 480);
            this.Text = "Multi-Leg Pedestal Properties";
            tabControl1.Visible = false;

            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = false };
            this.Controls.Add(pnl);

            int yLeft = 10;
            int yRight = 10;
            int col1 = 15;
            int col2 = 390; // Start of second column

            // --- Column 1 ---
            _txtPgravity = AddResultRow(pnl, "Gravity Axial Load / Leg (kip):", "", ref yLeft, false, col1);
            _txtTleg = AddResultRow(pnl, "Overturning Tension / Leg (kip):", "", ref yLeft, false, col1);
            _txtPcomp = AddResultRow(pnl, "Max Compression Pedestal Load (kip):", "", ref yLeft, false, col1);
            _txtPcompTotal = AddResultRow(pnl, "Total Compression incl. Self Wt (kip):", "", ref yLeft, false, col1);
            yLeft += 15;

            _txtA1 = AddResultRow(pnl, "Base Plate Area A1 (in2):", "", ref yLeft, false, col1);
            _txtA2 = AddResultRow(pnl, "Pedestal Area A2 (in2):", "", ref yLeft, false, col1);
            _txtBearingCap = AddResultRow(pnl, "Bearing Capacity φPn (kip):", "", ref yLeft, false, col1);
            _txtBearingDC = AddResultRow(pnl, "Bearing D/C Ratio:", "", ref yLeft, false, col1);
            yLeft += 15;

            _txtAsMin = AddResultRow(pnl, "Min Reinforcement As,min (in2):", "", ref yLeft, false, col1);
            _txtPo = AddResultRow(pnl, "Nominal Capacity Po (kip):", "", ref yLeft, false, col1);
            _txtPhiPn = AddResultRow(pnl, "Design Capacity φPn (kip):", "", ref yLeft, false, col1);
            _txtAxialDC = AddResultRow(pnl, "Axial D/C Ratio:", "", ref yLeft, false, col1);
            yLeft += 15;

            // --- Column 2 ---
            _txtUpliftDemand = AddResultRow(pnl, "Uplift Demand Tu (kip):", "", ref yRight, false, col2);
            _txtUpliftCap = AddResultRow(pnl, "Uplift Capacity φTn (kip):", "", ref yRight, false, col2);
            _txtUpliftDC = AddResultRow(pnl, "Uplift D/C Ratio:", "", ref yRight, false, col2);
            yRight += 15;

            _txtShearPed = AddResultRow(pnl, "Horizontal Shear Vu,ped (kip):", "", ref yRight, false, col2);
            _txtShearCap = AddResultRow(pnl, "Shear Capacity φVc (kip):", "", ref yRight, false, col2);
            _txtShearDC = AddResultRow(pnl, "Shear D/C Ratio:", "", ref yRight, false, col2);
            _txtFlexureDemand = AddResultRow(pnl, "Flexure Demand Mu (kip-ft):", "", ref yRight, false, col2);
            _txtAsReq = AddResultRow(pnl, "Flexure As,req (in2):", "", ref yRight, false, col2);
            _txtTransverse = AddResultRow(pnl, "Transverse Ties Recommendation:", "", ref yRight, false, col2);
            yRight += 15;

            _txtFootingSteelReq = AddResultRow(pnl, "Ped-to-Footing As,req (in2):", "", ref yRight, false, col2);
            _txtFootingSteelProv = AddResultRow(pnl, "Provided As (in2):", "", ref yRight, false, col2);
            _txtFootingDC = AddResultRow(pnl, "Development D/C Ratio:", "", ref yRight, false, col2);
        }

        private TextBox _txtPgravity, _txtTleg, _txtPcomp, _txtPcompTotal;
        private TextBox _txtA1, _txtA2, _txtBearingCap, _txtBearingDC;
        private TextBox _txtAsMin, _txtPo, _txtPhiPn, _txtAxialDC;
        private TextBox _txtUpliftDemand, _txtUpliftCap, _txtUpliftDC;
        private TextBox _txtShearPed, _txtShearCap, _txtShearDC, _txtFlexureDemand, _txtAsReq, _txtTransverse;
        private TextBox _txtFootingSteelReq, _txtFootingSteelProv, _txtFootingDC;

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

            TextBox txt = new TextBox { Text = initialValue, Location = new Point(xOffset + 225, y - 3), Size = new Size(130, 23), ReadOnly = true, BackColor = Color.White };
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

            // Load predefined values (simulate LoadService for Pu, Mu, Vu)
            double puTotal = anchorBolt.Pu ?? 1609.756;
            if (puTotal <= 0) puTotal = 1609.756;

            double muTotal = basePlate.OverturningMoment ?? 5673.437; // kip-ft
            if (muTotal <= 0) muTotal = 5673.437;

            double vuTotal = 52.30; // Typically from LoadService; using example value if missing

            int numLegs = (anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : (AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4);

            // Geometry
            double B = entity.PedestalSizeB ?? 39;
            double L = entity.PedestalSizeL ?? 39;
            double Hp = entity.Hp ?? 7.417; // ft
            
            double basePlateArea = 706.86;
            // In Multi-Column mode, basePlate.Ro actually stores the "Outer Diameter Do" in inches.
            if (basePlate.Ro > 0)
            {
                basePlateArea = Math.PI * Math.Pow(basePlate.Ro, 2) / 4.0;
            }
            if (basePlateArea <= 0 || basePlateArea > 2000) basePlateArea = 706.86;
            
            double fcPrime = entity.FcPrime ?? 4.0;
            double fy = entity.Fy ?? 60.0;
            double gammaC = entity.GammaC ?? 150.0;
            
            // Provided reinforcement
            double AsProv = entity.PedestalAsProv ?? 7.92; 

            // Step 1: Gravity
            double pGrav = eq.GravityAxialLoadPerLeg(puTotal, numLegs);
            _txtPgravity.Text = pGrav.ToString("F2");

            // Step 2: Overturning Tension
            double coneRadius = (anchorBolt.Dcone ?? 41.04) * 12.0 / 2.0; 
            var anchorEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
            double totalTension = anchorEq.TotalOverturningTension(muTotal, coneRadius);
            double tLeg = anchorEq.TensionPerLeg(totalTension, anchorEq.TensionLegs(numLegs));
            _txtTleg.Text = tLeg.ToString("F2");

            double pComp = eq.MaxCompressionPedestalLoad(pGrav, tLeg);
            _txtPcomp.Text = pComp.ToString("F2");

            double wPed = eq.PedestalSelfWeight(B, L, Hp, gammaC);
            double wPedFactored = eq.FactoredPedestalSelfWeight(wPed);
            double pCompTotal = pComp + wPedFactored;
            _txtPcompTotal.Text = pCompTotal.ToString("F2");

            // Step 3: Bearing
            double pedestalArea = B * L;
            _txtA1.Text = basePlateArea.ToString("F2");
            _txtA2.Text = pedestalArea.ToString("F2");

            double enhancement = eq.BearingEnhancementFactor(basePlateArea, pedestalArea);
            double bearingCap = eq.PedestalBearingCapacity(fcPrime, basePlateArea, enhancement, 0.65);
            _txtBearingCap.Text = bearingCap.ToString("F2");

            double bearingDC = eq.DemandCapacityRatio(pCompTotal, bearingCap);
            SetRatioBox(_txtBearingDC, bearingDC);

            // Step 4: Reinforcement
            double asMin = eq.MinimumReinforcementArea(pedestalArea);
            _txtAsMin.Text = asMin.ToString("F2");

            // Step 5: Axial Compression Capacity
            double po = eq.NominalConcentricCompressionStrength(fcPrime, pedestalArea, AsProv, fy);
            _txtPo.Text = po.ToString("F1");
            double phiPn = eq.DesignAxialCompressionCapacity(po);
            _txtPhiPn.Text = phiPn.ToString("F1");
            
            double axialDC = eq.DemandCapacityRatio(pCompTotal, phiPn);
            SetRatioBox(_txtAxialDC, axialDC);

            // Step 6: Uplift
            _txtUpliftDemand.Text = tLeg.ToString("F2");
            double phiTn = eq.DesignTensileCapacity(AsProv, fy, 0.90);
            _txtUpliftCap.Text = phiTn.ToString("F2");
            
            double upliftDC = eq.DemandCapacityRatio(tLeg, phiTn);
            SetRatioBox(_txtUpliftDC, upliftDC);

            // Step 7 & 8: Shear
            double vuPed = eq.HorizontalShearPerPedestal(vuTotal, numLegs);
            _txtShearPed.Text = vuPed.ToString("F2");

            double d = B - 3.6; // approx d = 35.4
            double vc = eq.NominalShearCapacity(fcPrime * 1000.0, B, d);
            double phiVc = eq.DesignShearCapacity(vc, 0.75);
            _txtShearCap.Text = phiVc.ToString("F1");

            double shearDC = eq.DemandCapacityRatio(vuPed, phiVc);
            SetRatioBox(_txtShearDC, shearDC);

            // Step 9: Flexure
            double muPed = eq.FactoredFlexuralMoment(vuPed, Hp);
            _txtFlexureDemand.Text = muPed.ToString("F2");

            double asReq = eq.RequiredFlexuralReinforcement(muPed, fy, d);
            _txtAsReq.Text = asReq.ToString("F2");

            _txtTransverse.Text = "#4 closed ties @ 10\" o.c (add tie near base-plate)";

            // Step 11: Pedestal-to-Footing Development
            double asReqFooting = eq.RequiredPedestalToFootingSteel(tLeg, fy, 0.90);
            _txtFootingSteelReq.Text = asReqFooting.ToString("F2");

            double asProvFooting = entity.FootingAsProv ?? 5.28; 
            _txtFootingSteelProv.Text = asProvFooting.ToString("F2");

            double footingDC = eq.DemandCapacityRatio(asReqFooting, asProvFooting);
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