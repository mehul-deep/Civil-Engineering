using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Services;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public class FootingProperties : Form
    {
        private readonly WaterTankDbContext _context;
        private readonly FootingEquations _eq;

        private Panel pnlLeft = null!;
        private Panel pnlMid = null!;
        private Panel pnlRight = null!;

        public FootingProperties()
        {
            this.Text = "Multi-Leg Footing Design Results";
            this.Size = new Size(1260, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _context = WaterTankDbContext.GetInstance();
            _eq = new FootingEquations();

            SetupUI();
            LoadAndCalculate();
        }

        private void SetupUI()
        {
            pnlLeft = new Panel { Location = new Point(10, 10), Size = new Size(400, 500) };
            pnlMid = new Panel { Location = new Point(420, 10), Size = new Size(400, 500) };
            pnlRight = new Panel { Location = new Point(830, 10), Size = new Size(405, 500) };
            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlMid);
            this.Controls.Add(pnlRight);
        }

        private TextBox AddResultRow(Panel pnl, string labelText, string value, ref int y, bool isRatio = false, int xOffset = 10)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(xOffset, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lbl);

            TextBox txt = new TextBox
            {
                Text = value,
                Location = new Point(xOffset + 215, y),
                Width = 120,
                ReadOnly = true,
                BackColor = Color.White
            };
            pnl.Controls.Add(txt);
            txt.BringToFront();

            y += 32;
            return txt;
        }

        private void SetRatioBox(TextBox txt, double ratio)
        {
            txt.Text = ratio.ToString("F4", CultureInfo.InvariantCulture);
            txt.BackColor = ratio <= 1.0 ? Color.LightGreen : Color.LightCoral;
        }

        private void LoadAndCalculate()
        {
            AppState.Load();
            var entity = _context.FootingEntity?.FirstOrDefault();
            var ringWall = _context.RingWallEntity?.FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();
            var loadService = new LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

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

            // Pedestal geometry & material
            double B_ped = (ringWall?.PedestalSizeB.HasValue == true && ringWall.PedestalSizeB.Value > 0)
                ? ringWall.PedestalSizeB.Value
                : ((anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0);
            double L_ped = (ringWall?.PedestalSizeL.HasValue == true && ringWall.PedestalSizeL.Value > 0)
                ? ringWall.PedestalSizeL.Value
                : ((anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : B_ped);

            double Hp = (entity?.PedestalHeightHp.HasValue == true && entity.PedestalHeightHp.Value > 0)
                ? entity.PedestalHeightHp.Value
                : ((ringWall?.Hp.HasValue == true && ringWall.Hp.Value > 0) ? ringWall.Hp.Value : 5.5);

            double gammaC = (ringWall?.GammaC.HasValue == true && ringWall.GammaC.Value > 0) ? ringWall.GammaC.Value : 150.0;
            double fcPrime = (entity?.FcPrime.HasValue == true && entity.FcPrime.Value > 0)
                ? entity.FcPrime.Value
                : ((ringWall?.FcPrime.HasValue == true && ringWall.FcPrime.Value > 0) ? ringWall.FcPrime.Value : 4.0);
            double fy = (entity?.Fy.HasValue == true && entity.Fy.Value > 0)
                ? entity.Fy.Value
                : ((ringWall?.Fy.HasValue == true && ringWall.Fy.Value > 0) ? ringWall.Fy.Value : 60.0);
            double alphaS = (entity?.AlphaS.HasValue == true && entity.AlphaS.Value > 0) ? entity.AlphaS.Value : 40.0;

            // Footing inputs
            double B_footing = (entity?.FootingSizeB.HasValue == true && entity.FootingSizeB.Value > 0) ? entity.FootingSizeB.Value : 13.5;
            double L_footing = (entity?.FootingSizeL.HasValue == true && entity.FootingSizeL.Value > 0) ? entity.FootingSizeL.Value : 13.5;
            double h_footing = (entity?.FootingThickness.HasValue == true && entity.FootingThickness.Value > 0) ? entity.FootingThickness.Value : 30.0;
            double cover = (entity?.ConcreteCover.HasValue == true && entity.ConcreteCover.Value > 0) ? entity.ConcreteCover.Value : 3.0;
            double qAllow = (entity?.Qallow.HasValue == true && entity.Qallow.Value > 0) ? entity.Qallow.Value : 3.0;
            double muFriction = (entity?.FrictionCoeff.HasValue == true && entity.FrictionCoeff.Value > 0) ? entity.FrictionCoeff.Value : 0.50;
            double bottomAs = (entity?.BottomRebarArea.HasValue == true && entity.BottomRebarArea.Value > 0) ? entity.BottomRebarArea.Value : 0.677;
            double db = (entity?.RebarDiameter.HasValue == true && entity.RebarDiameter.Value > 0) ? entity.RebarDiameter.Value : 1.0;

            double autoDowelAs = (ringWall?.PedestalAsProv.HasValue == true && ringWall.PedestalAsProv.Value > 0) ? ringWall.PedestalAsProv.Value : 7.92;
            double dowelAs = (entity?.TopRebarArea.HasValue == true && entity.TopRebarArea.Value > 0) ? entity.TopRebarArea.Value : autoDowelAs;
            double db_dowel = (entity?.DowelDiameter.HasValue == true && entity.DowelDiameter.Value > 0) ? entity.DowelDiameter.Value : 0.75;

            // Step 1: Footing Load (Pc = P_leg + 1.2 * W_p)
            double pLeg = _eq.FootingGravityLoad(puTotal, numLegs);
            double pedWeight = _eq.PedestalSelfWeight(B_ped, L_ped, Hp, gammaC);
            double pedWeightFactored = 1.2 * pedWeight;
            double pFootingDesign = _eq.FootingDesignLoad(pLeg, pedWeightFactored);

            // Step 2 & 3: Preliminary Footing Size & Soil Bearing Check
            double areaReq = _eq.RequiredFootingArea(pFootingDesign, qAllow);
            double areaProv = B_footing * L_footing;
            double qApplied = _eq.AppliedSoilPressure(pFootingDesign, areaProv);

            // Sliding Check
            double vLeg = vuTotal / numLegs;
            double fsSliding = _eq.SlidingFactorOfSafety(muFriction, pFootingDesign, vLeg);

            // Step 4 & 5: Effective Depth & Two-Way Punching Shear
            double d = _eq.EffectiveDepth(h_footing, cover, db);
            double b_o = _eq.CriticalPunchingPerimeter(B_ped, L_ped, d);
            double a_o = _eq.AreaInsideCriticalPerimeter(B_ped, L_ped, d);
            double pSoilInside = qApplied * a_o;
            double v_u_punch = _eq.FactoredPunchingShear(pFootingDesign, qApplied, a_o);
            double phi_v_c_force = _eq.DesignPunchingShearCapacity(fcPrime * 1000.0, B_ped, L_ped, b_o, d, alphaS);
            double punchingUtilization = _eq.DemandCapacityRatio(v_u_punch, phi_v_c_force);

            // Step 7: One-Way Shear (per 1-ft-wide strip)
            double m = _eq.GoverningCantileverProjection(B_footing, L_footing, B_ped, L_ped);
            double v_u_oneway = _eq.OneWayShearForcePerFt(qApplied, m, d);
            double phi_v_c_oneway = _eq.DesignOneWayShearCapacityPerFt(fcPrime * 1000.0, d);
            double oneWayUtilization = _eq.DemandCapacityRatio(v_u_oneway, phi_v_c_oneway);

            // Step 8: Flexure and Reinforcement (per 1-ft-wide strip)
            double mu_flexure_ft = _eq.FlexureMomentPerFt(qApplied, m); // kip-ft/ft
            double mu_flexure_in = mu_flexure_ft * 12.0; // kip-in/ft
            double as_req = _eq.RequiredFlexuralSteelPerFt(mu_flexure_in, fcPrime, fy, d);
            double as_min = _eq.MinimumReinforcementPerFt(h_footing);
            double as_design = Math.Max(as_req, as_min);
            double flexureAsDC = _eq.DemandCapacityRatio(as_design, bottomAs);

            double phi_mn_in = _eq.DesignFlexuralCapacityKipInPerFt(bottomAs, fcPrime, fy, d);
            double flexureMomentDC = _eq.DemandCapacityRatio(mu_flexure_in, phi_mn_in);

            // Step 9: Pedestal/Footing Bearing
            double n1_bearing = _eq.PedestalBearingCapacity(fcPrime, B_ped, L_ped);
            double pedBearingDC = _eq.DemandCapacityRatio(pFootingDesign, n1_bearing);

            // Step 10: Dowel Reinforcement
            double as_dowel_min = _eq.DowelMinimumArea(B_ped, L_ped);
            double dowelDC = _eq.DemandCapacityRatio(as_dowel_min, dowelAs);

            // Step 11: Compression Development Length
            double ldc = _eq.DevelopmentLength(fy * 1000.0, fcPrime * 1000.0, db_dowel);

            // Display UI
            int yLeft = 10;
            int yMid = 10;
            int yRight = 10;

            // === LEFT COLUMN (Steps 1, 2, 3 & Sliding) ===
            AddResultRow(pnlLeft, "Number of Legs:", numLegs.ToString(CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Axial Pu (kip):", puTotal.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Gravity Load P_leg (kip):", pLeg.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Factored Pedestal Wt (kip):", pedWeightFactored.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Design Footing Load Pc (kip):", pFootingDesign.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);

            AddResultRow(pnlLeft, "Required Area A_req (ft2):", areaReq.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Provided Area A_f (ft2):", areaProv.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);

            AddResultRow(pnlLeft, "Soil Pressure q_u (ksf):", qApplied.ToString("F3", CultureInfo.InvariantCulture), ref yLeft);
            AddResultRow(pnlLeft, "Allowable q_allow (ksf):", qAllow.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            TextBox txtSoilDC = AddResultRow(pnlLeft, "Soil Bearing D/C Ratio:", "", ref yLeft, true);
            SetRatioBox(txtSoilDC, _eq.DemandCapacityRatio(qApplied, qAllow));

            AddResultRow(pnlLeft, "Shear/Leg V_leg (kip):", vLeg.ToString("F2", CultureInfo.InvariantCulture), ref yLeft);
            TextBox txtSlide = AddResultRow(pnlLeft, "Sliding Factor of Safety:", fsSliding.ToString("F2", CultureInfo.InvariantCulture) + " >= 1.5", ref yLeft);
            txtSlide.BackColor = fsSliding >= 1.5 ? Color.LightGreen : Color.LightCoral;

            // === MIDDLE COLUMN (Steps 4, 5 & 7) ===
            AddResultRow(pnlMid, "Effective Depth d (in):", d.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "Crit. Perimeter bo (in):", b_o.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "Area Inside Ao (ft2):", a_o.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "Soil Reaction P_soil (kip):", pSoilInside.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "Punching Demand Vu (kip):", v_u_punch.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "Punching Cap. φVc (kip):", phi_v_c_force.ToString("F1", CultureInfo.InvariantCulture), ref yMid);
            TextBox txtPunchDC = AddResultRow(pnlMid, "Punching Shear D/C:", "", ref yMid, true);
            SetRatioBox(txtPunchDC, punchingUtilization);

            AddResultRow(pnlMid, "Cantilever Proj. l (ft):", m.ToString("F3", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "1-Way Shear Vu (kip/ft):", v_u_oneway.ToString("F3", CultureInfo.InvariantCulture), ref yMid);
            AddResultRow(pnlMid, "1-Way Cap. φVc (kip/ft):", phi_v_c_oneway.ToString("F2", CultureInfo.InvariantCulture), ref yMid);
            TextBox txtOneWayDC = AddResultRow(pnlMid, "One-Way Shear D/C:", "", ref yMid, true);
            SetRatioBox(txtOneWayDC, oneWayUtilization);

            // === RIGHT COLUMN (Steps 8, 9, 10 & 11) ===
            AddResultRow(pnlRight, "Flexure Mu (kip-ft/ft):", mu_flexure_ft.ToString("F2", CultureInfo.InvariantCulture), ref yRight);
            AddResultRow(pnlRight, "Flexure Mu (kip-in/ft):", mu_flexure_in.ToString("F2", CultureInfo.InvariantCulture), ref yRight);
            AddResultRow(pnlRight, "As,req / As,min (in2/ft):", $"{as_req.ToString("F3", CultureInfo.InvariantCulture)} / {as_min.ToString("F3", CultureInfo.InvariantCulture)}", ref yRight);
            AddResultRow(pnlRight, "Provided As (in2/ft):", bottomAs.ToString("F3", CultureInfo.InvariantCulture), ref yRight);
            TextBox txtFlexAsDC = AddResultRow(pnlRight, "Flexure Steel D/C:", "", ref yRight, true);
            SetRatioBox(txtFlexAsDC, flexureAsDC);

            AddResultRow(pnlRight, "Flexure Cap. φMn (kip-in/ft):", phi_mn_in.ToString("F1", CultureInfo.InvariantCulture), ref yRight);
            TextBox txtFlexMomDC = AddResultRow(pnlRight, "Flexure Moment D/C:", "", ref yRight, true);
            SetRatioBox(txtFlexMomDC, flexureMomentDC);

            AddResultRow(pnlRight, "Pedestal Bearing N1 (kip):", n1_bearing.ToString("F1", CultureInfo.InvariantCulture), ref yRight);
            TextBox txtPedBearDC = AddResultRow(pnlRight, "Pedestal Bearing D/C:", "", ref yRight, true);
            SetRatioBox(txtPedBearDC, pedBearingDC);

            AddResultRow(pnlRight, "Dowel As,min / Prov (in2):", $"{as_dowel_min.ToString("F3", CultureInfo.InvariantCulture)} / {dowelAs.ToString("F2", CultureInfo.InvariantCulture)}", ref yRight);
            TextBox txtDowelDC = AddResultRow(pnlRight, "Dowel Steel D/C:", "", ref yRight, true);
            SetRatioBox(txtDowelDC, dowelDC);

            AddResultRow(pnlRight, "Comp. Dev. Length ldc (in):", ldc.ToString("F2", CultureInfo.InvariantCulture), ref yRight);
        }
    }
}
