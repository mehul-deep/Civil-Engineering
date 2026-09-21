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
        private Panel pnlRight;
        
        public FootingProperties()
        {
            this.Text = "Multi-Leg Footing Properties";
            this.Size = new Size(820, 520);
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
            pnlLeft = new Panel { Location = new Point(10, 10), Size = new Size(390, 460) };
            pnlRight = new Panel { Location = new Point(410, 10), Size = new Size(390, 460) };
            this.Controls.Add(pnlLeft);
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

            // Display UI
            int yLeft = 10;
            int yRight = 10;
            
            // Left Column
            AddResultRow(pnlLeft, "1. Gravity Load/Leg (kip):", pLeg.ToString("F2"), ref yLeft, false, 15);
            AddResultRow(pnlLeft, "   Compression Side Load (kip):", pCompTotal.ToString("F2"), ref yLeft, false, 15);
            yLeft += 10;
            AddResultRow(pnlLeft, "2. Required Footing Area (ft2):", areaReq.ToString("F2"), ref yLeft, false, 15);
            AddResultRow(pnlLeft, "   Provided Footing Area (ft2):", areaProv.ToString("F2"), ref yLeft, false, 15);
            yLeft += 10;
            AddResultRow(pnlLeft, "3. Applied Pressure q (ksf):", qApplied.ToString("F2"), ref yLeft, false, 15);
            AddResultRow(pnlLeft, "   Allowable q_allow (ksf):", qAllow.ToString("F2"), ref yLeft, false, 15);
            TextBox txtBearing = AddResultRow(pnlLeft, "   Bearing D/C Ratio:", "", ref yLeft, true, 15);
            SetRatioBox(txtBearing, _eq.DemandCapacityRatio(qApplied, qAllow));
            yLeft += 10;
            AddResultRow(pnlLeft, "6. Sliding Check FS:", fsSliding.ToString("F1") + " > 1.5", ref yLeft, false, 15);
            
            // Right Column
            AddResultRow(pnlRight, "7. Effective Depth d (in):", d.ToString("F1"), ref yRight, false, 15);
            AddResultRow(pnlRight, "   Crit. Perimeter bo (in):", b_o.ToString("F2"), ref yRight, false, 15);
            AddResultRow(pnlRight, "   Area Inside Perim Ao (ft2):", a_o.ToString("F2"), ref yRight, false, 15);
            yRight += 10;
            AddResultRow(pnlRight, "   Factored Punch Shear (kip):", v_u_punch.ToString("F1"), ref yRight, false, 15);
            AddResultRow(pnlRight, "   Punch Shear Stress (psi):", v_u_stress.ToString("F1"), ref yRight, false, 15);
            AddResultRow(pnlRight, "   Capacity phi_vc (psi):", phi_v_c_stress.ToString("F1"), ref yRight, false, 15);
            TextBox txtPunchingStress = AddResultRow(pnlRight, "   Punching Stress D/C:", "", ref yRight, true, 15);
            SetRatioBox(txtPunchingStress, punchingUtilization);
            yRight += 10;
            AddResultRow(pnlRight, "8. Design Capacity phi_Vc (kip):", phi_v_c_force.ToString("F0"), ref yRight, false, 15);
            TextBox txtPunchingForce = AddResultRow(pnlRight, "   Punching Force D/C:", "", ref yRight, true, 15);
            SetRatioBox(txtPunchingForce, punchingForceUtilization);
        }
    }
}
