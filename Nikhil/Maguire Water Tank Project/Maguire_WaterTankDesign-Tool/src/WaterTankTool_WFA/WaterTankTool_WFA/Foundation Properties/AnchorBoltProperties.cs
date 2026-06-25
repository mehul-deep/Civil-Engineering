using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Foundation_Design;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public partial class AnchorBoltProperties : Form
    {
        private readonly WaterTankDbContext _context;

        public AnchorBoltProperties()
        {
            InitializeComponent();

            _context = WaterTankDbContext.GetInstance();

            this.Load += AnchorBoltProperties_Load;
        }

        private void AnchorBoltProperties_Load(object? sender, EventArgs e)
        {
            LoadAnchorBoltCalculatedValues();
        }

        private void LoadAnchorBoltCalculatedValues()
        {
            try
            {
                var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();

                if (anchorBolt == null)
                {
                    MessageBox.Show(
                        "No anchor bolt data found. Please enter anchor bolt parameters first.",
                        "No Data",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                CalculateAnchorBoltValues(anchorBolt);
                PopulateBoltDropdown(anchorBolt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load anchor bolt values.\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CalculateAnchorBoltValues(AnchorBoltEntity anchorBolt)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                CalculateMultiColumnAnchorBoltValues(anchorBolt);
                return;
            }

            var eq = new FoundationEquations.AnchorBoltEquations();

            double area = eq.CrossSectionalArea(anchorBolt.Db);
            double holeArea = eq.HoleArea(anchorBolt.Dh);
            double angularSpacing = eq.BoltAngularSpacing(anchorBolt.Nb);
            double arcSpacing = eq.ArcSpacing(anchorBolt.Rb, anchorBolt.Nb);
            double chordSpacing = eq.ChordSpacing(anchorBolt.Rb, anchorBolt.Nb);

            double? boltsPerSegment = null;
            bool boltsPerSegmentIsInteger = true;
            if (anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0)
            {
                boltsPerSegment = eq.BoltsPerSegment(anchorBolt.Nb, anchorBolt.Ns.Value);
                boltsPerSegmentIsInteger = eq.BoltsPerSegmentIsInteger(anchorBolt.Nb, anchorBolt.Ns.Value);
            }

            double? clearEdgeDistance = null;
            if (anchorBolt.E.HasValue)
            {
                clearEdgeDistance = eq.ClearEdgeDistance(anchorBolt.E.Value, anchorBolt.Dh);
                
                // Section 10: Clear Edge Distance Check
                // PDF says SAFE if > 1.5 in (absolute minimum)
                bool isSafe = clearEdgeDistance.Value >= 1.5;
                labelEdgeDistanceStatus.Text = isSafe ? "PASS" : "FAIL";
                labelEdgeDistanceStatus.ForeColor = isSafe ? Color.Green : Color.Red;
                textBox9.BackColor = isSafe ? Color.LightGreen : Color.LightCoral;
            }

            // --- Updated Calculation Logic ---
            double totalTensionTu = anchorBolt.Tu;
            if (anchorBolt.Mu.HasValue && anchorBolt.Mu.Value > 0)
            {
                // Calculate Tu from Mu if Mu is provided: Tu = Mu / (0.67 * D)
                totalTensionTu = eq.TotalTensionDemandFromMoment(anchorBolt.Mu.Value, 2.0 * anchorBolt.Rb);
            }

            double tensionPerBolt;
            // Use selected Distribution Method
            switch (anchorBolt.DistributionMethod)
            {
                case "Equal Distribution":
                    tensionPerBolt = eq.TensionDemandPerBolt_Equal(totalTensionTu, anchorBolt.Nb);
                    break;
                case "Effective Bolts":
                    tensionPerBolt = eq.TensionDemandPerBolt_Effective(totalTensionTu, anchorBolt.Nb);
                    break;
                case "Circular Group":
                default:
                    tensionPerBolt = eq.TensionDemandPerBolt_CircularGroup(totalTensionTu, anchorBolt.Nb);
                    break;
            }

            double shearPerBolt = eq.ShearDemandPerBolt(anchorBolt.Vu, anchorBolt.Nb);

            double tensileCapacity = 0;
            if (anchorBolt.Fu.HasValue)
            {
                tensileCapacity = eq.TensileDesignStrengthUltimate(
                    anchorBolt.Db,
                    anchorBolt.Fu.Value,
                    anchorBolt.Phi ?? 0.75);
            }
            else if (anchorBolt.Fy.HasValue)
            {
                tensileCapacity = eq.TensileDesignStrength(
                    anchorBolt.Db,
                    anchorBolt.Fy.Value,
                    anchorBolt.Phi ?? 0.75);
            }

            double shearCapacity = 0;
            if (anchorBolt.Fu.HasValue)
            {
                // φVn = φ * Ab * (0.6 * Fu)
                shearCapacity = eq.ShearDesignStrengthUltimate(
                    anchorBolt.Db,
                    anchorBolt.Fu.Value,
                    anchorBolt.Phi ?? 0.75);
            }
            else if (anchorBolt.Fy.HasValue)
            {
                shearCapacity = eq.ShearDesignStrength(
                    anchorBolt.Db,
                    anchorBolt.Fy.Value,
                    anchorBolt.Phi ?? 0.75);
            }
            // --- End Updated Logic ---

            double interaction = 0;
            bool interactionPass = false;

            if (tensileCapacity > 0 && shearCapacity > 0)
            {
                interaction = eq.InteractionCheck(
                    tensionPerBolt,
                    tensileCapacity,
                    shearPerBolt,
                    shearCapacity);

                interactionPass = eq.InteractionPass(interaction);
            }

            textBox1.Text = area.ToString("F4");
            textBox2.Text = holeArea.ToString("F4");
            textBox3.Text = angularSpacing.ToString("F4");
            textBox6.Text = (arcSpacing * 12.0).ToString("F2"); // Convert to inches
            textBox7.Text = (chordSpacing * 12.0).ToString("F2"); // Convert to inches
            textBox8.Text = boltsPerSegment?.ToString("F4") ?? "";
            textBox9.Text = clearEdgeDistance?.ToString("F4") ?? "";
            
            // New Input Reference Fields
            textBoxTbp.Text = anchorBolt.Tbp.ToString("F4");
            textBoxMu.Text = anchorBolt.Mu?.ToString("F4") ?? "0.0000";
            textBoxFcPrime.Text = anchorBolt.FcPrime?.ToString("F4") ?? "0.0000";
            textBoxHef.Text = anchorBolt.Hef?.ToString("F4") ?? "0.0000";
            textBoxDistMethod.Text = anchorBolt.DistributionMethod ?? "Circular Group";

            textBox18.Text = totalTensionTu.ToString("F4"); // New field: Total Uplift (Tu)
            textBox10.Text = tensionPerBolt.ToString("F4");
            textBox11.Text = shearPerBolt.ToString("F4");
            textBox12.Text = tensileCapacity > 0 ? tensileCapacity.ToString("F4") : "";
            textBox13.Text = shearCapacity > 0 ? shearCapacity.ToString("F4") : "";

            // --- Sections 15, 16, 17 ---
            if (anchorBolt.Hef.HasValue)
            {
                // Section 15: Minimum Embedment Check (hef >= 8 * db)
                bool embedmentPass = anchorBolt.Hef.Value >= (8.0 * anchorBolt.Db);
                textBox15.Text = embedmentPass ? "PASS" : "FAIL";
                textBox15.BackColor = embedmentPass ? Color.LightGreen : Color.LightCoral;

                if (anchorBolt.FcPrime.HasValue)
                {
                    // Section 16: Concrete Breakout Capacity (Factored: phi * Nb)
                    // kc = 24 for cast-in anchors as per PDF example
                    double breakoutCapacity = eq.ConcreteBreakoutStrength(24, anchorBolt.FcPrime.Value, anchorBolt.Hef.Value);
                    double breakoutCapacityKips = breakoutCapacity / 1000.0;
                    double designBreakoutCapacity = (anchorBolt.Phi ?? 0.75) * breakoutCapacityKips;
                    
                    textBox16.Text = designBreakoutCapacity.ToString("F2"); // Matches PDF Section 16 (1070 kips)

                    // Section 17: Concrete Breakout Utilization
                    double breakoutUtilization = eq.ConcreteBreakoutUtilization(tensionPerBolt, breakoutCapacityKips, anchorBolt.Phi ?? 0.75);
                    textBox17.Text = breakoutUtilization.ToString("F4");

                    bool breakoutPass = breakoutUtilization <= 1.0;
                    textBox17.BackColor = breakoutPass ? Color.LightGreen : Color.LightCoral;
                    labelBreakoutStatus.Text = breakoutPass ? "PASS" : "FAIL";
                    labelBreakoutStatus.ForeColor = breakoutPass ? Color.Green : Color.Red;
                }
            }
            // --- End Sections 15, 16, 17 ---

            if (tensileCapacity > 0 && shearCapacity > 0)
            {
                SetInteractionStatus(interaction, interactionPass, false);
            }
            else
            {
                SetInteractionStatus(null, null, true);
            }

            if (boltsPerSegment.HasValue && !boltsPerSegmentIsInteger)
            {
                textBox8.BackColor = Color.Khaki;
            }
            else
            {
                textBox8.BackColor = Color.White;
            }
        }

        private void PopulateBoltDropdown(AnchorBoltEntity anchorBolt)
        {
            comboBoxBoltSelect.Items.Clear();
            for (int i = 1; i <= anchorBolt.Nb; i++)
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
            var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();
            if (anchorBolt == null) return;

            int boltIndex = comboBoxBoltSelect.SelectedIndex + 1;
            var eq = new FoundationEquations.AnchorBoltEquations();

            double angle = eq.BoltAngle(anchorBolt.Ab, boltIndex, anchorBolt.Nb);
            double x = eq.BoltXCoordinate(anchorBolt.Rb, angle);
            double y = eq.BoltYCoordinate(anchorBolt.Rb, angle);

            textBoxAngleDetail.Text = angle.ToString("F2");
            textBoxXCoordDetail.Text = x.ToString("F2");
            textBoxYCoordDetail.Text = y.ToString("F2");
            textBoxLocationDetail.Text = GetBoltLocationDescription(angle);
        }

        private string GetBoltLocationDescription(double angle)
        {
            // Normalize angle to 0-360 range
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

        private void SetInteractionStatus(double? interactionValue, bool? interactionPass, bool isWarning = false)
        {
            textBox14.BackColor = Color.White;
            textBox14.ForeColor = Color.Black;
            labelStatus.Text = "";
            labelStatus.ForeColor = Color.Black;

            if (!interactionValue.HasValue)
            {
                textBox14.Text = "";
                textBox14.BackColor = Color.LightYellow;
                labelStatus.Text = "WARNING";
                labelStatus.ForeColor = Color.Goldenrod;
                return;
            }

            textBox14.Text = interactionValue.Value.ToString("F4");

            if (isWarning)
            {
                textBox14.BackColor = Color.Khaki;
                labelStatus.Text = "WARNING";
                labelStatus.ForeColor = Color.Goldenrod;
            }
            else if (interactionPass == true)
            {
                textBox14.BackColor = Color.LightGreen;
                labelStatus.Text = "PASS";
                labelStatus.ForeColor = Color.Green;
            }
            else
            {
                textBox14.BackColor = Color.LightCoral;
                labelStatus.Text = "FAIL";
                labelStatus.ForeColor = Color.Red;
            }
        }

        private void CalculateMultiColumnAnchorBoltValues(AnchorBoltEntity anchorBolt)
        {
            var eq = new FoundationEquations.MultiColumnAnchorBoltEquations();

            // Override labels for Multi-Column
            label1.Text = "Tension Side Legs:";
            label2.Text = "Leg Radius (in):";
            label3.Text = "Tension Force (kips):";
            label6.Text = "Tension per Bolt (kips):";
            label7.Text = "Required Steel Area (in²):";
            label8.Text = "Steel Tension Capacity (kips):";
            label9.Text = "Steel Shear Capacity (kips):";
            label18.Text = "Concrete Breakout (kips):";
            label10.Text = "Pullout Capacity (kips):";
            label11.Text = "Pryout Capacity (kips):";
            label12.Text = "Interaction Check:";
            label13.Text = "Edge Distance (in):";
            label14.Text = "Required Min Edge (in):";

            // Hide unused single column specific fields
            label15.Visible = textBox15.Visible = false;
            label16.Visible = textBox16.Visible = false;
            label17.Visible = textBox17.Visible = false;
            labelBreakoutStatus.Visible = false;
            
            labelTbp.Visible = textBoxTbp.Visible = false;
            labelMu.Visible = textBoxMu.Visible = false;
            labelFcPrime.Visible = textBoxFcPrime.Visible = false;
            labelHef.Visible = textBoxHef.Visible = false;
            labelDistMethod.Visible = textBoxDistMethod.Visible = false;
            groupBoxBoltDetail.Visible = false;
            
            // Step 1
            int totalColumns = AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4;
            int tensionLegs = eq.TensionLegs(totalColumns);
            
            // Step 2
            double dcone = anchorBolt.Dcone ?? 0;
            double legRadius = eq.LegRadiusInches(dcone);
            
            // Step 3
            double mu = anchorBolt.Mu ?? 0;
            double totalTension = eq.TotalOverturningTension(mu, legRadius);
            double tensionPerLeg = eq.TensionPerLeg(totalTension, tensionLegs);
            
            // Step 4
            int nb = anchorBolt.Nb;
            double tensionPerBolt = eq.TensionPerBolt(tensionPerLeg, nb);
            
            // Step 5
            double phi = anchorBolt.Phi ?? 0.75;
            double fy = anchorBolt.Fy ?? 36.0;
            double requiredArea = eq.RequiredSteelArea(tensionPerBolt, phi, fy);
            
            // Step 6 / 7
            double db = anchorBolt.Db;
            double boltArea = anchorBolt.Ab > 0 ? anchorBolt.Ab : eq.BoltArea(db);
            double tensionCapacity = eq.SteelTensionCapacity(phi, boltArea, fy);
            
            // Step 8
            double vu = anchorBolt.Vu;
            double shearPerLeg = eq.ShearPerLeg(vu, totalColumns);
            double shearPerBolt = eq.ShearPerBolt(shearPerLeg, nb);
            double shearCapacity = eq.SteelShearCapacity(phi, boltArea, fy);
            
            // Step 9
            double hef = anchorBolt.Hef ?? 40.0;
            double fcPrime = anchorBolt.FcPrime ?? 4000.0;
            double kc = 24;
            double breakoutCapacity = eq.ConcreteBreakoutStrength(phi, kc, fcPrime, hef);
            
            // Step 10
            double washerSize = anchorBolt.WasherSize ?? 5.0;
            double washerArea = Math.Pow(washerSize, 2);
            double pulloutCapacity = eq.PulloutStrength(washerArea, fcPrime);
            
            // Step 11
            double pryoutCapacity = eq.PryoutStrength(breakoutCapacity, hef);
            
            // Step 12
            double interaction = eq.InteractionRatio(tensionPerBolt, tensionCapacity, shearPerBolt, shearCapacity);
            
            // Step 13
            double pedestalSize = anchorBolt.PedestalSize ?? 39.0;
            double boltSpacing = anchorBolt.BoltSpacing ?? 12.0;
            double edgeDistance = (anchorBolt.E.HasValue && anchorBolt.E.Value > 0) ? anchorBolt.E.Value : eq.EdgeDistance(pedestalSize, boltSpacing);
            double minEdgeDistance = eq.MinimumEdgeDistance(db);

            // Populate textboxes
            textBox1.Text = tensionLegs.ToString();
            textBox2.Text = legRadius.ToString("F2");
            textBox3.Text = tensionPerLeg.ToString("F2");
            textBox6.Text = tensionPerBolt.ToString("F2");
            textBox7.Text = requiredArea.ToString("F2");
            textBox8.Text = tensionCapacity.ToString("F2");
            textBox9.Text = shearCapacity.ToString("F2");
            textBox18.Text = breakoutCapacity.ToString("F2");
            textBox10.Text = pulloutCapacity.ToString("F2");
            textBox11.Text = pryoutCapacity.ToString("F2");
            textBox12.Text = interaction.ToString("F3");
            textBox13.Text = edgeDistance.ToString("F2");
            textBox14.Text = minEdgeDistance.ToString("F2");
            
            // Status Check 
            bool tensionPass = tensionCapacity > tensionPerBolt;
            textBox8.BackColor = tensionPass ? Color.LightGreen : Color.LightCoral;
            
            bool shearPass = shearCapacity > shearPerBolt;
            textBox9.BackColor = shearPass ? Color.LightGreen : Color.LightCoral;

            bool breakoutPass = breakoutCapacity > tensionPerBolt;
            textBox18.BackColor = breakoutPass ? Color.LightGreen : Color.LightCoral;

            bool pulloutPass = pulloutCapacity > tensionPerBolt;
            textBox10.BackColor = pulloutPass ? Color.LightGreen : Color.LightCoral;

            bool pryoutPass = pryoutCapacity > shearPerBolt;
            textBox11.BackColor = pryoutPass ? Color.LightGreen : Color.LightCoral;

            bool interactionPass = interaction <= 1.0;
            textBox12.BackColor = interactionPass ? Color.LightGreen : Color.LightCoral;
            
            bool edgePass = edgeDistance >= minEdgeDistance;
            textBox13.BackColor = edgePass ? Color.LightGreen : Color.LightCoral;
            
            labelStatus.Text = interactionPass ? "PASS" : "FAIL";
            labelStatus.ForeColor = interactionPass ? Color.Green : Color.Red;
        }

        private string BuildBoltAnglesText(FoundationEquations.AnchorBoltEquations eq, AnchorBoltEntity anchorBolt)
        {
            var sb = new StringBuilder();

            for (int i = 1; i <= anchorBolt.Nb; i++)
            {
                double angle = eq.BoltAngle(anchorBolt.Ab, i, anchorBolt.Nb);

                if (i > 1)
                    sb.Append(Environment.NewLine);

                sb.Append($"Bolt {i}: {angle:F2}°");
            }

            return sb.ToString();
        }

        private string BuildBoltCoordinatesText(FoundationEquations.AnchorBoltEquations eq, AnchorBoltEntity anchorBolt)
        {
            var sb = new StringBuilder();

            for (int i = 1; i <= anchorBolt.Nb; i++)
            {
                double angle = eq.BoltAngle(anchorBolt.Ab, i, anchorBolt.Nb);
                double x = eq.BoltXCoordinate(anchorBolt.Rb, angle);
                double y = eq.BoltYCoordinate(anchorBolt.Rb, angle);

                if (i > 1)
                    sb.Append(Environment.NewLine);

                sb.Append($"Bolt {i}: ({x:F2}, {y:F2})");
            }

            return sb.ToString();
        }

        private void textBox14_DoubleClick(object sender, EventArgs e)
        {
            AnchorBoltParameters anchorBoltParameters = new AnchorBoltParameters();
            if (anchorBoltParameters.ShowDialog() == DialogResult.OK)
            {
                LoadAnchorBoltCalculatedValues();
            }
        }
    }
}
