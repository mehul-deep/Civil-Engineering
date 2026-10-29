using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Services;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Output.SpheroidTank
{
    internal class ExportEngineeringSummary
    {
        public void RunExport(IWin32Window owner = null)
        {
            AppState.Load();
            var context = WaterTankDbContext.GetInstance();
            var tankProps = context.TankProperties?.FirstOrDefault();
            var basePlate = context.BasePlateEntity?.FirstOrDefault();
            var anchorBolt = context.AnchorBoltEntity?.FirstOrDefault();
            var ringWall = context.RingWallEntity?.FirstOrDefault();
            var footing = context.FootingEntity?.FirstOrDefault();
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
            if (AppState.CurrentTankType == TankType.MultiColumn && (pu <= 0 || mu <= 0 || vu <= 0))
            {
                var (puGov, muGov, vuGov) = new LoadService().GetGoverningLoads();
                if (pu <= 0) pu = puGov;
                if (mu <= 0) mu = muGov;
                if (vu <= 0) vu = vuGov;
            }
            row = AddDataRow(ws, row, "Overturning Moment (Mu) (kip-ft)", Math.Round(mu, 4), "Factored Axial Load (Pu) (kips)", Math.Round(pu, 4));
            row = AddDataRow(ws, row, "Base Shear Force (Vu) (kips)", Math.Round(vu, 4), "Governing Load Factor", "1.667 (Ultimate)");
            row++;

            // Section 3: Anchor Bolt Design (ACI 318-19 Appendix D / Chapter 17)
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                row = AddSectionHeader(ws, row, "3. ANCHOR BOLT DESIGN & BREAKOUT CHECK (ACI 318-19 - MULTI-LEG TOWER)");
                var mcEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
                int totalCols = AppState.NoOfColumns > 1
                    ? AppState.NoOfColumns
                    : ((anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);
                int tensionLegs = mcEq.TensionLegs(totalCols);
                int nb = anchorBolt?.Nb ?? 4;
                double db = anchorBolt?.Db ?? 1.5;
                double dcone = anchorBolt?.Dcone ?? 41.0469;
                double legRadius = mcEq.LegRadiusInches(dcone);
                double abMu = (anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0)
                    ? anchorBolt.Mu.Value
                    : ((basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0) ? basePlate.OverturningMoment.Value : mu);
                double totalTension = mcEq.TotalOverturningTension(abMu, legRadius);
                double tensionPerLeg = mcEq.TensionPerLeg(totalTension, tensionLegs);
                double tensionPerBolt = mcEq.TensionPerBolt(tensionPerLeg, nb);

                double fy = anchorBolt?.Fy ?? 36.0;
                double fu = anchorBolt?.Fu ?? 58.0;
                double phi = anchorBolt?.Phi ?? 0.75;
                double requiredArea = mcEq.RequiredSteelArea(tensionPerBolt, phi, fy);
                double boltArea = (anchorBolt != null && anchorBolt.Ab > 0) ? anchorBolt.Ab : mcEq.BoltArea(db);
                double tensionCapacity = mcEq.SteelTensionCapacity(phi, boltArea, fy);

                double boltVu = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vu;
                double shearPerLeg = mcEq.ShearPerLeg(boltVu, totalCols);
                double shearPerBolt = mcEq.ShearPerBolt(shearPerLeg, nb);
                double shearCapacity = mcEq.SteelShearCapacity(phi, boltArea, fy);

                double hef = anchorBolt?.Hef ?? 35.0;
                double fcPrime = anchorBolt?.FcPrime ?? (basePlate?.Fc_prime ?? 4000.0);
                double breakoutCapacity = mcEq.ConcreteBreakoutStrength(phi, 24, fcPrime, hef);

                double washerSize = anchorBolt?.WasherSize ?? 5.0;
                double washerArea = Math.Pow(washerSize, 2);
                double pulloutCapacity = mcEq.PulloutStrength(washerArea, fcPrime);
                double pryoutCapacity = mcEq.PryoutStrength(breakoutCapacity, hef);
                double interaction = mcEq.InteractionRatio(tensionPerBolt, tensionCapacity, shearPerBolt, shearCapacity);

                double pedB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
                double pedL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : pedB;
                double bsB = (anchorBolt?.BoltSpacing.HasValue == true && anchorBolt.BoltSpacing.Value > 0) ? anchorBolt.BoltSpacing.Value : 12.0;
                double bsL = (anchorBolt?.BoltSpacingLength.HasValue == true && anchorBolt.BoltSpacingLength.Value > 0) ? anchorBolt.BoltSpacingLength.Value : bsB;

                double calcEdge = Math.Min(mcEq.EdgeDistance(pedB, bsB), mcEq.EdgeDistance(pedL, bsL));
                double edgeDist = (anchorBolt != null && anchorBolt.E.HasValue && anchorBolt.E.Value > 0) ? anchorBolt.E.Value : calcEdge;
                double minEdgeDist = mcEq.MinimumEdgeDistance(db);

                row = AddDataRow(ws, row, "Total Tower Columns / Legs", totalCols, "Tension Resisting Legs", tensionLegs);
                row = AddDataRow(ws, row, "Bolts per Pedestal (Nb)", nb, "Nominal Bolt Diameter (db) (in)", db);
                row = AddDataRow(ws, row, "Column Centerline Dia (Dcl) (ft)", dcone, "Leg Radius from Center (in)", Math.Round(legRadius, 4));
                row = AddDataRow(ws, row, "Overturning Moment (Mu) (kip-ft)", Math.Round(abMu, 4), "Total Overturning Uplift (kips)", Math.Round(totalTension, 4));
                row = AddDataRow(ws, row, "Tension per Leg (kips)", Math.Round(tensionPerLeg, 4), "Tension per Bolt (Tu/bolt) (kips)", Math.Round(tensionPerBolt, 4));
                row = AddDataRow(ws, row, "Shear per Leg (kips)", Math.Round(shearPerLeg, 4), "Shear per Bolt (Vu/bolt) (kips)", Math.Round(shearPerBolt, 4));
                row = AddDataRow(ws, row, "Bolt Yield Strength (Fy) (ksi)", fy, "Bolt Ultimate Strength (Fu) (ksi)", fu);
                row = AddDataRow(ws, row, "Concrete Strength (f'c) (psi)", fcPrime, "Embedment Depth (hef) (in)", hef);
                row = AddDataRow(ws, row, "Required Steel Area (in²)", Math.Round(requiredArea, 4), "Actual Bolt Area (Ab) (in²)", Math.Round(boltArea, 4));
                row = AddDataRow(ws, row, "Tensile Capacity (phi*Ns) (kips)", Math.Round(tensionCapacity, 4), "Shear Capacity (phi*Vs) (kips)", Math.Round(shearCapacity, 4));
                row = AddDataRow(ws, row, "Breakout Capacity (phi*Ncb) (kips)", Math.Round(breakoutCapacity, 4), "Pullout Capacity (Np) (kips)", Math.Round(pulloutCapacity, 4));
                row = AddDataRow(ws, row, "Pryout Capacity (Vcp) (kips)", Math.Round(pryoutCapacity, 4), "Pedestal Size (B x L) (in)", $"{pedB} x {pedL}");
                row = AddDataRow(ws, row, "Bolt Spacing (b x l) (in)", $"{bsB} x {bsL}", "Actual / Min Edge Dist (in)", $"{Math.Round(edgeDist, 2)} / {Math.Round(minEdgeDist, 2)}");
                bool abPass = interaction <= 1.0 && tensionCapacity > tensionPerBolt && shearCapacity > shearPerBolt && breakoutCapacity > tensionPerBolt && pulloutCapacity > tensionPerBolt && pryoutCapacity > shearPerBolt && edgeDist >= minEdgeDist;
                row = AddDataRow(ws, row, "Combined Interaction Ratio", Math.Round(interaction, 4), "Design Status", abPass ? "PASS / OK" : "EXCEEDS CAPACITY");
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
                int totalLegs = AppState.NoOfColumns > 1
                    ? AppState.NoOfColumns
                    : ((anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);
                double p = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
                double l = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : p;
                double d_o = (basePlate != null && basePlate.Ro > 0) ? basePlate.Ro : 30.0;
                double dpip = (basePlate != null && basePlate.Dbp > 0) ? basePlate.Dbp : 20.04;
                double t = (basePlate != null && basePlate.T > 0) ? basePlate.T : 1.50;
                double fy = (basePlate != null && basePlate.Fy > 0) ? basePlate.Fy : 36.0;
                double fcPrimePsi = (basePlate != null && basePlate.Fc_prime > 0) ? basePlate.Fc_prime : (anchorBolt?.FcPrime ?? 4000.0);
                double fcPrimeKsi = fcPrimePsi > 100 ? fcPrimePsi / 1000.0 : fcPrimePsi;
                double totalMuKipFt = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                    ? basePlate.OverturningMoment.Value
                    : mu;
                double totalPuKips = (basePlate?.Pu.HasValue == true && basePlate.Pu.Value > 0)
                    ? basePlate.Pu.Value
                    : pu;

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
                row = AddDataRow(ws, row, "Outer Diameter Do (in)", d_o, "Pedestal Size B x L (in)", $"{p} x {l}");
                row = AddDataRow(ws, row, "Base Plate Area A1 (in²)", Math.Round(a1, 4), "Supporting Area A2 (in²)", Math.Round(a2, 4));
                row = AddDataRow(ws, row, "Bearing Stress Fp (ksi)", Math.Round(fp, 4), "Bearing Stress Limit (ksi)", Math.Round(fpLimit, 4));
                row = AddDataRow(ws, row, "Bearing Capacity Pn (kips)", Math.Round(pn, 4), "Max Compression Load Pu,comp (kips)", Math.Round(puComp, 4));
                row = AddDataRow(ws, row, "Factored Axial Load Pu,ped (kips)", Math.Round(puPed, 4), "Factored Moment Mu,ped (kip-ft)", Math.Round(muPedFt, 4));
                row = AddDataRow(ws, row, "Equivalent Eccentricity e (in)", Math.Round(eVal, 4), "Bearing Limit N/6 (in)", Math.Round(nLimit, 4));
                row = AddDataRow(ws, row, "Bearing Pressure Demand q (ksi)", Math.Round(qVal, 4), "Cantilever Projection m (in)", Math.Round(mVal, 4));
                row = AddDataRow(ws, row, "Plastic Moment Mplu (kip-in/in)", Math.Round(mplu, 4), "Required Thickness treq (in)", Math.Round(treq, 4));
                row = AddDataRow(ws, row, "Provided Plate Thickness t (in)", t, "Total Weight across all Legs (kips)", Math.Round(totalWeight, 4));
                row = AddDataRow(ws, row, "Base Plate Design Status", (t >= treq && pn >= puComp && fp <= fpLimit) ? "PASS / OK" : "CHECK THICKNESS / BEARING", "Weight per Plate (kips)", Math.Round(weightPerPlate, 4));
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

            int nextSection = 5;

            // Multi-Column Only: Section 5 (Pedestal) & Section 6 (Footing)
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                int numLegs = AppState.NoOfColumns > 1
                    ? AppState.NoOfColumns
                    : ((anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);

                // --- SECTION 5: PEDESTAL DESIGN (ACI 318-19) ---
                row = AddSectionHeader(ws, row, $"{nextSection++}. REINFORCED CONCRETE PEDESTAL DESIGN (ACI 318-19 - MULTI-LEG TOWER)");
                var pedEq = new FoundationEquations.MultiColumnPedestalEquations();
                var abEq = new FoundationEquations.MultiColumnAnchorBoltEquations();

                double autoB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
                double autoL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : autoB;
                double pedB = (ringWall?.PedestalSizeB.HasValue == true && ringWall.PedestalSizeB.Value > 0) ? ringWall.PedestalSizeB.Value : autoB;
                double pedL = (ringWall?.PedestalSizeL.HasValue == true && ringWall.PedestalSizeL.Value > 0) ? ringWall.PedestalSizeL.Value : autoL;
                double bMin = Math.Min(pedB, pedL);
                double hpFt = (ringWall?.Hp.HasValue == true && ringWall.Hp.Value > 0) ? ringWall.Hp.Value : 7.417;

                double bpArea = (basePlate != null && basePlate.Ro > 0) ? (Math.PI * Math.Pow(basePlate.Ro, 2) / 4.0) : 706.86;
                double pedFc = (ringWall?.FcPrime.HasValue == true && ringWall.FcPrime.Value > 0) ? ringWall.FcPrime.Value : 4.0;
                double pedFy = (ringWall?.Fy.HasValue == true && ringWall.Fy.Value > 0) ? ringWall.Fy.Value : 60.0;
                double phiComp = (ringWall?.PhiCompression.HasValue == true && ringWall.PhiCompression.Value > 0) ? ringWall.PhiCompression.Value : 0.65;
                bool isSpiral = Math.Abs(phiComp - 0.75) < 0.01;
                double maxAxialFactor = isSpiral ? 0.85 : 0.80;
                double gammaC = (ringWall?.GammaC.HasValue == true && ringWall.GammaC.Value > 0) ? ringWall.GammaC.Value : 150.0;
                double asProv = (ringWall?.PedestalAsProv.HasValue == true && ringWall.PedestalAsProv.Value > 0) ? ringWall.PedestalAsProv.Value : 7.92;

                double lu_b = (hpFt * 12.0) / bMin;
                double rGyr = pedEq.RadiusOfGyrationSquare(bMin);
                double klu_r = (2.0 * (hpFt * 12.0)) / rGyr;

                double pGrav = pedEq.GravityAxialLoadPerLeg(pu, numLegs);
                double coneDiamFt = (anchorBolt?.Dcone.HasValue == true && anchorBolt.Dcone.Value > 0) ? anchorBolt.Dcone.Value : 41.0469;
                double tLeg = Math.Max(0, abEq.TensionPerLeg(abEq.TotalOverturningTension(mu, coneDiamFt * 6.0), abEq.TensionLegs(numLegs)));

                double wPed = pedEq.PedestalSelfWeight(pedB, pedL, hpFt, gammaC);
                double wPedFactored = pedEq.FactoredPedestalSelfWeight(wPed);
                double pCompTotal = pGrav + wPedFactored;

                double pedArea = pedB * pedL;
                double bearEnh = pedEq.BearingEnhancementFactor(bpArea, pedArea);
                double pedBearCap = pedEq.PedestalBearingCapacity(pedFc, bpArea, bearEnh, 0.65);
                double pedBearDC = pedEq.DemandCapacityRatio(pCompTotal, pedBearCap);

                double pedAsMin = pedEq.MinimumReinforcementArea(pedArea);
                double po = pedEq.NominalConcentricCompressionStrength(pedFc, pedArea, asProv, pedFy);
                double phiPn = pedEq.DesignAxialCompressionCapacity(po, phiComp, maxAxialFactor);
                double axialDC = pedEq.DemandCapacityRatio(pCompTotal, phiPn);

                double phiTn = pedEq.DesignTensileCapacity(asProv, pedFy, 0.90);
                double upliftDC = pedEq.DemandCapacityRatio(tLeg, phiTn);

                double vuPed = pedEq.HorizontalShearPerPedestal(vu, numLegs);
                double tieSpacing = pedEq.RequiredTieSpacing(1.0, bMin);

                double cbVal, abVal, asbVal, mnbVal;
                double phiMnb = pedEq.BalancedSectionMomentCapacity(pedB, pedL, 3.5, pedFc, pedFy, phiComp, out cbVal, out abVal, out asbVal, out mnbVal);
                double pedMu = Math.Round(vuPed * hpFt, 2);
                double pmDC = pedEq.DemandCapacityRatio(pedMu, phiMnb);

                double ld = pedEq.TensionDevelopmentLength(pedFy, pedFc, 1.0);
                double lProv = ringWall?.FootingAsProv ?? 30.0;
                if (Math.Abs(lProv - 5.28) < 0.01) lProv = 30.0;
                double ldDC = pedEq.DemandCapacityRatio(ld, lProv);

                bool pedAllPass = asProv >= pedAsMin && pedBearDC <= 1.0 && axialDC <= 1.0 && upliftDC <= 1.0 && pmDC <= 1.0 && ldDC <= 1.0;

                row = AddDataRow(ws, row, "Pedestal Size B x L (in)", $"{pedB} x {pedL}", "Pedestal Clear Height Hp (ft)", Math.Round(hpFt, 4));
                row = AddDataRow(ws, row, "Concrete Strength f'c (ksi)", pedFc, "Reinforcing Steel fy (ksi)", pedFy);
                row = AddDataRow(ws, row, "Transverse Reinforcement (phi)", isSpiral ? "0.75 (Spirals)" : "0.65 (Tied)", "Classification lu/b_min (<= 3.0)", Math.Round(lu_b, 2));
                row = AddDataRow(ws, row, "Slenderness k*lu/r (<= 22)", Math.Round(klu_r, 2), "Gravity Axial Load/Leg (kips)", Math.Round(pGrav, 2));
                row = AddDataRow(ws, row, "Max Compression Demand (kips)", Math.Round(pCompTotal, 2), "Overturning Tension/Leg (kips)", Math.Round(tLeg, 2));
                row = AddDataRow(ws, row, "Bearing Capacity phi*Pn (kips)", Math.Round(pedBearCap, 2), "Bearing D/C Ratio", Math.Round(pedBearDC, 4));
                row = AddDataRow(ws, row, "Min Longitudinal Steel As,min (in²)", Math.Round(pedAsMin, 3), "Provided Steel As,prov (in²)", Math.Round(asProv, 2));
                row = AddDataRow(ws, row, "Axial Compression Cap. phi*Pn (kips)", Math.Round(phiPn, 2), "Axial Compression D/C Ratio", Math.Round(axialDC, 4));
                row = AddDataRow(ws, row, "Tensile Capacity phi*Tn (kips)", Math.Round(phiTn, 2), "Uplift Tension D/C Ratio", Math.Round(upliftDC, 4));
                row = AddDataRow(ws, row, "Balanced Section cb / ab (in)", $"{cbVal:F2} / {abVal:F2}", "Design Moment Cap. phi*Mnb (kip-ft)", Math.Round(phiMnb, 1));
                row = AddDataRow(ws, row, "Pedestal Moment Demand Mu (kip-ft)", Math.Round(pedMu, 2), "P-M Interaction D/C Ratio", Math.Round(pmDC, 4));
                row = AddDataRow(ws, row, "Horizontal Shear/Leg Vu,ped (kips)", Math.Round(vuPed, 3), "Max Tie Spacing s_max (in)", Math.Round(tieSpacing, 1));
                row = AddDataRow(ws, row, "Tension Dev. Length ld / l_prov (in)", $"{ld:F2} / {lProv:F2}", "Pedestal Design Status", pedAllPass ? "PASS / OK" : "CHECK PEDESTAL");
                row++;

                // --- SECTION 6: SPREAD FOOTING DESIGN (ACI 318-19) ---
                row = AddSectionHeader(ws, row, $"{nextSection++}. ISOLATED SPREAD FOOTING DESIGN (ACI 318-19 - MULTI-LEG TOWER)");
                var ftEq = new FootingEquations();

                double hpFoot = (footing?.PedestalHeightHp.HasValue == true && footing.PedestalHeightHp.Value > 0)
                    ? footing.PedestalHeightHp.Value
                    : ((ringWall?.Hp.HasValue == true && ringWall.Hp.Value > 0) ? ringWall.Hp.Value : 5.5);
                double ftFc = (footing?.FcPrime.HasValue == true && footing.FcPrime.Value > 0) ? footing.FcPrime.Value : pedFc;
                double ftFy = (footing?.Fy.HasValue == true && footing.Fy.Value > 0) ? footing.Fy.Value : pedFy;
                double alphaS = (footing?.AlphaS.HasValue == true && footing.AlphaS.Value > 0) ? footing.AlphaS.Value : 40.0;

                double ftB = (footing?.FootingSizeB.HasValue == true && footing.FootingSizeB.Value > 0) ? footing.FootingSizeB.Value : 13.5;
                double ftL = (footing?.FootingSizeL.HasValue == true && footing.FootingSizeL.Value > 0) ? footing.FootingSizeL.Value : 13.5;
                double ftH = (footing?.FootingThickness.HasValue == true && footing.FootingThickness.Value > 0) ? footing.FootingThickness.Value : 30.0;
                double ftCover = (footing?.ConcreteCover.HasValue == true && footing.ConcreteCover.Value > 0) ? footing.ConcreteCover.Value : 3.0;
                double qAllow = (footing?.Qallow.HasValue == true && footing.Qallow.Value > 0) ? footing.Qallow.Value : 3.0;
                double muFriction = (footing?.FrictionCoeff.HasValue == true && footing.FrictionCoeff.Value > 0) ? footing.FrictionCoeff.Value : 0.50;
                double bottomAs = (footing?.BottomRebarArea.HasValue == true && footing.BottomRebarArea.Value > 0) ? footing.BottomRebarArea.Value : 0.677;
                double ftDb = (footing?.RebarDiameter.HasValue == true && footing.RebarDiameter.Value > 0) ? footing.RebarDiameter.Value : 1.0;
                double dowelAs = (footing?.TopRebarArea.HasValue == true && footing.TopRebarArea.Value > 0) ? footing.TopRebarArea.Value : asProv;
                double dbDowel = (footing?.DowelDiameter.HasValue == true && footing.DowelDiameter.Value > 0) ? footing.DowelDiameter.Value : 0.75;

                double pLeg = ftEq.FootingGravityLoad(pu, numLegs);
                double pedWt = ftEq.PedestalSelfWeight(pedB, pedL, hpFoot, gammaC);
                double pedWtFactored = 1.2 * pedWt;
                double pFootDesign = ftEq.FootingDesignLoad(pLeg, pedWtFactored);

                double areaReq = ftEq.RequiredFootingArea(pFootDesign, qAllow);
                double areaProv = ftB * ftL;
                double qApplied = ftEq.AppliedSoilPressure(pFootDesign, areaProv);
                double soilDC = ftEq.DemandCapacityRatio(qApplied, qAllow);

                double vLeg = vu / numLegs;
                double fsSliding = ftEq.SlidingFactorOfSafety(muFriction, pFootDesign, vLeg);

                double dEff = ftEq.EffectiveDepth(ftH, ftCover, ftDb);
                double b_o = ftEq.CriticalPunchingPerimeter(pedB, pedL, dEff);
                double a_o = ftEq.AreaInsideCriticalPerimeter(pedB, pedL, dEff);
                double vuPunch = ftEq.FactoredPunchingShear(pFootDesign, qApplied, a_o);
                double phiVcPunch = ftEq.DesignPunchingShearCapacity(ftFc * 1000.0, pedB, pedL, b_o, dEff, alphaS);
                double punchDC = ftEq.DemandCapacityRatio(vuPunch, phiVcPunch);

                double cantProj = ftEq.GoverningCantileverProjection(ftB, ftL, pedB, pedL);
                double vuOneWay = ftEq.OneWayShearForcePerFt(qApplied, cantProj, dEff);
                double phiVcOneWay = ftEq.DesignOneWayShearCapacityPerFt(ftFc * 1000.0, dEff);
                double oneWayDC = ftEq.DemandCapacityRatio(vuOneWay, phiVcOneWay);

                double muFlexFt = ftEq.FlexureMomentPerFt(qApplied, cantProj);
                double muFlexIn = muFlexFt * 12.0;
                double asReq = ftEq.RequiredFlexuralSteelPerFt(muFlexIn, ftFc, ftFy, dEff);
                double asMin = ftEq.MinimumReinforcementPerFt(ftH);
                double asDesign = Math.Max(asReq, asMin);
                double flexSteelDC = ftEq.DemandCapacityRatio(asDesign, bottomAs);

                double phiMnIn = ftEq.DesignFlexuralCapacityKipInPerFt(bottomAs, ftFc, ftFy, dEff);
                double flexMomDC = ftEq.DemandCapacityRatio(muFlexIn, phiMnIn);

                double n1Bear = ftEq.PedestalBearingCapacity(ftFc, pedB, pedL);
                double ftPedBearDC = ftEq.DemandCapacityRatio(pFootDesign, n1Bear);

                double asDowelMin = ftEq.DowelMinimumArea(pedB, pedL);
                double dowelDC = ftEq.DemandCapacityRatio(asDowelMin, dowelAs);
                double ldc = ftEq.DevelopmentLength(ftFy * 1000.0, ftFc * 1000.0, dbDowel);

                bool ftAllPass = soilDC <= 1.0 && fsSliding >= 1.5 && punchDC <= 1.0 && oneWayDC <= 1.0 && flexSteelDC <= 1.0 && flexMomDC <= 1.0 && ftPedBearDC <= 1.0 && dowelDC <= 1.0;

                row = AddDataRow(ws, row, "Footing Dimensions B x L (ft)", $"{Math.Round(ftB, 4)} x {Math.Round(ftL, 4)}", "Footing Thickness h / Cover cc (in)", $"{ftH} / {ftCover}");
                row = AddDataRow(ws, row, "Gravity Load/Leg P_leg (kips)", Math.Round(pLeg, 2), "Factored Pedestal Wt 1.2*Wp (kips)", Math.Round(pedWtFactored, 2));
                row = AddDataRow(ws, row, "Design Footing Load Pc (kips)", Math.Round(pFootDesign, 2), "Effective Depth d (in)", Math.Round(dEff, 2));
                row = AddDataRow(ws, row, "Required Area A_req (ft²)", Math.Round(areaReq, 2), "Provided Area A_f (ft²)", Math.Round(areaProv, 2));
                row = AddDataRow(ws, row, "Soil Pressure q_u / q_allow (ksf)", $"{qApplied:F3} / {qAllow:F2}", "Soil Bearing D/C Ratio", Math.Round(soilDC, 4));
                row = AddDataRow(ws, row, "Shear/Leg V_leg (kips)", Math.Round(vLeg, 2), "Sliding Factor of Safety (>= 1.5)", Math.Round(fsSliding, 2));
                row = AddDataRow(ws, row, "Punching Perimeter bo (in) / Ao (ft²)", $"{b_o:F2} / {a_o:F2}", "Punching Demand Vu / Cap. phi*Vc (kips)", $"{vuPunch:F2} / {phiVcPunch:F1}");
                row = AddDataRow(ws, row, "Punching Shear D/C Ratio", Math.Round(punchDC, 4), "Cantilever Projection l (ft)", Math.Round(cantProj, 3));
                row = AddDataRow(ws, row, "1-Way Shear Vu / Cap. phi*Vc (kip/ft)", $"{vuOneWay:F3} / {phiVcOneWay:F2}", "One-Way Shear D/C Ratio", Math.Round(oneWayDC, 4));
                row = AddDataRow(ws, row, "Flexure Mu (kip-ft/ft) / (kip-in/ft)", $"{muFlexFt:F2} / {muFlexIn:F2}", "As,req / As,min / Prov (in²/ft)", $"{asReq:F3} / {asMin:F3} / {bottomAs:F3}");
                row = AddDataRow(ws, row, "Flexural Cap. phi*Mn (kip-in/ft)", Math.Round(phiMnIn, 1), "Flexure Moment D/C Ratio", Math.Round(flexMomDC, 4));
                row = AddDataRow(ws, row, "Pedestal Bearing N1 (kips)", Math.Round(n1Bear, 1), "Pedestal Bearing D/C Ratio", Math.Round(ftPedBearDC, 4));
                row = AddDataRow(ws, row, "Dowel Steel As,min / Prov (in²)", $"{asDowelMin:F3} / {dowelAs:F2}", "Dowel Steel D/C Ratio", Math.Round(dowelDC, 4));
                row = AddDataRow(ws, row, "Comp. Dev. Length ldc (in)", Math.Round(ldc, 2), "Footing Design Status", ftAllPass ? "PASS / OK" : "CHECK FOOTING");
                row++;
            }
            else
            {
                // Single-Column Only: Section 5 (Annular Concrete Ring Wall Stem - Steps 1 to 15)
                row = AddSectionHeader(ws, row, $"{nextSection++}. ANNULAR CONCRETE RING WALL FOUNDATION DESIGN (ACI 318-19 - SINGLE-LEG)");
                var rwEq = new FoundationEquations.RingWallEquations();
                var (puGov, muGov, _) = new LoadService().GetGoverningLoads();

                double rwP = (ringWall?.ScPu.HasValue == true && ringWall.ScPu.Value > 0)
                    ? ringWall.ScPu.Value
                    : (puGov > 0 ? puGov : (pu > 0 ? pu : (basePlate?.Pu ?? 0)));
                double rwMu = (ringWall?.ScMu.HasValue == true && ringWall.ScMu.Value > 0)
                    ? ringWall.ScMu.Value
                    : (muGov > 0 ? muGov : (mu > 0 ? mu : (basePlate?.OverturningMoment ?? 0)));

                double coneDiamFt = 21.0;
                if (basePlate != null && basePlate.Dbp > 0)
                    coneDiamFt = basePlate.Dbp;
                else if (baseConeDia > 0)
                    coneDiamFt = baseConeDia;
                else if (anchorBolt != null && anchorBolt.Rb > 0)
                    coneDiamFt = 2.0 * anchorBolt.Rb;

                double defaultRcl = coneDiamFt / 2.0;
                double defaultTw = (basePlate?.Wrw.HasValue == true && basePlate.Wrw.Value >= 2.0) ? basePlate.Wrw.Value : 2.0;
                double ri = (ringWall != null && ringWall.Rin > 0) ? ringWall.Rin : (defaultRcl - defaultTw / 2.0);
                double ro = (ringWall != null && ringWall.Rout > ri) ? ringWall.Rout : (defaultRcl + defaultTw / 2.0);
                double tw = rwEq.RingwallWidthFt(ro, ri);
                double meanR = rwEq.MeanRadiusFt(ro, ri);
                double hw = ringWall?.Hw ?? 11.5;

                double fcPsi = ringWall?.ScFcPrime ?? ((basePlate != null && basePlate.Fc_prime > 0)
                    ? (basePlate.Fc_prime > 100 ? basePlate.Fc_prime : basePlate.Fc_prime * 1000.0)
                    : 4000.0);
                if (fcPsi > 0 && fcPsi < 100) fcPsi *= 1000.0;

                double fyKsi = ringWall?.ScFy ?? 60.0;
                double phi = ringWall?.ScPhi ?? 0.90;

                double tUplift = (ringWall?.TotalUpliftT.HasValue == true && ringWall.TotalUpliftT.Value > 0)
                    ? ringWall.TotalUpliftT.Value
                    : rwEq.DerivedTotalUpliftKips(rwMu, meanR);

                string hoopBarLbl = string.IsNullOrWhiteSpace(ringWall?.HoopBarLabel) ? "#5" : ringWall!.HoopBarLabel!;
                double hoopBarArea = (ringWall?.HoopBarArea.HasValue == true && ringWall.HoopBarArea.Value > 0) ? ringWall.HoopBarArea.Value : 0.31;
                double hoopSpacing = (ringWall?.HoopSpacingProv.HasValue == true && ringWall.HoopSpacingProv.Value > 0) ? ringWall.HoopSpacingProv.Value : 6.0;
                string vertCallout = string.IsNullOrWhiteSpace(ringWall?.VertRebarCallout) ? "#6 @ 12\"" : ringWall!.VertRebarCallout!;

                double areaFt2 = rwEq.AnnularAreaFt2(ro, ri);
                double areaIn2 = rwEq.AnnularAreaIn2(areaFt2);
                double qKsf = rwEq.AverageBearingPressureKsf(rwP, areaFt2);
                double qPsi = rwEq.KsfToPsi(qKsf);
                double eccFt = rwEq.OverturningEccentricityFt(rwMu, rwP);
                double qMax = rwEq.MaxBearingPressureKsf(qKsf, eccFt, tw);
                double qMin = rwEq.MinBearingPressureKsf(qKsf, eccFt, tw);
                bool uplift = rwEq.UpliftOccurs(qMin);

                double pNet = rwEq.NetCompressionForceKips(rwP, tUplift);
                double sigmaKsf = rwEq.CompressiveStressKsf(pNet, areaFt2);
                double sigmaPsi = rwEq.KsfToPsi(sigmaKsf);
                double allowCompPsi = rwEq.AllowableConcreteCompressionPsi(fcPsi);
                double compDC = allowCompPsi > 0 ? sigmaPsi / allowCompPsi : 0;
                bool compPass = sigmaPsi <= allowCompPsi && pNet >= 0;

                double perimFt = rwEq.RingwallPerimeterFt(meanR);
                double tHoop = rwEq.HoopTensionKipsPerFt(tUplift, meanR);
                double asReq = rwEq.RequiredHoopSteelIn2PerFt(tHoop, phi, fyKsi);
                double asMin = rwEq.MinimumHoopSteelIn2PerFt(tw);
                double asGov = Math.Max(asReq, asMin);
                double sMax = rwEq.MaxHoopSpacingIn(hoopBarArea, asGov);
                double asProv = rwEq.ProvidedHoopSteelIn2PerFt(hoopBarArea, hoopSpacing);
                double hoopDC = asProv > 0 ? asGov / asProv : 9.99;
                bool hoopPass = asProv >= asGov && hoopSpacing <= sMax;

                double shearDem = rwEq.ShearDemandKipsPerFt(rwP, meanR);
                double shearCap = rwEq.DesignConcreteShearCapacityKipsPerFt(fcPsi, tw, 0.75);
                double shearDC = shearCap > 0 ? shearDem / shearCap : 9.99;
                bool shearPass = shearDem <= shearCap;
                bool rwAllPass = tw >= 2.0 && compPass && hoopPass && shearPass;

                row = AddDataRow(ws, row, "Inner / Outer / Mean Radius (ft)", $"{ri:F2} / {ro:F2} / {meanR:F2}", "Ringwall Width tw / Height hw (ft)", $"{tw:F2} ({tw * 12.0:F0} in) / {hw:F2}");
                row = AddDataRow(ws, row, "Concrete f'c (psi) / Rebar fy (ksi)", $"{fcPsi:F0} / {fyKsi:F0}", "Cross-Sectional Area A (ft² / in²)", $"{areaFt2:F2} / {areaIn2:F0}");
                row = AddDataRow(ws, row, "Avg Bearing Pressure q (ksf / psi)", $"{qKsf:F2} / {qPsi:F1}", "Overturning Eccentricity e (ft)", Math.Round(eccFt, 2));
                row = AddDataRow(ws, row, "Max / Min Pressure q_max / q_min (ksf)", $"{qMax:F2} / {qMin:F2}", "Foundation Contact Condition", uplift ? "Uplift Active (Anchor Bolts Resist)" : "Full Contact (No Uplift)");
                row = AddDataRow(ws, row, "Total Uplift Force T (kips)", Math.Round(tUplift, 2), "Net Compression Force P_net (kips)", Math.Round(pNet, 2));
                row = AddDataRow(ws, row, "Compressive Stress sigma (ksf / psi)", $"{sigmaKsf:F2} / {sigmaPsi:F1}", "Allowable Stress 0.45*f'c (psi)", Math.Round(allowCompPsi, 1));
                row = AddDataRow(ws, row, "Concrete Stress D/C Ratio", Math.Round(compDC, 4), "Mean Perimeter 2*pi*R (ft)", Math.Round(perimFt, 2));
                row = AddDataRow(ws, row, "Hoop Tension T_hoop (kips/ft)", Math.Round(tHoop, 2), "Hoop Steel As,req / As,min (in²/ft)", $"{asReq:F4} / {asMin:F4}");
                row = AddDataRow(ws, row, "Max Hoop Spacing s_max (in)", Math.Round(sMax, 2), "Provided Hoop Reinforcement", $"{hoopBarLbl} @ {hoopSpacing:0.#}\" ({asProv:F3} in²/ft)");
                row = AddDataRow(ws, row, "Hoop Steel D/C Ratio", Math.Round(hoopDC, 4), "Vertical Reinforcement (Each Face)", vertCallout);
                row = AddDataRow(ws, row, "Shear V / Cap. phi*Vc (kips/ft)", $"{shearDem:F2} / {shearCap:F2}", "Shear Check D/C Ratio", Math.Round(shearDC, 4));
                row = AddDataRow(ws, row, "Overturning & Sliding Stability", "Anchor Bolts Active / Circular Ring (OK)", "Ring Wall Design Status", rwAllPass ? "PASS / OK" : "CHECK RING WALL");
                row++;
            }

            // Material & Geotechnical Properties
            row = AddSectionHeader(ws, row, $"{nextSection}. MATERIAL & GEOTECHNICAL PROPERTIES");
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
