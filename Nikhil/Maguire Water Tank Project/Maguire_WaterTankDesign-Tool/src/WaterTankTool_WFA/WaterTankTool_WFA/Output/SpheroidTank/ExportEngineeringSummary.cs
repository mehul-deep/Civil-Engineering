using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Output.SpheroidTank
{
    internal class ExportEngineeringSummary
    {
        public void RunExport(IWin32Window owner = null)
        {
            var context = WaterTankDbContext.GetInstance();
            var tankProps = context.TankProperties?.FirstOrDefault();
            var basePlate = context.BasePlateEntity?.FirstOrDefault();
            var anchorBolt = context.AnchorBoltEntity?.FirstOrDefault();
            var material = context.MaterialProperties?.FirstOrDefault();
            var baseSegments = context.SegmentProperties?.Where(s => s.SegmentType == "Base").ToList();
            var colSegments = context.SegmentProperties?.Where(s => s.SegmentType == "Cylinder").ToList();

            using var sfd = new SaveFileDialog
            {
                Title = "Export Engineering Summary",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"EngineeringSummary_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (sfd.ShowDialog(owner) != DialogResult.OK)
                return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Engineering Summary");

            int row = 1;

            // Title Block
            ws.Cell(row, 1).Value = "MAGUIRE WATER TANK PROJECT - ENGINEERING DESIGN & CAPACITY SUMMARY";
            ws.Range(row, 1, row, 4).Merge();
            ws.Cell(row, 1).Style.Font.FontName = "Tahoma";
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(row).Height = 30;
            row += 2;

            // Section 1: General Project & Geometry
            row = AddSectionHeader(ws, row, "1. PROJECT & TANK GEOMETRY OVERVIEW");
            row = AddDataRow(ws, row, "Tank Designation", $"TANK-{tankProps?.TankNumber ?? 1}", "Capacity (MG)", tankProps?.Capacity ?? "100 MG");
            
            double baseConeDia = (double)(baseSegments?.FirstOrDefault()?.DiameterFinal ?? 0);
            double baseConeHt = (double)(baseSegments?.LastOrDefault()?.HeightFinal ?? 0);
            row = AddDataRow(ws, row, "Base Cone Diameter (ft)", baseConeDia, "Base Cone Height (ft)", baseConeHt);
            
            double colDia = (double)(colSegments?.FirstOrDefault()?.Diameter ?? 0);
            double colHt = ((double)(colSegments?.LastOrDefault()?.HeightFinal ?? 0)) - ((double)(colSegments?.FirstOrDefault()?.HeightInitial ?? 0));
            row = AddDataRow(ws, row, "Column Diameter (ft)", colDia, "Column Height (ft)", colHt);
            row++;

            // Section 2: Governing Structural Demands
            row = AddSectionHeader(ws, row, "2. GOVERNING STRUCTURAL LOADS & DEMANDS");
            double mu = anchorBolt?.Mu ?? basePlate?.OverturningMoment ?? 0;
            double pu = anchorBolt?.Pu ?? basePlate?.Pu ?? 0;
            double vu = anchorBolt?.Vu ?? 0;
            row = AddDataRow(ws, row, "Overturning Moment (Mu) (kip-ft)", mu, "Factored Axial Load (Pu) (kips)", pu);
            row = AddDataRow(ws, row, "Base Shear Force (Vu) (kips)", vu, "Governing Load Factor", "1.667 (Ultimate)");
            row++;

            // Section 3: Anchor Bolt Design (ACI 318-19 Appendix D)
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                row = AddSectionHeader(ws, row, "3. ANCHOR BOLT DESIGN & BREAKOUT CHECK (ACI 318-19 - MULTI-LEG TOWER)");
                var mcEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
                int totalCols = (anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : (AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4);
                int tensionLegs = mcEq.TensionLegs(totalCols);
                int nb = anchorBolt?.Nb ?? 4;
                double db = anchorBolt?.Db ?? 1.0;
                double dcone = anchorBolt?.Dcone ?? 20.04;
                double legRadius = mcEq.LegRadiusInches(dcone);
                double abMu = anchorBolt?.Mu ?? (basePlate?.OverturningMoment ?? mu);
                double totalTension = mcEq.TotalOverturningTension(abMu, legRadius);
                double tensionPerLeg = mcEq.TensionPerLeg(totalTension, tensionLegs);
                double tensionPerBolt = mcEq.TensionPerBolt(tensionPerLeg, nb);

                double fy = anchorBolt?.Fy ?? 36.0;
                double fu = anchorBolt?.Fu ?? 58.0;
                double phi = anchorBolt?.Phi ?? 0.75;
                double requiredArea = mcEq.RequiredSteelArea(tensionPerBolt, phi, fy);
                double boltArea = (anchorBolt != null && anchorBolt.Ab > 0) ? anchorBolt.Ab : mcEq.BoltArea(db);
                double tensionCapacity = mcEq.SteelTensionCapacity(phi, boltArea, fy);

                double boltVu = anchorBolt?.Vu ?? 0;
                double shearPerLeg = mcEq.ShearPerLeg(boltVu, totalCols);
                double shearPerBolt = mcEq.ShearPerBolt(shearPerLeg, nb);
                double shearCapacity = mcEq.SteelShearCapacity(phi, boltArea, fy);

                double hef = anchorBolt?.Hef ?? 40.0;
                double fcPrime = anchorBolt?.FcPrime ?? (basePlate?.Fc_prime ?? 4000.0);
                double breakoutCapacity = mcEq.ConcreteBreakoutStrength(phi, 24, fcPrime, hef);

                double washerSize = anchorBolt?.WasherSize ?? 5.0;
                double washerArea = Math.Pow(washerSize, 2);
                double pulloutCapacity = mcEq.PulloutStrength(washerArea, fcPrime);
                double pryoutCapacity = mcEq.PryoutStrength(breakoutCapacity, hef);
                double interaction = mcEq.InteractionRatio(tensionPerBolt, tensionCapacity, shearPerBolt, shearCapacity);

                double pedestalSize = anchorBolt?.PedestalSize ?? 39.0;
                double boltSpacing = anchorBolt?.BoltSpacing ?? 12.0;
                double edgeDist = (anchorBolt != null && anchorBolt.E.HasValue && anchorBolt.E.Value > 0) ? anchorBolt.E.Value : mcEq.EdgeDistance(pedestalSize, boltSpacing);
                double minEdgeDist = mcEq.MinimumEdgeDistance(db);

                row = AddDataRow(ws, row, "Total Tower Columns / Legs", totalCols, "Tension Resisting Legs", tensionLegs);
                row = AddDataRow(ws, row, "Bolts per Pedestal (Nb)", nb, "Nominal Bolt Diameter (db) (in)", db);
                row = AddDataRow(ws, row, "Cone Diameter (Dcone) (ft)", dcone, "Leg Radius from Center (in)", Math.Round(legRadius, 4));
                row = AddDataRow(ws, row, "Overturning Moment (Mu) (kip-ft)", mu, "Total Overturning Uplift (kips)", Math.Round(totalTension, 4));
                row = AddDataRow(ws, row, "Tension per Leg (kips)", Math.Round(tensionPerLeg, 4), "Tension per Bolt (Tu/bolt) (kips)", Math.Round(tensionPerBolt, 4));
                row = AddDataRow(ws, row, "Shear per Leg (kips)", Math.Round(shearPerLeg, 4), "Shear per Bolt (Vu/bolt) (kips)", Math.Round(shearPerBolt, 4));
                row = AddDataRow(ws, row, "Bolt Yield Strength (Fy) (ksi)", fy, "Bolt Ultimate Strength (Fu) (ksi)", fu);
                row = AddDataRow(ws, row, "Concrete Strength (f'c) (psi)", fcPrime, "Embedment Depth (hef) (in)", hef);
                row = AddDataRow(ws, row, "Required Steel Area (in²)", Math.Round(requiredArea, 4), "Actual Bolt Area (Ab) (in²)", Math.Round(boltArea, 4));
                row = AddDataRow(ws, row, "Tensile Capacity (phi*Tn) (kips)", Math.Round(tensionCapacity, 4), "Shear Capacity (phi*Vn) (kips)", Math.Round(shearCapacity, 4));
                row = AddDataRow(ws, row, "Breakout Capacity (phi*Ncb) (kips)", Math.Round(breakoutCapacity, 4), "Pullout Capacity (kips)", Math.Round(pulloutCapacity, 4));
                row = AddDataRow(ws, row, "Pryout Capacity (kips)", Math.Round(pryoutCapacity, 4), "Pedestal Size (B x L) (in)", $"{pedestalSize} x {pedestalSize}");
                row = AddDataRow(ws, row, "Actual Edge Distance (in)", Math.Round(edgeDist, 4), "Required Min Edge (in)", Math.Round(minEdgeDist, 4));
                row = AddDataRow(ws, row, "Combined Interaction Ratio", Math.Round(interaction, 4), "Design Status", interaction <= 1.0 ? "PASS / OK" : "EXCEEDS CAPACITY");
                row++;
            }
            else
            {
                row = AddSectionHeader(ws, row, "3. ANCHOR BOLT DESIGN & BREAKOUT CHECK (ACI 318-19 - SINGLE-COLUMN STANDPIPE)");
                var scEq = new FoundationEquations.AnchorBoltEquations();
                int nb = anchorBolt?.Nb ?? (basePlate?.Nb ?? 0);
                double db = anchorBolt?.Db ?? 0;
                double dh = anchorBolt?.Dh ?? 0;
                double rb = anchorBolt?.Rb ?? 0;
                row = AddDataRow(ws, row, "Total Anchor Bolts (Nb)", nb, "Bolt Nominal Diameter (db) (in)", db);
                row = AddDataRow(ws, row, "Hole Diameter (dh) (in)", dh, "Bolt Circle Radius (rb) (in/ft)", rb);

                double fy = anchorBolt?.Fy ?? 36;
                double fu = anchorBolt?.Fu ?? 58;
                double fc = anchorBolt?.FcPrime ?? (basePlate?.Fc_prime ?? 4000);
                double hef = anchorBolt?.Hef ?? 24;
                row = AddDataRow(ws, row, "Bolt Yield Strength (Fy) (ksi)", fy, "Bolt Ultimate Strength (Fu) (ksi)", fu);
                row = AddDataRow(ws, row, "Concrete Strength (f'c) (psi)", fc, "Embedment Depth (hef) (in)", hef);

                double totalTensionTu = anchorBolt?.Tu ?? 0;
                if (anchorBolt != null && anchorBolt.Mu.HasValue && anchorBolt.Mu.Value > 0)
                {
                    totalTensionTu = scEq.TotalTensionDemandFromMoment(anchorBolt.Mu.Value, 2.0 * rb);
                }

                double tensionPerBolt = 0;
                string distMethod = anchorBolt?.DistributionMethod ?? "Circular Group";
                if (nb > 0)
                {
                    switch (distMethod)
                    {
                        case "Equal Distribution":
                            tensionPerBolt = scEq.TensionDemandPerBolt_Equal(totalTensionTu, nb);
                            break;
                        case "Effective Bolts":
                            tensionPerBolt = scEq.TensionDemandPerBolt_Effective(totalTensionTu, nb);
                            break;
                        case "Circular Group":
                        default:
                            tensionPerBolt = scEq.TensionDemandPerBolt_CircularGroup(totalTensionTu, nb);
                            break;
                    }
                }

                double boltVu = anchorBolt?.Vu ?? 0;
                double shearPerBolt = nb > 0 ? scEq.ShearDemandPerBolt(boltVu, nb) : 0;
                row = AddDataRow(ws, row, "Total Uplift Tension (Tu) (kips)", Math.Round(totalTensionTu, 4), "Distribution Method", distMethod);
                row = AddDataRow(ws, row, "Tension Demand / Bolt (Nua) (kips)", Math.Round(tensionPerBolt, 4), "Shear Demand / Bolt (Vua) (kips)", Math.Round(shearPerBolt, 4));

                double tensileCapacity = 0;
                if (anchorBolt != null && anchorBolt.Fu.HasValue) tensileCapacity = scEq.TensileDesignStrengthUltimate(db, anchorBolt.Fu.Value, anchorBolt.Phi ?? 0.75);
                else if (anchorBolt != null && anchorBolt.Fy.HasValue) tensileCapacity = scEq.TensileDesignStrength(db, anchorBolt.Fy.Value, anchorBolt.Phi ?? 0.75);

                double shearCapacity = 0;
                if (anchorBolt != null && anchorBolt.Fu.HasValue) shearCapacity = scEq.ShearDesignStrengthUltimate(db, anchorBolt.Fu.Value, anchorBolt.Phi ?? 0.75);
                else if (anchorBolt != null && anchorBolt.Fy.HasValue) shearCapacity = scEq.ShearDesignStrength(db, anchorBolt.Fy.Value, anchorBolt.Phi ?? 0.75);

                double breakoutCapacity = scEq.ConcreteBreakoutStrength(24, fc, hef) / 1000.0 * (anchorBolt?.Phi ?? 0.75);
                double breakoutUtil = breakoutCapacity > 0 ? scEq.ConcreteBreakoutUtilization(tensionPerBolt, breakoutCapacity / (anchorBolt?.Phi ?? 0.75), anchorBolt?.Phi ?? 0.75) : 0;

                row = AddDataRow(ws, row, "Tensile Design Strength (phi*Nn) (kips)", Math.Round(tensileCapacity, 4), "Shear Design Strength (phi*Vn) (kips)", Math.Round(shearCapacity, 4));
                row = AddDataRow(ws, row, "Breakout Strength (phi*Ncb) (kips)", Math.Round(breakoutCapacity, 4), "Breakout Utilization Ratio", Math.Round(breakoutUtil, 4));

                double interaction = 0.0;
                string status = "OK";
                if (tensileCapacity > 0 && shearCapacity > 0)
                {
                    interaction = scEq.InteractionCheck(tensionPerBolt, tensileCapacity, shearPerBolt, shearCapacity);
                    if (interaction > 1.0) status = "EXCEEDS CAPACITY";
                }
                row = AddDataRow(ws, row, "Combined Interaction Ratio", Math.Round(interaction, 4), "Design Status", status);
                row++;
            }

            // Section 4: Base Plate Design (AISC LRFD Annular Strip Method)
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                row = AddSectionHeader(ws, row, "4. BASE PLATE DESIGN & BEARING STRESS (AISC LRFD - MULTI-LEG TOWER)");
                var mcBpEq = new FoundationEquations.MultiColumnBasePlateEquations();
                int totalLegs = (anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : (AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4);
                double p = anchorBolt?.PedestalSize ?? 39.0;
                double l = anchorBolt?.PedestalSize ?? 39.0;
                double d_o = basePlate?.Ro ?? 30.0;
                double dpip = basePlate?.Dbp ?? (anchorBolt?.Dcone ?? 20.04);
                double t = basePlate?.T ?? 1.50;
                double fy = basePlate?.Fy ?? 36.0;
                double fcPrimePsi = basePlate?.Fc_prime ?? (anchorBolt?.FcPrime ?? 4000.0);
                double fcPrimeKsi = fcPrimePsi > 100 ? fcPrimePsi / 1000.0 : fcPrimePsi;
                double totalMuKipFt = basePlate?.OverturningMoment ?? (anchorBolt?.Mu ?? 0);
                double totalPuKips = basePlate?.Pu ?? (anchorBolt?.Pu ?? 0);

                double a1 = mcBpEq.BasePlateArea(d_o);
                double a2 = mcBpEq.PedestalArea(p, l);
                double fp = mcBpEq.MaximumBearingStress(fcPrimeKsi, a2, a1, 0.65);
                double fpLimit = mcBpEq.BearingStressLimit(fcPrimeKsi, 0.65);
                double pn = mcBpEq.BearingCapacity(fp, a1);

                double muPedFt = mcBpEq.FactoredMomentPerPedestal(totalMuKipFt, totalLegs);
                double muPedIn = mcBpEq.ConvertMomentToKipIn(muPedFt);
                double puPed = totalLegs > 0 ? Math.Round(totalPuKips / totalLegs, 2) : totalPuKips;
                double puComp = mcBpEq.AppliedLoadPerCompressionPedestal(totalPuKips, totalLegs);
                double eVal = mcBpEq.EquivalentEccentricity(muPedIn, puPed);
                double nLimit = mcBpEq.BearingConditionLimit(d_o);

                double colArea = mcBpEq.ColumnBearingArea(dpip);
                double qVal = mcBpEq.BearingPressure(puPed, colArea);
                double mVal = mcBpEq.PlateProjection(d_o, dpip);
                double mplu = mcBpEq.StripPlasticMoment(qVal, mVal);
                double treq = mcBpEq.RequiredThickness(mplu, fy, 0.90);

                double volPerPlate = a1 * t;
                double weightPerPlate = (volPerPlate / 1728.0) * 0.490;
                double totalWeight = weightPerPlate * totalLegs;

                row = AddDataRow(ws, row, "Tower Supporting Legs (Ns)", totalLegs, "Pipe Column Diameter Dpip (in)", dpip);
                row = AddDataRow(ws, row, "Outer Diameter Do (Ro) (in)", d_o, "N/A", "-");
                row = AddDataRow(ws, row, "Pedestal Size P x L (in)", $"{p} x {l}", "Base Plate Area A1 (in²)", Math.Round(a1, 4));
                row = AddDataRow(ws, row, "Supporting Area A2 (in²)", Math.Round(a2, 4), "Bearing Stress Demand Fp (ksi)", Math.Round(fp, 4));
                row = AddDataRow(ws, row, "Design Bearing Limit (phi*Fp) (ksi)", Math.Round(fpLimit, 4), "Bearing Capacity Pn (kips)", Math.Round(pn, 4));
                row = AddDataRow(ws, row, "Factored Axial Load Pu,ped (kips)", Math.Round(puPed, 4), "Max Compression Load Pu,comp (kips)", Math.Round(puComp, 4));
                row = AddDataRow(ws, row, "Factored Moment Mu,ped (kip-ft)", Math.Round(muPedFt, 4), "Equivalent Eccentricity e (in)", Math.Round(eVal, 4));
                row = AddDataRow(ws, row, "Bearing Limit D/8 (in)", Math.Round(nLimit, 4), "Bearing Pressure Demand q (ksi)", Math.Round(qVal, 4));
                row = AddDataRow(ws, row, "Cantilever Projection m (in)", Math.Round(mVal, 4), "Plastic Moment Mplu (kip-in/in)", Math.Round(mplu, 4));
                row = AddDataRow(ws, row, "Required Thickness treq (in)", Math.Round(treq, 4), "Actual Plate Thickness t (in)", t);
                row = AddDataRow(ws, row, "Weight per Plate (kips)", Math.Round(weightPerPlate, 4), "Total Weight across all Legs (kips)", Math.Round(totalWeight, 4));
                row = AddDataRow(ws, row, "Plate Compactness Status", (t >= treq && pn >= puComp && fp <= fpLimit) ? "COMPACT / OK" : "CHECK THICKNESS / BEARING", "", "");
                row++;
            }
            else
            {
                row = AddSectionHeader(ws, row, "4. BASE PLATE DESIGN & BEARING STRESS (AISC LRFD - SINGLE-COLUMN STANDPIPE)");
                var scBpEq = new FoundationEquations.BasePlateEquations();
                double dbp = basePlate?.Dbp ?? (baseConeDia * 12);
                double ro = basePlate?.Ro ?? ((baseConeDia * 12 / 2) + 7);
                double ri = basePlate?.Ri ?? (ro - 12);
                double wrw = basePlate?.Wrw ?? 1.5;
                row = AddDataRow(ws, row, "Base Plate Diameter (Dbp) (in)", dbp, "Ring Wall Width (Wrw) (ft)", wrw);
                row = AddDataRow(ws, row, "Outside Radius (Ro) (in)", ro, "Inside Radius (Ri) (in)", ri);

                double grossArea = scBpEq.GrossArea(ro, ri, basePlate?.Theta ?? 360.0);
                double netArea = scBpEq.NetArea(ro, ri, basePlate?.Theta ?? 360.0, basePlate?.Nh ?? 0, basePlate?.Dh ?? 0);
                double areaA1 = grossArea * 144.0;
                double x1 = (ro - ri) * 12.0;
                double x2 = (wrw * 12.0);

                double fcPrimeKsi = (basePlate?.Fc_prime ?? 4000) > 100 ? (basePlate?.Fc_prime ?? 4000) / 1000.0 : (basePlate?.Fc_prime ?? 4000);
                double fp = (basePlate != null && basePlate.A2.HasValue && basePlate.A2.Value > 0)
                    ? scBpEq.MaximumDesignBearingStress(fcPrimeKsi, basePlate.A2.Value, areaA1, 0.90)
                    : scBpEq.MaximumDesignBearingStress(fcPrimeKsi, x2, x1, 0.90);

                double a2 = basePlate?.A2 ?? (x2 * x2);
                double bearUtil = basePlate?.BearingUtilization ?? 0;
                row = AddDataRow(ws, row, "Supporting Area A2 (sq in)", Math.Round(a2, 4), "Bearing Stress Demand (fp) (ksi)", Math.Round(fp, 4));

                double bpPu = basePlate?.Pu ?? pu;
                double bpMu = basePlate?.OverturningMoment ?? mu;
                double mKipIn = scBpEq.ConvertMomentToKipIn(bpMu);
                double mStrip = scBpEq.CircumferentialMomentPerUnitStrip(mKipIn, dbp);
                double eVal = scBpEq.EquivalentEccentricity(mStrip, bpPu, dbp);
                double bearingLimit = scBpEq.BearingConditionLimit(x1);
                double mCritical = scBpEq.CriticalSection(x1);
                double mPlu = scBpEq.StripPlasticMoment(mStrip);
                double treq = scBpEq.RequiredThickness(mPlu, basePlate?.Fy ?? 36.0, 0.90);

                row = AddDataRow(ws, row, "Circumferential Moment Mstrip (kip-in/in)", Math.Round(mStrip, 4), "Equivalent Eccentricity e (in)", Math.Round(eVal, 4));
                row = AddDataRow(ws, row, "Bearing Condition Limit N/6 (in)", Math.Round(bearingLimit, 4), "Critical Section Cantilever m (in)", Math.Round(mCritical, 4));
                row = AddDataRow(ws, row, "Strip Plastic Moment Mplu (kip-in/in)", Math.Round(mPlu, 4), "Design Bearing Strength (phi Pp) (kips)", Math.Round(basePlate?.Phi_Pp ?? 0, 4));

                double t = basePlate?.T ?? 0;
                double thkUtil = basePlate?.ThicknessUtilization ?? (t > 0 ? treq / t : 0);
                row = AddDataRow(ws, row, "Actual Plate Thickness (t) (in)", t, "Required Thickness (treq) (in)", Math.Round(treq, 4));
                row = AddDataRow(ws, row, "Thickness Utilization Ratio", Math.Round(thkUtil, 4), "Plate Compactness Status", thkUtil <= 1.0 ? "COMPACT / OK" : "CHECK THICKNESS");
                row++;
            }

            // Section 5: Material & Geotechnical Properties
            row = AddSectionHeader(ws, row, "5. MATERIAL & GEOTECHNICAL PROPERTIES");
            row = AddDataRow(ws, row, "Material Name", material?.MaterialName ?? "A36 Structural Steel", "Material Type", material?.MaterialType ?? "Carbon Steel");
            row = AddDataRow(ws, row, "Density (pcf)", material?.Density ?? 490, "Modulus of Elasticity (E) (ksi)", material?.ModulusOfElasticity ?? 29000);
            row = AddDataRow(ws, row, "Tensile Yield Stress (Fy) (psi)", material?.TensileYieldStress ?? 36000, "Tensile Ultimate Stress (Fu) (psi)", material?.TensileUltimateStress ?? 58000);

            // Format table borders and widths
            ws.Columns(1, 4).AdjustToContents();
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 32);
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 20);
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 32);
            ws.Column(4).Width = Math.Max(ws.Column(4).Width, 20);

            var fullRange = ws.Range(3, 1, row - 1, 4);
            fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wb.SaveAs(sfd.FileName);
            MessageBox.Show(owner, "Engineering Summary exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static int AddSectionHeader(IXLWorksheet ws, int row, string title)
        {
            ws.Cell(row, 1).Value = title;
            ws.Range(row, 1, row, 4).Merge();
            var cell = ws.Cell(row, 1);
            cell.Style.Font.FontName = "Tahoma";
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            cell.Style.Font.FontColor = XLColor.FromHtml("#1F497D");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(row).Height = 24;
            return row + 1;
        }

        private static int AddDataRow(IXLWorksheet ws, int row, string label1, object val1, string label2, object val2)
        {
            ws.Cell(row, 1).Value = label1;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            SetCellValue(ws.Cell(row, 2), val1);

            ws.Cell(row, 3).Value = label2;
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            SetCellValue(ws.Cell(row, 4), val2);

            ws.Row(row).Height = 20;
            return row + 1;
        }

        private static void SetCellValue(IXLCell cell, object value)
        {
            if (value == null)
            {
                cell.Clear(XLClearOptions.Contents);
                return;
            }
            switch (value)
            {
                case string s: cell.SetValue(s); break;
                case int i: cell.SetValue(i); break;
                case double d: cell.SetValue(d); break;
                case float f: cell.SetValue((double)f); break;
                case decimal m: cell.SetValue((double)m); break;
                case bool b: cell.SetValue(b); break;
                default: cell.SetValue(value.ToString() ?? ""); break;
            }
        }
    }
}
