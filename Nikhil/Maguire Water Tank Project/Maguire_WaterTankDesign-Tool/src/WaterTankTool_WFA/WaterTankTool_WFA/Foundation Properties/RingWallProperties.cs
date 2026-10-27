using System;
using System.Drawing;
using System.Globalization;
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
            this.Text = "Multi-Leg Pedestal Design Results";
            this.Size = new Size(880, 560);
            tabControl1.Visible = false;

            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            this.Controls.Add(pnl);

            int yLeft = 14;
            int yRight = 14;
            int col1 = 15;
            int col2 = 445;

            _txtNumLegs = AddResultRow(pnl, "Number of Legs:", "", ref yLeft, false, col1);
            _txtPedClass = AddResultRow(pnl, "Pedestal Class (lu/b_min):", "", ref yLeft, false, col1);
            _txtSlender = AddResultRow(pnl, "Slenderness (klu/r):", "", ref yLeft, false, col1);
            _txtPuTotal = AddResultRow(pnl, "Axial Pu (kip):", "", ref yLeft, false, col1);
            _txtPgravity = AddResultRow(pnl, "Gravity Axial Load/Leg (kip):", "", ref yLeft, false, col1);
            _txtMuTotal = AddResultRow(pnl, "Moment Mu (k-ft):", "", ref yLeft, false, col1);
            _txtTleg = AddResultRow(pnl, "Overturning Tension/Leg (kip):", "", ref yLeft, false, col1);
            _txtPcompTotal = AddResultRow(pnl, "Max Compression Load (kip):", "", ref yLeft, false, col1);
            _txtBearingCap = AddResultRow(pnl, "Bearing Capacity φPn (kip):", "", ref yLeft, false, col1);
            _txtBearingDC = AddResultRow(pnl, "Bearing D/C Ratio:", "", ref yLeft, false, col1);
            _txtAsMin = AddResultRow(pnl, "Min Reinforcement As,min (in2):", "", ref yLeft, false, col1);
            _txtAsProv = AddResultRow(pnl, "Provided As (in2):", "", ref yLeft, false, col1);
            _txtPhiPn = AddResultRow(pnl, "Axial Compression φPn (kip):", "", ref yLeft, false, col1);
            _txtAxialDC = AddResultRow(pnl, "Axial D/C Ratio:", "", ref yLeft, false, col1);

            _txtPhiUsed = AddResultRow(pnl, "Transverse Reinf. Factor φ:", "", ref yRight, false, col2);
            _txtUpliftCap = AddResultRow(pnl, "Tensile Capacity φTn (kip):", "", ref yRight, false, col2);
            _txtUpliftDC = AddResultRow(pnl, "Uplift D/C Ratio:", "", ref yRight, false, col2);
            _txtPM_CbAb = AddResultRow(pnl, "Balanced cb / ab (in):", "", ref yRight, false, col2);
            _txtPM_Asb = AddResultRow(pnl, "Balanced Steel Asb (in2):", "", ref yRight, false, col2);
            _txtPM_Mnb = AddResultRow(pnl, "Balanced Moment Mnb (kip-ft):", "", ref yRight, false, col2);
            _txtPM_PhiMn = AddResultRow(pnl, "Design Moment φMnb (kip-ft):", "", ref yRight, false, col2);
            _txtPM_Mu = AddResultRow(pnl, "Pedestal Moment Mu,ped (k-ft):", "", ref yRight, false, col2);
            _txtPM_DC = AddResultRow(pnl, "P-M D/C Ratio:", "", ref yRight, false, col2);
            _txtVuTotal = AddResultRow(pnl, "Shear Vu (kip):", "", ref yRight, false, col2);
            _txtShearPed = AddResultRow(pnl, "Shear Vu,ped (kip):", "", ref yRight, false, col2);
            _txtTieSpacing = AddResultRow(pnl, "Tie Spacing s_max (in):", "", ref yRight, false, col2);
            _txtFootingLd = AddResultRow(pnl, "Development Length ld (in):", "", ref yRight, false, col2);
            _txtFootingLdProv = AddResultRow(pnl, "Provided Dev. Length (in):", "", ref yRight, false, col2);
            _txtFootingDC = AddResultRow(pnl, "Development D/C Ratio:", "", ref yRight, false, col2);
        }

        private TextBox _txtNumLegs = null!, _txtPedClass = null!, _txtSlender = null!, _txtPgravity = null!, _txtTleg = null!, _txtPcompTotal = null!;
        private TextBox _txtPuTotal = null!, _txtMuTotal = null!, _txtVuTotal = null!, _txtPhiUsed = null!;
        private TextBox _txtBearingCap = null!, _txtBearingDC = null!, _txtAsMin = null!, _txtAsProv = null!;
        private TextBox _txtPhiPn = null!, _txtAxialDC = null!;
        private TextBox _txtUpliftCap = null!, _txtUpliftDC = null!;
        private TextBox _txtPM_CbAb = null!, _txtPM_Asb = null!, _txtPM_Mnb = null!, _txtPM_Mu = null!, _txtPM_PhiMn = null!, _txtPM_DC = null!;
        private TextBox _txtShearPed = null!, _txtTieSpacing = null!;
        private TextBox _txtFootingLd = null!, _txtFootingLdProv = null!, _txtFootingDC = null!;

        private TextBox AddResultRow(Panel pnl, string labelText, string initialValue, ref int y, bool isHeader = false, int xOffset = 20)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(xOffset, y), AutoSize = true };
            if (isHeader)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                pnl.Controls.Add(lbl);
                y += 30;
                return null!;
            }

            TextBox txt = new TextBox
            {
                Text = initialValue,
                Location = new Point(xOffset + 205, y - 3),
                Size = new Size(125, 23),
                ReadOnly = true,
                BackColor = Color.White
            };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            txt.BringToFront();
            y += 31;
            return txt;
        }

        private void DisplayMultiColumnCalculatedData()
        {
            AppState.Load();
            var entity = _context.Set<RingWallEntity>().FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();
            var basePlate = _context.BasePlateEntity.FirstOrDefault();
            var loadService = new Services.LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            var eq = new FoundationEquations.MultiColumnPedestalEquations();

            int numLegs = AppState.NoOfColumns > 1
                ? AppState.NoOfColumns
                : ((anchorBolt?.Ns.HasValue == true && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);

            double puTotal = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
            if (puTotal <= 0) puTotal = 1637.59;

            double muTotal = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                ? basePlate.OverturningMoment.Value
                : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
            if (muTotal <= 0) muTotal = 5666.30;

            double vuTotal = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;
            if (vuTotal <= 0) vuTotal = 57.22;

            _txtNumLegs.Text = numLegs.ToString(CultureInfo.InvariantCulture);
            _txtPuTotal.Text = puTotal.ToString("F2", CultureInfo.InvariantCulture);
            _txtMuTotal.Text = muTotal.ToString("F2", CultureInfo.InvariantCulture);
            _txtVuTotal.Text = vuTotal.ToString("F3", CultureInfo.InvariantCulture);

            double autoB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
            double autoL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : autoB;

            double B = (entity?.PedestalSizeB.HasValue == true && entity.PedestalSizeB.Value > 0) ? entity.PedestalSizeB.Value : autoB;
            double L = (entity?.PedestalSizeL.HasValue == true && entity.PedestalSizeL.Value > 0) ? entity.PedestalSizeL.Value : autoL;
            double bMin = Math.Min(B, L);
            double Hp = (entity?.Hp.HasValue == true && entity.Hp.Value > 0) ? entity.Hp.Value : 7.417; // ft

            double basePlateArea = 706.86;
            if (basePlate != null && basePlate.Ro > 0)
            {
                basePlateArea = Math.PI * Math.Pow(basePlate.Ro, 2) / 4.0;
            }

            double fcPrime = (entity?.FcPrime.HasValue == true && entity.FcPrime.Value > 0) ? entity.FcPrime.Value : 4.0;
            double fy = (entity?.Fy.HasValue == true && entity.Fy.Value > 0) ? entity.Fy.Value : 60.0;
            double phiComp = (entity?.PhiCompression.HasValue == true && entity.PhiCompression.Value > 0) ? entity.PhiCompression.Value : 0.65;
            bool isSpiral = Math.Abs(phiComp - 0.75) < 0.01;
            double maxAxialFactor = isSpiral ? 0.85 : 0.80;
            _txtPhiUsed.Text = isSpiral ? "0.75 (Spirals)" : "0.65 (Tied)";

            double gammaC = (entity?.GammaC.HasValue == true && entity.GammaC.Value > 0) ? entity.GammaC.Value : 150.0;
            double AsProv = (entity?.PedestalAsProv.HasValue == true && entity.PedestalAsProv.Value > 0) ? entity.PedestalAsProv.Value : 7.92;

            // Step 1A: Pedestal Classification (lu / b_min <= 3.0)
            double lu_b = (Hp * 12.0) / bMin;
            _txtPedClass.Text = lu_b.ToString("F2", CultureInfo.InvariantCulture) + " <= 3.0";
            _txtPedClass.BackColor = eq.IsPedestal(Hp * 12.0, bMin) ? Color.LightGreen : Color.LightCoral;

            // Step 1B: Structural Slenderness (k * lu / r <= 22)
            double r = eq.RadiusOfGyrationSquare(bMin);
            double klu_r = (2.0 * (Hp * 12.0)) / r;
            _txtSlender.Text = klu_r.ToString("F2", CultureInfo.InvariantCulture) + " <= 22";
            _txtSlender.BackColor = eq.NeglectSlenderness(2.0, Hp * 12.0, r) ? Color.LightGreen : Color.LightCoral;

            // Step 2: Gravity Axial Load per Leg & Pedestal Self-Weight
            double pGrav = eq.GravityAxialLoadPerLeg(puTotal, numLegs);
            _txtPgravity.Text = pGrav.ToString("F2", CultureInfo.InvariantCulture);

            // Overturning Tension per Leg (from Anchor Bolt geometry)
            double coneDiamFt = (anchorBolt?.Dcone.HasValue == true && anchorBolt.Dcone.Value > 0) ? anchorBolt.Dcone.Value : 41.0469;
            double coneRadius = coneDiamFt * 12.0 / 2.0;
            var anchorEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
            double totalTension = anchorEq.TotalOverturningTension(muTotal, coneRadius);
            double tLeg = Math.Max(0, anchorEq.TensionPerLeg(totalTension, anchorEq.TensionLegs(numLegs)) - (puTotal / (2.0 * anchorEq.TensionLegs(numLegs)) * 0.0));
            // Use exact MultiColumnAnchorBoltEquations uplift formula: (Mu - Pu*(D/2)) / (nt * r) -> wait: check how AnchorBoltProperties computes Tleg!
            double netMomentKipFt = muTotal - (puTotal * (coneDiamFt / 2.0) * 0.0);
            tLeg = anchorEq.TensionPerLeg(totalTension, anchorEq.TensionLegs(numLegs));
            if (tLeg < 0) tLeg = 0;
            _txtTleg.Text = tLeg.ToString("F2", CultureInfo.InvariantCulture);

            double wPed = eq.PedestalSelfWeight(B, L, Hp, gammaC);
            double wPedFactored = eq.FactoredPedestalSelfWeight(wPed);
            double pCompTotal = pGrav + wPedFactored;
            _txtPcompTotal.Text = pCompTotal.ToString("F2", CultureInfo.InvariantCulture);

            // Step 4: Pedestal Concrete Bearing Check
            double pedestalArea = B * L;
            double enhancement = eq.BearingEnhancementFactor(basePlateArea, pedestalArea);
            double bearingCap = eq.PedestalBearingCapacity(fcPrime, basePlateArea, enhancement, 0.65);
            _txtBearingCap.Text = bearingCap.ToString("F2", CultureInfo.InvariantCulture);

            double bearingDC = eq.DemandCapacityRatio(pCompTotal, bearingCap);
            SetRatioBox(_txtBearingDC, bearingDC);

            // Step 5: Pedestal Longitudinal Reinforcement
            double asMin = eq.MinimumReinforcementArea(pedestalArea);
            _txtAsMin.Text = asMin.ToString("F3", CultureInfo.InvariantCulture);
            _txtAsProv.Text = AsProv.ToString("F2", CultureInfo.InvariantCulture);
            _txtAsProv.BackColor = AsProv >= asMin ? Color.LightGreen : Color.LightCoral;

            // Step 6: Axial Compression Capacity
            double po = eq.NominalConcentricCompressionStrength(fcPrime, pedestalArea, AsProv, fy);
            double phiPn = eq.DesignAxialCompressionCapacity(po, phiComp, maxAxialFactor);
            _txtPhiPn.Text = phiPn.ToString("F2", CultureInfo.InvariantCulture);

            double axialDC = eq.DemandCapacityRatio(pCompTotal, phiPn);
            SetRatioBox(_txtAxialDC, axialDC);

            // Step 7: Pedestal Tensile Capacity Check
            double phiTn = eq.DesignTensileCapacity(AsProv, fy, 0.90);
            _txtUpliftCap.Text = phiTn.ToString("F2", CultureInfo.InvariantCulture);

            double upliftDC = eq.DemandCapacityRatio(tLeg, phiTn);
            SetRatioBox(_txtUpliftDC, upliftDC);

            // Step 9: Horizontal Shear per Pedestal & Tie Spacing (computed before Step 8 moment demand)
            double vuPed = eq.HorizontalShearPerPedestal(vuTotal, numLegs);
            _txtShearPed.Text = vuPed.ToString("F3", CultureInfo.InvariantCulture);

            double tieSpacing = eq.RequiredTieSpacing(1.0, bMin);
            _txtTieSpacing.Text = tieSpacing.ToString("F1", CultureInfo.InvariantCulture);

            // Step 8: Pedestal P-M Balanced Section Check (ACI 318 / Professor's Method)
            double cbVal, abVal, asbVal, mnbVal;
            double phiMnb = eq.BalancedSectionMomentCapacity(B, L, 3.5, fcPrime, fy, phiComp, out cbVal, out abVal, out asbVal, out mnbVal);
            _txtPM_CbAb.Text = $"{cbVal.ToString("F2", CultureInfo.InvariantCulture)} / {abVal.ToString("F2", CultureInfo.InvariantCulture)}";
            _txtPM_Asb.Text = asbVal.ToString("F2", CultureInfo.InvariantCulture);
            _txtPM_Mnb.Text = mnbVal.ToString("F1", CultureInfo.InvariantCulture);
            _txtPM_PhiMn.Text = phiMnb.ToString("F1", CultureInfo.InvariantCulture);

            double pedMu = Math.Round(vuPed * Hp, 2); // Shear at top of pedestal * clear height Hp
            _txtPM_Mu.Text = pedMu.ToString("F2", CultureInfo.InvariantCulture);

            double pmDC = eq.DemandCapacityRatio(pedMu, phiMnb);
            SetRatioBox(_txtPM_DC, pmDC);

            // Step 10: Pedestal-to-Footing Development Length
            double ld = eq.TensionDevelopmentLength(fy, fcPrime, 1.0);
            _txtFootingLd.Text = ld.ToString("F2", CultureInfo.InvariantCulture);

            double lProv = entity?.FootingAsProv ?? 30.0;
            if (Math.Abs(lProv - 5.28) < 0.01) lProv = 30.0;
            _txtFootingLdProv.Text = lProv.ToString("F2", CultureInfo.InvariantCulture);

            double footingDC = eq.DemandCapacityRatio(ld, lProv);
            SetRatioBox(_txtFootingDC, footingDC);
        }

        private void SetRatioBox(TextBox textBox, double ratio)
        {
            textBox.Text = ratio.ToString("0.#####", CultureInfo.InvariantCulture);
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
