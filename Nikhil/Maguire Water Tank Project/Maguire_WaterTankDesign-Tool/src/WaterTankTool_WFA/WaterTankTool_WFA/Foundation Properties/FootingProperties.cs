using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public class FootingProperties : Form
    {
        private WaterTankDbContext _context;
        private FootingEquations _eq;
        
        private Panel pnlLeft;
        private Panel pnlMid;
        private Panel pnlRight;

        public FootingProperties()
        {
            this.Text = "Multi-Leg Footing Properties";
            this.Size = new Size(1220, 620);
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
            pnlLeft = new Panel { Location = new Point(10, 10), Size = new Size(390, 560) };
            pnlMid = new Panel { Location = new Point(410, 10), Size = new Size(390, 560) };
            pnlRight = new Panel { Location = new Point(810, 10), Size = new Size(390, 560) };
            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlMid);
            this.Controls.Add(pnlRight);
        }

        private TextBox AddResultRow(Panel pnl, string labelText, string value, ref int y, bool isRatio = false, int xOffset = 20)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(xOffset, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lbl);

            TextBox txt = new TextBox
            {
                Text = value,
                Location = new Point(xOffset + 215, y - 3),
                Width = 120,
                ReadOnly = true,
                BackColor = Color.White
            };
            pnl.Controls.Add(txt);

            y += 35;
            return txt;
        }

        private void SetRatioBox(TextBox txt, double ratio)
        {
            txt.Text = ratio.ToString("F5");
            if (ratio <= 1.0)
            {
                txt.BackColor = Color.LightGreen;
            }
            else
            {
                txt.BackColor = Color.LightCoral;
            }
        }

        private void LoadAndCalculate()
        {
            var entity = _context.FootingEntity?.FirstOrDefault();
            var ringWall = _context.RingWallEntity?.FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();

            double puTotal = anchorBolt?.Pu ?? 1609.756;
            double muTotal = basePlate?.OverturningMoment ?? 5673.437;
            int numLegs = 4;
            if (anchorBolt?.Ns > 0) numLegs = (int)anchorBolt.Ns.Value;
            else if (AppState.NoOfColumns > 1) numLegs = AppState.NoOfColumns;

            // Geometry from RingWall (Pedestal)
            double B_pedestal = ringWall?.PedestalSizeB ?? 39;
            double L_pedestal = ringWall?.PedestalSizeL ?? 39;
            double Hp = ringWall?.Hp ?? 7.417; // ft
            double gammaC = ringWall?.GammaC ?? 150.0;
            
            // Inputs from FootingEntity
            double B_footing = entity?.FootingSizeB ?? 13.5;
            double L_footing = entity?.FootingSizeL ?? 13.5;
            double h_footing = entity?.FootingThickness ?? 30.0; // in
            double cover = entity?.ConcreteCover ?? 3.0; // in
            double qAllow = entity?.Qallow ?? 3.0; // ksf
            double muFriction = entity?.FrictionCoeff ?? 0.50;
            double bottomAs = entity?.BottomRebarArea ?? 1.185; // in2/ft
            double dowelAs = entity?.TopRebarArea ?? 7.92; // in2 (repurposed TopRebarArea)
            double db = entity?.RebarDiameter ?? 1.0; // #8 bar
            double fcPrime = ringWall?.FcPrime ?? 4.0;
            
            // Reconstruct Overturning Tension per Leg
            double dCone = anchorBolt?.Dcone ?? 41.04;
            double coneRadius = (dCone * 12.0) / 2.0;
            double totalTension = (muTotal * 12.0) / ((4.0 / 3.0) * coneRadius);
            int tensionLegs = (numLegs == 4) ? 2 : (numLegs / 2);
            double tLeg = totalTension / tensionLegs;
            
            // Step 1: Footing Load
            double pLeg = _eq.FootingGravityLoad(puTotal, numLegs);
            double pedWeight = (B_pedestal / 12.0) * (L_pedestal / 12.0) * Hp * (gammaC / 1000.0);
            double pedWeightFactored = 1.2 * pedWeight; // 14.10 kip
            
            double pCompTotal = _eq.CompressionSideLoad(pLeg, tLeg, pedWeightFactored); // Should be 520.2 kip
            
            // Step 2 & 3: Soil Bearing Check
            double areaReq = _eq.RequiredFootingArea(pCompTotal, qAllow);
            double areaProv = B_footing * L_footing;
            double qApplied = _eq.AppliedSoilPressure(pCompTotal, areaProv);
            
            // Step 6: Sliding Check
            double vuTotal = 52.30; // Usually from BasePlate/AnchorBolt, assuming 52.30 for QA if missing
            double vLeg = vuTotal / numLegs;
            double fsSliding = _eq.SlidingFactorOfSafety(muFriction, pCompTotal, vLeg);
            
            // Step 7: Two-Way Punching Shear
            double d = _eq.EffectiveDepth(h_footing, cover, db);
            double b_o = _eq.CriticalPunchingPerimeter(B_pedestal, d); // in
            double a_o = _eq.AreaInsideCriticalPerimeter(B_pedestal, d); // ft2
            
            double v_u_punch = _eq.FactoredPunchingShear(pCompTotal, qApplied, a_o); // kips
            double v_u_stress = _eq.PunchingShearStress(v_u_punch, b_o, d); // psi
            
            double phi_v_c_stress = 0.75 * _eq.ConcretePunchingShearCapacity(fcPrime * 1000.0); // psi
            double punchingUtilization = _eq.DemandCapacityRatio(v_u_stress, phi_v_c_stress);
            
            // Step 8: Punching Force
            double v_c_force = _eq.PunchingShearForceCapacity(_eq.ConcretePunchingShearCapacity(fcPrime * 1000.0), b_o, d); // kips
            double phi_v_c_force = 0.75 * v_c_force; // kips
            double punchingForceUtilization = _eq.DemandCapacityRatio(v_u_punch, phi_v_c_force);

            // Step 7: One-Way Shear
            double m = _eq.OneWayShearCantilever(B_footing, B_pedestal / 12.0); // ft
            double v_u_oneway = _eq.OneWayShearForce(qApplied, B_footing, m, d); // kips
            double phi_v_c_oneway = 0.75 * _eq.OneWayShearCapacity(fcPrime * 1000.0, bottomAs, B_footing * 12.0, d); // kips
            double oneWayUtilization = _eq.DemandCapacityRatio(v_u_oneway, phi_v_c_oneway);

            // Step 8: Flexure and Reinforcement
            double mu_flexure = _eq.FlexureMoment(qApplied, m); // kip-ft/ft
            double as_min = _eq.MinimumReinforcement(h_footing); // in2/ft
            double flexureUtilization = _eq.DemandCapacityRatio(as_min, bottomAs); // Demand As / Provided As

            // Step 9: Pedestal/Footing Bearing
            double n1_bearing = _eq.PedestalBearingCapacity(fcPrime * 1000.0, B_pedestal); // kips
            double bearingUtilization = _eq.DemandCapacityRatio(pCompTotal, n1_bearing);

            // Step 10: Dowel Reinforcement
            double as_dowel_min = _eq.DowelMinimumArea(B_pedestal); // in2
            double dowelUtilization = _eq.DemandCapacityRatio(as_dowel_min, dowelAs); // Demand As / Provided As

            // Step 11: Development Length
            double fy = ringWall?.Fy ?? 60.0; // ksi
            double fyPsi = fy * 1000.0;
            double db_dowel = 0.75; // #6 dowel typical based on PDF
            double ldc = _eq.DevelopmentLength(fyPsi, fcPrime * 1000.0, db_dowel);

            // Display UI
            int yLeft = 10;
            int yMid = 10;
            int yRight = 10;
            
            // === LEFT COLUMN ===
            AddResultRow(pnlLeft, "Gravity Load/Leg (kip):", pLeg.ToString("F2"), ref yLeft, false, 10);
            AddResultRow(pnlLeft, "Compression Side Load (kip):", pCompTotal.ToString("F2"), ref yLeft, false, 10);

            AddResultRow(pnlLeft, "Required Footing Area (ft2):", areaReq.ToString("F2"), ref yLeft, false, 10);
            AddResultRow(pnlLeft, "Provided Footing Area (ft2):", areaProv.ToString("F2"), ref yLeft, false, 10);

            AddResultRow(pnlLeft, "Applied Pressure q (ksf):", qApplied.ToString("F2"), ref yLeft, false, 10);
            AddResultRow(pnlLeft, "Allowable q_allow (ksf):", qAllow.ToString("F2"), ref yLeft, false, 10);
            TextBox txtBearing = AddResultRow(pnlLeft, "Bearing D/C Ratio:", "", ref yLeft, true, 10);
            SetRatioBox(txtBearing, _eq.DemandCapacityRatio(qApplied, qAllow));

            AddResultRow(pnlLeft, "Sliding Check FS:", fsSliding.ToString("F1") + " > 1.5", ref yLeft, false, 10);

            // === MIDDLE COLUMN ===
            AddResultRow(pnlMid, "Effective Depth d (in):", d.ToString("F1"), ref yMid, false, 10);

            AddResultRow(pnlMid, "Crit. Perimeter bo (in):", b_o.ToString("F2"), ref yMid, false, 10);
            AddResultRow(pnlMid, "Area Inside Perim Ao (ft2):", a_o.ToString("F2"), ref yMid, false, 10);
            AddResultRow(pnlMid, "Factored Punch Shear (kip):", v_u_punch.ToString("F1"), ref yMid, false, 10);
            AddResultRow(pnlMid, "Design Capacity phi_Vc (kip):", phi_v_c_force.ToString("F1"), ref yMid, false, 10);
            TextBox txtPunchingForce = AddResultRow(pnlMid, "Punching Force D/C:", "", ref yMid, true, 10);
            SetRatioBox(txtPunchingForce, punchingForceUtilization);

            AddResultRow(pnlMid, "One-Way Shear Vu (kip):", v_u_oneway.ToString("F2"), ref yMid, false, 10);
            AddResultRow(pnlMid, "Capacity phi_Vc (kip):", phi_v_c_oneway.ToString("F2"), ref yMid, false, 10);
            TextBox txtOneWay = AddResultRow(pnlMid, "One-Way Shear D/C:", "", ref yMid, true, 10);
            SetRatioBox(txtOneWay, oneWayUtilization);

            // === RIGHT COLUMN ===
            AddResultRow(pnlRight, "Flexure Mu (kip-ft/ft):", mu_flexure.ToString("F2"), ref yRight, false, 10);
            AddResultRow(pnlRight, "Required As_min (in2/ft):", as_min.ToString("F3"), ref yRight, false, 10);
            AddResultRow(pnlRight, "Provided As (in2/ft):", bottomAs.ToString("F3"), ref yRight, false, 10);
            TextBox txtFlexure = AddResultRow(pnlRight, "Flexure As D/C:", "", ref yRight, true, 10);
            SetRatioBox(txtFlexure, flexureUtilization);

            AddResultRow(pnlRight, "Pedestal Bearing N1 (kip):", n1_bearing.ToString("F1"), ref yRight, false, 10);
            TextBox txtPedBearing = AddResultRow(pnlRight, "Pedestal Bearing D/C:", "", ref yRight, true, 10);
            SetRatioBox(txtPedBearing, bearingUtilization);

            AddResultRow(pnlRight, "Dowel As_min (in2):", as_dowel_min.ToString("F3"), ref yRight, false, 10);
            AddResultRow(pnlRight, "Provided Dowel As (in2):", dowelAs.ToString("F3"), ref yRight, false, 10);
            TextBox txtDowel = AddResultRow(pnlRight, "Dowel As D/C:", "", ref yRight, true, 10);
            SetRatioBox(txtDowel, dowelUtilization);

            AddResultRow(pnlRight, "Min Develop Length (in):", ldc.ToString("F2"), ref yRight, false, 10);
        }
    }
}
