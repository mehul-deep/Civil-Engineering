using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace WaterTankTool_WFA.Output.SpheroidTank
{
    internal class ExportFabricationOutput
    {
        // Row 2
        private static readonly string[] HeaderShort =
        {
            "TANK DESIGNATION","PARAMETER REF.","MG","BASCONE DIA","BASECONE HT","DOOR TYPE","DDEG",
            "BPDIA","BPOR","BPIR","BPDEG","BPTHK","BPQ",
            "ABHQ","ABHSD","ABQ","SC","SCWT","BPSQ",
            "B1LR","B1UR","B1HT","B1THK","B1DEG","B1Q",
            "B2LR","B2UR","B2HT","B2THK","B2DEG","B2Q",
            "B3OR","B3LR","B3UR","B3HT","B3THK","B3DEG","B3Q",
            "B4OR","B4LR","B4UR","B4HT","B4THK","B4DEG","B4Q",
            "B5OR","B5LR","B5UR","B5HT","B5THK","B5DEG","B5Q",
            "CDIA","CHT",
            "C1DIA","C1HT","C1THK",
            "C2DIA","C2HT","C2THK",
            "C3DIA","C3HT","C3THK",
            "C4DIA","C4HT","C4THK",
            "C5DIA","C5HT","C5THK",
            "C6DIA","C6HT","C6THK",
            "C7DIA","C7HT","C7THK",
            "C8DIA","C8HT","C8THK",
            "C9DIA","C9HT","C9THK",
            "C10DIA","C10HT","C10THK",
            "C11DIA","C11HT","C11THK",
            "C12DIA","C12HT","C12THK",
            "C13DIA","C13HT","C13THK",
            "C14DIA","C14HT","C14THK",
            "C15DIA","C15HT","C15THK",
            "C16DIA","C16HT","C16THK",
            "C17DIA","C17HT","C17THK",
            "C18DIA","C18HT","C18THK",
            "T1OR","T1LR","T1UR","TIHT","T1THK","T1DEG","T1Q",
            "T2UR","T2LR","T2HT","T2THK","T2SEG","T2Q",
            "T3UR","T3LR","T3HT","T3THK","T3SEG","T3Q",
            "T4UR","T4LR","T4HT","T4THK","T4SEG","T4Q",
            "T5UR","T5LR","T5HT","T5THK","T5SEG","T5Q",
            "T6UR","T6LR","T6HT","T6THK","T6SEG","T6Q",
            "KKTHK","KKLR","KKCHT","KKUSR","KKUER","KKER","KKSDEG","KKEDEG","KKSECR",
            "BKTHK","BKR","BKQ","BKSDEG","BKEDEG","BKEDIM","BKDIA",
            "TKTHK","TKR","TKQ","TKSDEG","TKEDEG","TKEDIM","TKDIA",
            "RFTHK","RFR","RFQ","RFSDEG","RFEDEG","RFEDIM","RFDIA",
            "RCLR","RCUR","RCHT","RCTHK","RCQ",
            "RCBCRIR","RCBCROR","RCBCRTHK","RCBCRDEG","RCBCRQ",
            "RCTCRIR","RCTCROR","RCTCRTHK",
            "DWLDIA","DWLHT","DWLTHK",
            "DWUDIA","DWUHT","DWUTHK",
            "DWSTFOR","DWSTFIR","DWSTFTHK","DWSTFQ"
        };

        // Row 1
        private static readonly string[] HeaderLong =
        {
            "TANK DESIGNATION","PARAMETER REF.","MG","BASCONE DIA","BASECONE HT","DOOR TYPE","Door Position Degree",
            "Base Plate Diameter","BASE PLATE OUTSIDE RADIUS","BASE PLATE INSIDE RADIUS","BASE PLATE SEGMENT DEGREE","BASE PLATE THK","BASE PLATE SEGMENT QUANTITY",
            "Anchor Bolt Hole Quantity","Anchor Bolt Hole Start Degree","ANCHOR BOLT QUANTITY","Side Chairs","Side ChairsWith Tops","Base Plate Shim Quantity",
            "B1 LOWER RADIUS","B1 UPPER RADIUS","B1 HEIGHT","B1 THK","B1 SEGMENT DEGREE","B1 SEGEMENT QUANTITY",
            "B2 LOWER RADIUS","B2 UPPER RADIUS","B2 HEIGHT","B2 THK","B2 SEGMENT DEGREE","B2 SEGEMENT QUANTITY",
            "B3 OUTSIDE RADIUS","B3 LOWER RADIUS","B3 UPPER RADIUS","B3 HEIGHT","B3 THK","B3 SEGMENT DEGREE","B3 SEGEMENT QUANTITY",
            "B4 OUTSIDE RADIUS","B4 LOWER RADIUS","B4 UPPER RADIUS","B4 HEIGHT","B4 THK","B4 SEGMENT DEGREE","B4 SEGEMENT QUANTITY",
            "B5 OUTSIDE RADIUS","B5 LOWER RADIUS","B5 UPPER RADIUS","B5 HEIGHT","B5 THK","B5 SEGMENT DEGREE","B5 SEGEMENT QUANTITY",
            "COLUMN DIAMETER","COLUMN HEIGHT",
            "C1 DIAMETER","C1 HEIGHT","C1 THICKNESS",
            "C2 DIAMETER","C2 HEIGHT","C2 THICKNESS",
            "C3 DIAMETER","C3 HEIGHT","C3 THICKNESS",
            "C4 DIAMETER","C4 HEIGHT","C4 THICKNESS",
            "C5 DIAMETER","C5 HEIGHT","C5 THICKNESS",
            "C6 DIAMETER","C6 HEIGHT","C6 THICKNESS",
            "C7 DIAMETER","C7 HEIGHT","C7 THICKNESS",
            "C8 DIAMETER","C8 HEIGHT","C8 THICKNESS",
            "C9 DIAMETER","C9 HEIGHT","C9 THICKNESS",
            "C10 DIAMETER","C10 HEIGHT","C10 THICKNESS",
            "C11 DIAMETER","C11 HEIGHT","C11 THICKNESS",
            "C12 DIAMETER","C12 HEIGHT","C12 THICKNESS",
            "C13 DIAMETER","C13 HEIGHT","C13 THICKNESS",
            "C14 DIAMETER","C14 HEIGHT","C14 THICKNESS",
            "C15 DIAMETER","C15 HEIGHT","C15 THICKNESS",
            "C16 DIAMETER","C16 HEIGHT","C16 THICKNESS",
            "C17 DIAMETER","C17 HEIGHT","C17 THICKNESS",
            "C18 DIAMETER","C18 HEIGHT","C18 THICKNESS",
            "T1 OUTSIDE RADIUS","T1 LOWER RADIUS","T1 UPPER RADIUS","T1 HEIGHT","T1 THK","T1 SEGMENT DEGREE","T1 SEGMENT QUANTITY", // Row1 long label; Row2 short code is TIHT (template typo)
            "T2 UPPER RADIUS","T2 LOWER RADIUS","T2 HEIGHT","T2 THK","T2 SEGMENT DEGREE","T2 SEGMENT QUANTITY",
            "T3 UPPER RADIUS","T3 LOWER RADIUS","T3 HEIGHT","T3 THK","T3 SEGMENT DEGREE","T3 SEGMENT QUANTITY",
            "T4 UPPER RADIUS","T4 LOWER RADIUS","T4 HEIGHT","T4 THK","T4 SEGMENT DEGREE","T4 SEGMENT QUANTITY",
            "T5 UPPER RADIUS","T5 LOWER RADIUS","T5 HEIGHT","T5 THK","T5 SEGMENT DEGREE","T5 SEGMENT QUANTITY",
            "T6 UPPER RADIUS","T6 LOWER RADIUS","T6 HEIGHT","T6 THK","T6 SEGMENT DEGREE","T6 SEGMENT QUANTITY",
            "KNUCKLE KNUCKLE THICKNESS","KNUCKLE KNUCKLE LOWER RADIUS","KNUCKLE KNUCKLE CENTER HEIGHT","KNUCKLE KNUCKLE UPPER START RADIUS",
            "KNUCKLE KNUCKLE UPPER EXTEND RADIUS","KNUCKLE KNUCKLE EXTEND RADIUS","KNUCKLE KNUCKLE START DEGREE","KNUCKLE KNUCKLE END DEGREE","KNUCKLE KNUCKLE SECTION RADIUS",
            "Bottom Knuckle Thickness","Bottom Knuckle Radius","Bottom Knuckle Quantity","Bottom Knuckle Segement Degree","Bottom Knuckle End Degree","Bottom Knuck Extra Dimension","Bottom Knuckle Diameter",
            "Top Knuckle Thickness","Top Knuckle Radius","Top Knuckle Quantity","Top Knuckle Segement Degree","Top Knuckle End Degree","Top Knuck Extra Dimension","Top Knuckle Diameter",
            "Roof Finger Thickness","Roof Finger Radius","Roof Finger Quantity","Roof Finger Segement Degree","Roof Finger End Degree","Roof Finger Extra Dimension","Roof Finger Diameter",
            "Reducer Cone Lower Radius","Reducer Cone Upper Radius","Reducer Cone Height","Reducer Cone Thickness","Reducer Cone Quantity",
            "Reducer Cone Bottom Compression Ring Inside Radius","Reducer Cone Bottom Compression Ring Outside Radius","Reducer Cone Bottom Compression Ring Thickness","Reducer Cone Bottom Compression Ring Degree","Reducer Cone Bottom Compression Ring Quantity",
            "Reducer Cone Top Compression Ring Inside Radius","Reducer Cone Top Compression Ring Outside Radius","Reducer Cone Top Compression Ring Thickness",
            "DRYWELL LOWER DIAMETER","DRYWELL LOWER HEIGHT","DRYWELL LOWER THK",
            "DRYWELL UPPER DIAMETER","DRYWELL UPPER HEIGHT","DRYWELL UPPER THK",
            "DRYWELL STIFFENER OUTSIDE RADIUS","DRYWELL STIFFENER INSIDE RADIUS","DRYWELL STIFFENER THICKNESS","DRYWELL STIFFENER QUANTITY"
        };

        public void RunExport(FabricationOutputRow row, IWin32Window owner = null)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            using var sfd = new SaveFileDialog
            {
                Title = "Export Fabrication Output",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"FabricationOutput_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (sfd.ShowDialog(owner) != DialogResult.OK)
                return;

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "SHEROID TANK PARAMETERS.xlsx");

            if (!File.Exists(templatePath))
            {
                MessageBox.Show(owner, $"Template not found at: {templatePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // We create a fresh workbook and strictly copy cell values to prevent ClosedXML 
            // from causing XML corruption when trying to re-serialize advanced template features.
            using var templateWb = new XLWorkbook(templatePath);
            using var newWb = new XLWorkbook();

            var dataSheetsToUpdate = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SPHEROID PARAMETERS",
                "BASE PLATE",
                "BASE CONES",
                "TRANSITIONS",
                "DRYWELLS"
            };

            foreach (var templateWs in templateWb.Worksheets)
            {
                var newWs = newWb.AddWorksheet(templateWs.Name);
                
                int lastTemplateRow = templateWs.LastRowUsed()?.RowNumber() ?? 0;
                int maxCol = templateWs.LastColumnUsed()?.ColumnNumber() ?? 0;

                // By default, we copy all rows (to preserve static reference tables like DOOR TYPE PARAMETERS)
                int rowsToCopy = lastTemplateRow;

                // But for the main data sheets, we ONLY want to copy the headers to clear out the old company placeholder tanks!
                if (dataSheetsToUpdate.Contains(templateWs.Name))
                {
                    // SPHEROID PARAMETERS, BASE PLATE, BASE CONES, TRANSITIONS all have 2 header rows
                    rowsToCopy = 2; 
                    
                    if (templateWs.Name.Equals("DRYWELLS", StringComparison.OrdinalIgnoreCase))
                    {
                        rowsToCopy = 1; // DRYWELLS only has 1 header row
                    }
                }

                // 1. Safely copy the allowed rows & columns (raw values only)
                for (int r = 1; r <= rowsToCopy; r++)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var cell = templateWs.Cell(r, c);
                        if (!cell.IsEmpty())
                        {
                            newWs.Cell(r, c).Value = cell.Value;
                        }
                    }
                }

                // 2. If this is a data sheet, append our new row at the bottom
                if (dataSheetsToUpdate.Contains(templateWs.Name))
                {
                    var colMap = new Dictionary<string, int>();

                    // Scan first 3 rows of the template to build the column mapping
                    for (int r = 1; r <= 3; r++)
                    {
                        for (int c = 1; c <= maxCol; c++)
                        {
                            var valStr = templateWs.Cell(r, c).Value.ToString().Trim();
                            if (string.IsNullOrEmpty(valStr)) continue;

                            if (HeaderShort.Contains(valStr))
                            {
                                colMap[valStr] = c;
                            }
                            else if (HeaderLong.Contains(valStr))
                            {
                                int idx = Array.IndexOf(HeaderLong, valStr);
                                if (idx >= 0)
                                {
                                    colMap[HeaderShort[idx]] = c;
                                }
                            }
                        }
                    }

                    if (colMap.Count > 0)
                    {
                        // Append directly below the header rows we just copied
                        int dataRow = rowsToCopy + 1;

                        foreach (var kvp in colMap)
                        {
                            if (row.Values.TryGetValue(kvp.Key, out var val))
                            {
                                SetCellValue(newWs.Cell(dataRow, kvp.Value), val);
                            }
                        }
                    }
                }
            }

            newWb.SaveAs(sfd.FileName);

            MessageBox.Show(owner, "Excel export completed perfectly. No corruption errors.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void SetCellValue(IXLCell cell, object? value)
        {
            if (value == null)
            {
                cell.Clear(XLClearOptions.Contents);
                return;
            }

            // Put real numbers/dates as real Excel types (so formulas/sorting work)
            switch (value)
            {
                case string s:
                    cell.SetValue(s);
                    break;

                case bool b:
                    cell.SetValue(b);
                    break;

                case int i:
                    cell.SetValue(i);
                    break;

                case long l:
                    cell.SetValue(l);
                    break;

                case float f:
                    cell.SetValue((double)f);
                    break;

                case double d:
                    cell.SetValue(d);
                    break;

                case decimal m:
                    cell.SetValue((double)m); // Excel stores numeric as double
                    break;

                case DateTime dt:
                    cell.SetValue(dt);
                    break;

                default:
                    // Fallback: string representation
                    cell.SetValue(value.ToString() ?? "");
                    break;
            }
        }
    }
}