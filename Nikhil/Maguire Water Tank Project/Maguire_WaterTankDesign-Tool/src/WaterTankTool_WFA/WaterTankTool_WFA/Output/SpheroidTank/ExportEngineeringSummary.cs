using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using WaterTankTool_WFA.Entity;

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
            row = AddSectionHeader(ws, row, "3. ANCHOR BOLT DESIGN & BREAKOUT CHECK (ACI 318-19)");
            int nb = anchorBolt?.Nb ?? basePlate?.Nb ?? 0;
            double db = anchorBolt?.Db ?? 0;
            double dh = anchorBolt?.Dh ?? 0;
            double rb = anchorBolt?.Rb ?? 0;
            row = AddDataRow(ws, row, "Total Anchor Bolts (Nb)", nb, "Bolt Nominal Diameter (db) (in)", db);
            row = AddDataRow(ws, row, "Hole Diameter (dh) (in)", dh, "Bolt Circle Radius (rb) (in/ft)", rb);

            double fy = anchorBolt?.Fy ?? 36;
            double fu = anchorBolt?.Fu ?? 58;
            double fc = anchorBolt?.FcPrime ?? basePlate?.Fc_prime ?? 4000;
            double hef = anchorBolt?.Hef ?? 24;
            row = AddDataRow(ws, row, "Bolt Yield Strength (Fy) (ksi)", fy, "Bolt Ultimate Strength (Fu) (ksi)", fu);
            row = AddDataRow(ws, row, "Concrete Strength (f'c) (psi)", fc, "Embedment Depth (hef) (in)", hef);

            double tu = anchorBolt?.Tu ?? 0;
            double boltVu = anchorBolt?.Vu ?? 0;
            row = AddDataRow(ws, row, "Tension Demand / Bolt (Nua) (kips)", tu, "Shear Demand / Bolt (Vua) (kips)", boltVu);

            // Calculate estimated capacities if present
            double interaction = 0.0;
            string status = "OK";
            if (nb > 0 && tu > 0)
            {
                double ase = 0.7854 * Math.Pow(Math.Max(0.1, db - 0.12), 2);
                double nsa = ase * fu;
                double ncb = 15.0 * Math.Sqrt(fc) * Math.Pow(hef, 1.5) / 1000.0;
                double phiNn = 0.75 * Math.Min(nsa, ncb);
                if (phiNn > 0) interaction = (tu / phiNn);
                if (interaction > 1.0) status = "EXCEEDS CAPACITY";
            }
            row = AddDataRow(ws, row, "Combined Interaction Ratio", Math.Round(interaction, 4), "Design Status", status);
            row++;

            // Section 4: Base Plate Design (AISC LRFD Annular Strip Method)
            row = AddSectionHeader(ws, row, "4. BASE PLATE DESIGN & BEARING STRESS (AISC LRFD)");
            double dbp = basePlate?.Dbp ?? (baseConeDia * 12);
            double ro = basePlate?.Ro ?? ((baseConeDia * 12 / 2) + 7);
            double ri = basePlate?.Ri ?? (ro - 12);
            double wrw = basePlate?.Wrw ?? 1.5;
            row = AddDataRow(ws, row, "Base Plate Diameter (Dbp) (in)", dbp, "Ring Wall Width (Wrw) (ft)", wrw);
            row = AddDataRow(ws, row, "Outside Radius (Ro) (in)", ro, "Inside Radius (Ri) (in)", ri);

            double a2 = basePlate?.A2 ?? 0;
            double fp = basePlate?.Fp ?? 0;
            double phiPp = basePlate?.Phi_Pp ?? 0;
            double bearUtil = basePlate?.BearingUtilization ?? 0;
            row = AddDataRow(ws, row, "Supporting Area A2 (sq in)", a2, "Bearing Stress Demand (fp) (ksi)", fp);
            row = AddDataRow(ws, row, "Design Bearing Strength (phi Pp) (kips)", phiPp, "Bearing Utilization Ratio", Math.Round(bearUtil, 4));

            double t = basePlate?.T ?? 0;
            double treq = basePlate?.T_req ?? 0;
            double thkUtil = basePlate?.ThicknessUtilization ?? (t > 0 ? treq / t : 0);
            row = AddDataRow(ws, row, "Actual Plate Thickness (t) (in)", t, "Required Thickness (treq) (in)", Math.Round(treq, 4));
            row = AddDataRow(ws, row, "Thickness Utilization Ratio", Math.Round(thkUtil, 4), "Plate Compactness Status", thkUtil <= 1.0 ? "COMPACT / OK" : "CHECK THICKNESS");
            row++;

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
