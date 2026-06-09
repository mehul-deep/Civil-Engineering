using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // Path
using WaterTankTool_WFA.Constants;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;
using WaterTankTool_WFA.Tanks;

namespace WaterTankTool_WFA
{
    public partial class SegmentDialogBox : Form
    {
        private WaterTankDbContext _context;
        private WaterTank _waterTankForm;
        public String SegmentName { get; set; }
        public String SegmentType { get; set; }
        public double Diameter { get; set; }
        public double Thickness { get; set; }
        public double HeightInitial { get; set; }
        public double HeightFinal { get; set; }
        public double AverageHeight { get; set; }

        private string _segmentType;
        private int _segmentNumber;
        private string _dialogType;
        private double Ag;
        private double height;
        private TankType _tankType;
        private int _noOfCols;
        private List<object>? _singleColumnItems;

        TankData tankData = new TankData();
        TankDataDimensions dimensions = new TankDataDimensions();
        String _selectedTankCapacity;

        // ─────────────────────────────────────────────────────────────
        // Continuity helpers
        // ─────────────────────────────────────────────────────────────
        private const double HEIGHT_TOL = 1e-3;

        private static bool IsClose(double a, double b, double tol = HEIGHT_TOL)
            => Math.Abs(a - b) <= tol;

        // For multileg names like "Cyl_1", "Cyl_2" → returns "Cyl"
        private static string GetBaseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name ?? "";
            int idx = name.LastIndexOf('_');
            if (idx > 0 && int.TryParse(name[(idx + 1)..], out _))
                return name[..idx];
            return name;
        }

        /// <summary>
        /// Previous by height: max HeightFinal <= refInitial + tol
        /// SingleColumn: across Base/Cylinder/Riser/Tanks.
        /// MultiColumn : within the same type (and optionally exclude current multileg baseName).
        /// If none exists, returns null.
        /// </summary>
        private double? GetPrevFinalHeight(
            TankType tankType,
            string segmentType,
            double refInitial,
            string? excludeBaseNameIfAny = null)
        {
            IQueryable<SegmentProperties> q = _context.SegmentProperties.AsQueryable();

            if (tankType == TankType.SingleColumn)
            {
                // across all chainable types INCLUDING Tanks
                q = q.Where(s => s.SegmentType == "Base" ||
                                 s.SegmentType == "Cylinder" ||
                                 s.SegmentType == "Riser" ||
                                 s.SegmentType == "Tanks");
            }
            else
            {
                // MultiColumn → chain within the same type
                q = q.Where(s => s.SegmentType == segmentType);

                if (!string.IsNullOrWhiteSpace(excludeBaseNameIfAny))
                {
                    q = q.Where(s => !(s.SegmentName == excludeBaseNameIfAny ||
                                       s.SegmentName.StartsWith(excludeBaseNameIfAny + "_")));
                }
            }

            return q.Where(s => s.HeightFinal <= refInitial + HEIGHT_TOL)
                    .OrderByDescending(s => s.HeightFinal)
                    .Select(s => (double?)s.HeightFinal)
                    .FirstOrDefault();
        }

        /// <summary>
        /// Next by height: min HeightInitial >= refFinal - tol
        /// SingleColumn: across Base/Cylinder/Riser/Tanks.
        /// MultiColumn : within the same type (and optionally exclude current multileg baseName).
        /// If none exists, returns null.
        /// </summary>
        private (double? nextInit, string nextName)? GetNextInitialHeight(
            TankType tankType,
            string segmentType,
            double refFinal,
            string? excludeBaseNameIfAny = null)
        {
            IQueryable<SegmentProperties> q = _context.SegmentProperties.AsQueryable();

            if (tankType == TankType.SingleColumn)
            {
                q = q.Where(s => s.SegmentType == "Base" ||
                                 s.SegmentType == "Cylinder" ||
                                 s.SegmentType == "Riser" ||
                                 s.SegmentType == "Tanks");
            }
            else
            {
                q = q.Where(s => s.SegmentType == segmentType);
            }

            if (!string.IsNullOrWhiteSpace(excludeBaseNameIfAny))
            {
                q = q.Where(s => !(s.SegmentName == excludeBaseNameIfAny ||
                                   s.SegmentName.StartsWith(excludeBaseNameIfAny + "_")));
            }

            var next = q.Where(s => s.HeightInitial >= refFinal - HEIGHT_TOL)
                        .OrderBy(s => s.HeightInitial)
                        .Select(s => new { s.HeightInitial, s.SegmentName })
                        .FirstOrDefault();

            return next is null ? (null, string.Empty) : (next.HeightInitial, next.SegmentName);
        }

        /// <summary>
        /// Backward continuity: compare NEW initial to the PREVIOUS final.
        /// If anchorInitial is provided (Modify), neighbor search is anchored to the seed’s current height.
        /// </summary>
        private bool EnsureHeightContinuityOrWarn(
            string segmentType,
            double newHeightInitial,
            string? baseNameForMultileg = null,
            bool isModify = false,
            double? anchorInitial = null)
        {
            string? exclude = (_tankType == TankType.MultiColumn && isModify) ? baseNameForMultileg : null;

            // Use seed’s current initial when modifying; otherwise use the new initial
            double refInitial = anchorInitial ?? newHeightInitial;

            double? requiredPrevFinal = GetPrevFinalHeight(_tankType, segmentType, refInitial, exclude);

            if (requiredPrevFinal is null) return true; // no previous

            if (!IsClose(newHeightInitial, requiredPrevFinal.Value))
            {
                string scope = _tankType == TankType.SingleColumn ? "the previous segment"
                                                                  : $"the previous {segmentType.ToLower()}";
                MessageBox.Show(
                    $"Height continuity check (backward) failed.\n\n" +
                    $"New HeightInitial = {newHeightInitial:F4}\n" +
                    $"must match HeightFinal = {requiredPrevFinal.Value:F4} of {scope}.",
                    "Height Continuity",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Forward continuity: compare NEW final to the NEXT initial.
        /// If anchorFinal is provided (Modify), neighbor search is anchored to the seed’s current final.
        /// </summary>
        private bool EnsureForwardContinuityOrWarn(
            string segmentType,
            double newHeightFinal,
            string? baseNameForMultileg = null,
            bool isModify = false,
            double? anchorFinal = null)
        {
            string? exclude = (_tankType == TankType.MultiColumn && isModify) ? baseNameForMultileg : null;

            // Use seed’s current final when modifying; otherwise use the new final
            double refFinal = anchorFinal ?? newHeightFinal;

            var next = GetNextInitialHeight(_tankType, segmentType, refFinal, exclude);
            if (next is null || next.Value.nextInit is null) return true; // no next

            double nextInit = next.Value.nextInit.Value;
            string nextName = next.Value.nextName ?? "(unknown)";

            if (!IsClose(newHeightFinal, nextInit))
            {
                string scope = _tankType == TankType.SingleColumn ? "the next segment"
                                                                  : $"the next {segmentType.ToLower()}";
                MessageBox.Show(
                    $"Height continuity check (forward) failed.\n\n" +
                    $"New HeightFinal = {newHeightFinal:F4}\n" +
                    $"must match {scope}'s HeightInitial = {nextInit:F4} (segment: {nextName}).",
                    "Height Continuity",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return false;
            }
            return true;
        }

        public SegmentDialogBox()
        {
            Ag = 0;
            height = 0;
            InitializeComponent();
            GetTanksJsonData();
            maskedTextBox1.TextChanged += InputFields_TextChanged;
            maskedTextBox2.TextChanged += InputFields_TextChanged;
            maskedTextBox3.TextChanged += InputFields_TextChanged;
            maskedTextBox4.TextChanged += InputFields_TextChanged;
            maskedTextBox5.TextChanged += InputFields_TextChanged;
            this.FormClosing += SegmentDialogBox_FormClosing;
        }

        //public SegmentDialogBox(string segmentType, WaterTank waterTankForm)
        //{
        //    _segmentType = segmentType;
        //    _waterTankForm = waterTankForm;
        //    InitializeComponent();
        //    GetTanksJsonData();
        //    var context = WaterTankDbContext.GetInstance();
        //    _context = context;
        //    showInputFieldsOnType();
        //    this.FormClosing += SegmentDialogBox_FormClosing;
        //    comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        //}

        public SegmentDialogBox(string segmentType, WaterTank waterTankForm)
        {
            _segmentType = segmentType;
            _waterTankForm = waterTankForm;
            _tankType = AppState.CurrentTankType;
            _noOfCols = AppState.NoOfColumns;
            InitializeComponent();

            _singleColumnItems = comboBox1.Items.Cast<object>().ToList();
            GetTanksJsonData();
            var context = WaterTankDbContext.GetInstance();
            _context = context;

            showInputFieldsOnType();
            FillTankCombo();
            this.FormClosing += SegmentDialogBox_FormClosing;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        public SegmentDialogBox(string segmentType, WaterTank waterTankForm, string formTitle)
        {
            _segmentType = segmentType;
            _waterTankForm = waterTankForm;
            _tankType = AppState.CurrentTankType;
            _noOfCols = AppState.NoOfColumns;
            InitializeComponent();
            this.Text = formTitle;

            _singleColumnItems = comboBox1.Items.Cast<object>().ToList();
            GetTanksJsonData();
            var context = WaterTankDbContext.GetInstance();
            _context = context;

            showInputFieldsOnType();
            FillTankCombo();
            this.FormClosing += SegmentDialogBox_FormClosing;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        public SegmentDialogBox(int segmentNumber, string dialogType, WaterTank waterTankForm)
        {
            _dialogType = dialogType;
            _segmentNumber = segmentNumber;
            _tankType = AppState.CurrentTankType;
            _noOfCols = AppState.NoOfColumns;

            InitializeComponent();
            GetTanksJsonData();
            var context = WaterTankDbContext.GetInstance();
            _context = context;
            _waterTankForm = waterTankForm;

            showInputFieldsOnType();
            FillTankCombo();
            ModifyDialogBox();

            this.FormClosing += SegmentDialogBox_FormClosing;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        private void FillTankCombo()
        {
            if (_segmentType != "Tanks" || comboBox1 == null) return;   // dropdown is irrelevant

            comboBox1.BeginUpdate();
            comboBox1.Items.Clear();

            if (_tankType == TankType.SingleColumn)
            {
                if (_singleColumnItems != null)
                    comboBox1.Items.AddRange(_singleColumnItems.ToArray());
            }
            else   // Multi-column
            {
                if (tankData?.Tanks != null)
                {
                    var names = tankData.Tanks.Select(t => (object)t.Type).ToArray();
                    comboBox1.Items.AddRange(names);
                }
            }

            if (comboBox1.Items.Count > 0 && comboBox1.SelectedIndex == -1)
                comboBox1.SelectedIndex = 0;

            comboBox1.EndUpdate();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1?.SelectedItem == null)
                return;

            string selectedTank = comboBox1.SelectedItem.ToString() ?? "";
            _selectedTankCapacity = selectedTank;

            if (tankData?.Tanks != null)
            {
                dimensions = tankData.Tanks.FirstOrDefault(data => data.Type == selectedTank);
                if (dimensions != null)
                {
                    maskedTextBox2.Text = ExtractNumericValue(dimensions.Diameter);
                    maskedTextBox3.Text = ExtractNumericValue(dimensions.Thickness);
                    DoCalculations();
                }
                else
                {
                    Console.WriteLine("No matching tank found for selection.");
                }
            }
            else
            {
                Console.WriteLine("Tank data is not available.");
            }
        }

        public void showInputFieldsOnType()
        {
            if (_segmentType == "Base")
            {
                comboBox1.Visible = false;
                label3.Text = "Top Diameter";
                label4.Text = "Bottom Diameter";
                label17.Text = "ft";
                label25.Visible = true;
                maskedTextBox5.Visible = true;
                label26.Visible = true;
            }
            else if (_segmentType == "Cylinder" || _segmentType == "Riser")
            {
                comboBox1.Visible = false;
                label3.Text = "Diameter";
                label17.Text = "in";
                label25.Visible = false;
                maskedTextBox5.Visible = false;
                label26.Visible = false;
            }
            else if (_segmentType == "Tanks")
            {
                comboBox1.Visible = true;
                label3.Text = "Diameter";
                label17.Text = "in";
                label25.Visible = false;
                maskedTextBox5.Visible = false;
                label26.Visible = false;
                maskedTextBox3.ReadOnly = true;
                maskedTextBox4.ReadOnly = true;
                maskedTextBox2.ReadOnly = true;
            }
        }

        private void InputFields_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(maskedTextBox1.Text) && _selectedTankCapacity != null)
            {
                if (dimensions != null &&
                    double.TryParse(maskedTextBox1.Text, out double h1) &&
                    double.TryParse(ExtractNumericValue(dimensions.Height), out double h2))
                {
                    maskedTextBox4.Text = (h1 + h2).ToString("F4");
                }
            }
            DoCalculations();
        }

        private void ModifyDialogBox()
        {
            var segmentProperties = _context.SegmentProperties.FirstOrDefault(item => item.SegmentNumber == _segmentNumber);

            if (segmentProperties != null && _dialogType == "Modify")
            {
                if (segmentProperties.SegmentType == "Base")
                {
                    _segmentType = segmentProperties.SegmentType;
                    richTextBox1.Visible = true;
                    comboBox1.Visible = false;
                    showInputFieldsOnType();

                    richTextBox1.Text = segmentProperties.SegmentName;
                    maskedTextBox2.Text = segmentProperties.DiameterInitial.ToString();
                    maskedTextBox3.Text = segmentProperties.DiameterFinal.ToString();
                    maskedTextBox1.Text = segmentProperties.HeightInitial.ToString();
                    maskedTextBox4.Text = segmentProperties.HeightFinal.ToString();
                    maskedTextBox5.Text = segmentProperties.Thickness.ToString();

                    DoCalculations();
                }
                else if (segmentProperties.SegmentType == "Cylinder" || segmentProperties.SegmentType == "Riser")
                {
                    _segmentType = segmentProperties.SegmentType;
                    comboBox1.Visible = false;
                    richTextBox1.Visible = true;
                    showInputFieldsOnType();

                    richTextBox1.Text = segmentProperties.SegmentName;
                    maskedTextBox2.Text = segmentProperties.Diameter.ToString();
                    maskedTextBox3.Text = segmentProperties.Thickness.ToString();
                    maskedTextBox1.Text = segmentProperties.HeightInitial.ToString();
                    maskedTextBox4.Text = segmentProperties.HeightFinal.ToString();

                    DoCalculations();
                }
                else if (segmentProperties.SegmentType == "Tanks")
                {
                    _segmentType = segmentProperties.SegmentType;
                    comboBox1.Visible = true;
                    richTextBox1.Visible = false;
                    _selectedTankCapacity = segmentProperties.SegmentName;

                    if (tankData?.Tanks != null)
                    {
                        dimensions = tankData.Tanks.FirstOrDefault(data => data.Type == _selectedTankCapacity);
                        showInputFieldsOnType();
                        comboBox1.Text = _selectedTankCapacity;
                        maskedTextBox2.Text = ExtractNumericValue(dimensions.Diameter);
                        maskedTextBox3.Text = ExtractNumericValue(dimensions.Thickness);
                        maskedTextBox1.Text = segmentProperties.HeightInitial.ToString();
                        maskedTextBox4.Text = segmentProperties.HeightFinal.ToString();

                        DoCalculations();
                    }
                    else
                    {
                        Console.WriteLine("No tanks found in the JSON file.");
                    }
                }
            }
        }

        public static string ExtractNumericValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            string pattern = @"[+-]?(?:\d+\.?\d*|\.\d+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(input);
            if (match.Success)
            {
                if (double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericValue))
                {
                    return numericValue.ToString("0.##", CultureInfo.InvariantCulture);
                }
                return match.Value;
            }
            return string.Empty;
        }

        private void GetTanksJsonData()
        {
            try
            {
                string fileName = _tankType == TankType.MultiColumn
                                  ? "MultiLeg-Tanks.json"
                                  : "tanks.json";

                string jsonPath = Path.Combine(Application.StartupPath, fileName);

                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show($"{fileName} not found in application folder.");
                    return;
                }

                string jsonString = File.ReadAllText(jsonPath);
                tankData = JsonSerializer.Deserialize<TankData>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading tank JSON: {ex.Message}");
            }
        }

        private void SegmentDialogBox_Load(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void groupBox2_Enter(object sender, EventArgs e) { }

        private void Save_ClickBase(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox1.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox2.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox3.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox1.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox4.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox5.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(maskedTextBox2.Text, out double diameterInitial) ||
                !double.TryParse(maskedTextBox3.Text, out double diameterFinal) ||
                !double.TryParse(maskedTextBox1.Text, out double heightInitial) ||
                !double.TryParse(maskedTextBox4.Text, out double heightFinal) ||
                !double.TryParse(maskedTextBox5.Text, out double thickness))
            {
                MessageBox.Show("Please enter valid numbers.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var diameter = Math.Round((diameterFinal + diameterInitial) / 2, 4);

            SegmentProperties segmentProperties;

            if (_dialogType == "Modify")
            {
                segmentProperties = _context.SegmentProperties.FirstOrDefault(item => item.SegmentNumber == _segmentNumber);

                if (segmentProperties == null)
                {
                    MessageBox.Show("Error: Segment not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Anchored continuity checks (use seed's current heights to find neighbors)
                if (!EnsureHeightContinuityOrWarn("Base", heightInitial, null, true, anchorInitial: segmentProperties.HeightInitial)) return;
                if (!EnsureForwardContinuityOrWarn("Base", heightFinal, null, true, anchorFinal: segmentProperties.HeightFinal)) return;

                try
                {
                    ValidateSegment(segmentProperties);
                }
                catch (ValidationException ve)
                {
                    MessageBox.Show($"Validation error: {ve.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                segmentProperties.SegmentName = richTextBox1.Text;
                segmentProperties.SegmentType = _segmentType;
                segmentProperties.Diameter = diameter;
                segmentProperties.Thickness = thickness;
                segmentProperties.HeightInitial = heightInitial;
                segmentProperties.HeightFinal = heightFinal;
                segmentProperties.DiameterInitial = diameterInitial;
                segmentProperties.DiameterFinal = diameterFinal;
            }
            else
            {
                // ADD → non-anchored continuity checks
                if (!EnsureHeightContinuityOrWarn("Base", heightInitial, null, false)) return;
                if (!EnsureForwardContinuityOrWarn("Base", heightFinal, null, false)) return;

                segmentProperties = new SegmentProperties()
                {
                    SegmentName = richTextBox1.Text,
                    SegmentType = _segmentType,
                    Diameter = diameter,
                    Thickness = thickness,
                    HeightInitial = heightInitial,
                    HeightFinal = heightFinal,
                    DiameterInitial = diameterInitial,
                    DiameterFinal = diameterFinal
                };

                try
                {
                    ValidateSegment(segmentProperties);
                }
                catch (ValidationException ve)
                {
                    MessageBox.Show($"Validation error: {ve.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _context.SegmentProperties.Add(segmentProperties);
            }

            try
            {
                int rowsAffected = _context.SaveChanges();
                successDialog(rowsAffected); // closes on success
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ======================================================================
        //  Save_ClickCylinder/Riser  – includes anchored backward & forward checks
        // ======================================================================
        private void Save_ClickCylinder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox1.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox2.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox3.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox1.Text) ||
                string.IsNullOrWhiteSpace(maskedTextBox4.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(maskedTextBox2.Text, out double diameter) ||
                !double.TryParse(maskedTextBox3.Text, out double thickness) ||
                !double.TryParse(maskedTextBox1.Text, out double heightInitial) ||
                !double.TryParse(maskedTextBox4.Text, out double heightFinal))
            {
                MessageBox.Show("Please enter valid numbers.", "Input Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isMultilegTank = _tankType == TankType.MultiColumn && AppState.NoOfColumns > 1;
            bool isCylinderFanOutNeeded = isMultilegTank && _segmentType == "Cylinder"; // only Cylinder fans out
            bool isModifyMode = _dialogType == "Modify";

            try
            {
                if (isModifyMode)
                {
                    SegmentProperties seed =
                        _context.SegmentProperties
                                .FirstOrDefault(s => s.SegmentNumber == _segmentNumber);

                    if (seed == null)
                    {
                        MessageBox.Show("Error: Segment not found!", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string baseName = GetBaseName(seed.SegmentName);

                    // Anchored continuity checks (use seed heights to find neighbors)
                    if (!EnsureHeightContinuityOrWarn(_segmentType, heightInitial, null, true, anchorInitial: seed.HeightInitial)) return;
                    if (!EnsureForwardContinuityOrWarn(_segmentType, heightFinal, null, true, anchorFinal: seed.HeightFinal)) return;

                    // Apply edits ONLY to the seed (no fan-out)
                    seed.SegmentName = richTextBox1.Text;
                    seed.SegmentType = _segmentType;          // "Cylinder" or "Riser"
                    seed.Diameter = diameter;
                    seed.Thickness = thickness;
                    seed.HeightInitial = heightInitial;
                    seed.HeightFinal = heightFinal;

                    ValidateSegment(seed);


                    // Build list of targets
                    List<SegmentProperties> targets;
                    if (isMultilegTank && _segmentType == "Cylinder")
                    {
                        int idx = seed.SegmentName.LastIndexOf('_');
                        string bn = (idx > 0 && int.TryParse(seed.SegmentName[(idx + 1)..], out _))
                                    ? seed.SegmentName[..idx]
                                    : seed.SegmentName;

                        targets = _context.SegmentProperties
                                          .Where(s => s.SegmentName == bn ||
                                                      s.SegmentName.StartsWith(bn + "_"))
                                          .ToList();
                    }
                    else
                    {
                        targets = new() { seed };
                    }

                    // Apply edits to each target
                    foreach (var seg in targets)
                    {
                        string suffix = "";
                        if (isMultilegTank && _segmentType == "Cylinder")
                        {
                            int idx = seg.SegmentName.LastIndexOf('_');
                            suffix = (idx > 0 && int.TryParse(seg.SegmentName[(idx + 1)..], out _))
                                      ? seg.SegmentName[idx..]   // "_n"
                                      : "";
                        }

                        seg.SegmentName = richTextBox1.Text + suffix;
                        seg.SegmentType = _segmentType;          // "Cylinder" or "Riser"
                        seg.Diameter = diameter;
                        seg.Thickness = thickness;
                        seg.HeightInitial = heightInitial;
                        seg.HeightFinal = heightFinal;

                        ValidateSegment(seg);
                    }
                }
                else
                {
                    // ADD → non-anchored continuity checks (single row, no suffix)
                    string baseName = GetBaseName(richTextBox1.Text);
                    if (!EnsureHeightContinuityOrWarn(_segmentType, heightInitial, null, false)) return;
                    if (!EnsureForwardContinuityOrWarn(_segmentType, heightFinal, null, false)) return;

                    var seg = new SegmentProperties
                    {
                        SegmentName = richTextBox1.Text,    // keep original name; no "_n"
                        SegmentType = _segmentType,         // "Riser" or "Cylinder"
                        Diameter = diameter,
                        Thickness = thickness,
                        HeightInitial = heightInitial,
                        HeightFinal = heightFinal
                    };

                    ValidateSegment(seg);
                    _context.SegmentProperties.Add(seg);

                }

                int rows = _context.SaveChanges();
                successDialog(rows); // closes on success
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving data: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SegmentDialogBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK) // If OK was not set, ask for confirmation
            {
                var result = MessageBox.Show("Do you really want to cancel without saving?", "Confirm",
                                              MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Stop the form from closing
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            // IMPORTANT: Do NOT close here. Let successDialog handle closing on success.
            if (_segmentType == "Base")
            {
                Save_ClickBase(sender, e);
            }
            else if (_segmentType == "Cylinder" || _segmentType == "Riser")
            {
                Save_ClickCylinder(sender, e);
            }
            else if (_segmentType == "Tanks")
            {
                Save_ClickTank(sender, e);
            }

            // Do not set DialogResult or Close here; warnings should NOT close the form.
        }

        public bool SaveTankProperties()
        {
            TankProperties properties = new TankProperties()
            {
                Capacity = dimensions.Type,
                WeightOfWater = ExtractNumericValue(dimensions.Weight_of_Water).ToString(),
                WeightOfSteel = ExtractNumericValue(dimensions.Weight_of_Steel).ToString(),
                TotalWeight = ExtractNumericValue(dimensions.Total_Weight).ToString(),
                ProjectedArea = ExtractNumericValue(dimensions.Projected_Area).ToString(),
                Centroid = ExtractNumericValue(dimensions.Centroid).ToString()
            };

            _context.TankProperties.Add(properties);

            try
            {
                int rowsAffected = _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving Tank Properties data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Save_ClickTank(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboBox1.Text) ||
                 string.IsNullOrWhiteSpace(maskedTextBox2.Text) ||
                 string.IsNullOrWhiteSpace(maskedTextBox3.Text) ||
                 string.IsNullOrWhiteSpace(maskedTextBox1.Text) ||
                 string.IsNullOrWhiteSpace(maskedTextBox4.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(maskedTextBox2.Text, out double diameter) ||
                !double.TryParse(maskedTextBox3.Text, out double thickness) ||
                !double.TryParse(maskedTextBox1.Text, out double heightInitial) ||
                !double.TryParse(maskedTextBox4.Text, out double heightFinal))
            {
                MessageBox.Show("Please enter valid numbers.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SegmentProperties segmentProperties;

            if (_dialogType == "Modify")
            {
                // fetch seed first so we can anchor neighbor search
                segmentProperties = _context.SegmentProperties.FirstOrDefault(item => item.SegmentNumber == _segmentNumber);
                if (segmentProperties == null)
                {
                    MessageBox.Show("Error: Segment not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string baseName = GetBaseName(comboBox1.Text ?? "");

                // Anchored continuity (Tanks included in chain)
                if (!EnsureHeightContinuityOrWarn("Tanks", heightInitial, baseName, true, anchorInitial: segmentProperties.HeightInitial)) return;
                if (!EnsureForwardContinuityOrWarn("Tanks", heightFinal, baseName, true, anchorFinal: segmentProperties.HeightFinal)) return;

                if (!SaveTankProperties())
                {
                    return;
                }

                ValidateSegment(segmentProperties);

                segmentProperties.SegmentName = comboBox1.Text;
                segmentProperties.SegmentType = _segmentType;
                segmentProperties.Diameter = diameter;
                segmentProperties.Thickness = thickness;
                segmentProperties.HeightInitial = heightInitial;
                segmentProperties.HeightFinal = heightFinal;
            }
            else
            {
                // ADD → non-anchored continuity
                string baseName = GetBaseName(comboBox1.Text ?? "");
                if (!EnsureHeightContinuityOrWarn("Tanks", heightInitial, baseName, false)) return;
                if (!EnsureForwardContinuityOrWarn("Tanks", heightFinal, baseName, false)) return;

                if (!SaveTankProperties())
                {
                    return;
                }

                if (_selectedTankCapacity != null)
                {
                    if (tankData?.Tanks != null)
                    {
                        dimensions = tankData.Tanks.FirstOrDefault(data => data.Type == _selectedTankCapacity);
                        showInputFieldsOnType();
                        if (dimensions != null)
                        {
                            segmentProperties = new SegmentProperties()
                            {
                                SegmentName = comboBox1.Text,
                                SegmentType = _segmentType,
                                Diameter = Double.Parse(ExtractNumericValue(dimensions.Diameter)),
                                Thickness = Double.Parse(ExtractNumericValue(dimensions.Thickness)),
                                HeightInitial = heightInitial,
                                HeightFinal = heightFinal
                            };

                            ValidateSegment(segmentProperties);

                            var existingTank = _context.SegmentProperties
                                                       .FirstOrDefault(s => s.SegmentType == "Tanks");

                            if (existingTank != null)
                            {
                                DialogResult result = MessageBox.Show(
                                    $"A tank already exists with name: {existingTank.SegmentName}.\n" +
                                    "Saving this will override/delete the previous tank.\nDo you want to continue?",
                                    "Tank Already Exists",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning
                                );

                                if (result == DialogResult.No)
                                {
                                    return;
                                }
                                else
                                {
                                    _context.SegmentProperties.Remove(existingTank);
                                    _context.SegmentProperties.Add(segmentProperties);
                                }
                            }
                            else
                            {
                                _context.SegmentProperties.Add(segmentProperties);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No tanks found in the JSON file.");
                    }
                }
            }
            try
            {
                int rowsAffected = _context.SaveChanges();
                successDialog(rowsAffected);
                _waterTankForm.OnSegmentAdded();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ValidateSegment(SegmentProperties segment)
        {
            if (segment.SegmentType == "Base" && (segment.DiameterInitial == null || segment.DiameterFinal == null))
            {
                throw new ValidationException("DiameterInitial and DiameterFinal must be specified for 'base' segment type.");
            }
            else if (segment.SegmentType != "Base" && (segment.DiameterInitial != null || segment.DiameterFinal != null))
            {
                throw new ValidationException("DiameterInitial and DiameterFinal should be null for non-base segment types.");
            }
            else if (segment.SegmentType == "Base" && (segment.DiameterInitial != null && segment.DiameterFinal != null && segment.Diameter == null))
            {
                segment.Diameter = (double)(segment.DiameterFinal - segment.DiameterInitial);
            }
        }

        private void successDialog(int rowsAffected)
        {
            if (rowsAffected > 0)
            {
                DialogResult result = MessageBox.Show("Data saved successfully!", "Confirmation", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else
            {
                DialogResult result = MessageBox.Show("Data might not have been saved!", "Confirmation", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label9_Click(object sender, EventArgs e) { }
        private void groupBox3_Enter(object sender, EventArgs e) { }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            // Removed DoCalculations() to prevent infinite loop
        }

        public void DoCalculations()
        {
            if (string.IsNullOrWhiteSpace(_segmentType))
            {
                setTextboxvalues("Unknown Segment Type");
                return;
            }

            bool validInput = ValidateAndParseInput(out double diameter, out double diameterFinal, out double thickness, out double heightInitial, out double heightFinal);

            if (!validInput)
            {
                setTextboxvalues("");
                return;
            }

            if (_segmentType == "Cylinder" || _segmentType == "Riser")
            {
                CalculateCylinderValues(heightInitial, heightFinal, diameter, thickness, _segmentType);
            }
            else if (_segmentType == "Base")
            {
                CalculateBaseValues(heightInitial, heightFinal, diameter, diameterFinal, thickness);
            }
            else if (_segmentType == "Tanks")
            {
                CalculateTankValues(heightInitial, heightFinal);
            }
            else
            {
                setTextboxvalues("Unknown Segment Type");
            }
        }

        private bool ValidateAndParseInput(out double diameter, out double diameterFinal, out double thickness, out double heightInitial, out double heightFinal)
        {
            diameter = diameterFinal = thickness = heightInitial = heightFinal = 0.0;

            bool isCylinderValid = (_segmentType == "Cylinder" || _segmentType == "Riser") &&
                                   double.TryParse(maskedTextBox2.Text, out diameter) &&
                                   double.TryParse(maskedTextBox3.Text, out thickness) &&
                                   double.TryParse(maskedTextBox1.Text, out heightInitial) &&
                                   double.TryParse(maskedTextBox4.Text, out heightFinal);

            bool isBaseValid = _segmentType == "Base" &&
                               double.TryParse(maskedTextBox2.Text, out diameter) &&
                               double.TryParse(maskedTextBox3.Text, out diameterFinal) &&
                               double.TryParse(maskedTextBox1.Text, out heightInitial) &&
                               double.TryParse(maskedTextBox4.Text, out heightFinal) &&
                               double.TryParse(maskedTextBox5.Text, out thickness);

            bool isTankValid = _segmentType == "Tanks" &&
                                double.TryParse(maskedTextBox2.Text, out diameter) &&
                                double.TryParse(maskedTextBox3.Text, out thickness) &&
                                double.TryParse(maskedTextBox1.Text, out heightInitial) &&
                                double.TryParse(maskedTextBox4.Text, out heightFinal);

            return isCylinderValid || isBaseValid || isTankValid;
        }

        private void CalculateCylinderValues(double heightInitial, double heightFinal, double diameter, double thickness,string segmentType)
        {
            if (AppState.CurrentTankType == TankType.SingleColumn)
            {
                Segment_Cylinder_Equations cylinder_Equations = new Segment_Cylinder_Equations();

                textBox1.Text = cylinder_Equations.weightOfPedestal(heightInitial, heightFinal, diameter, thickness).ToString("F4");
                textBox2.Text = cylinder_Equations.ProjectedArea(heightInitial, heightFinal, diameter).ToString("F4");
                textBox3.Text = cylinder_Equations.Centroid(heightInitial, heightFinal).ToString("F4");
                textBox4.Text = cylinder_Equations.kzi(heightInitial).ToString("F4");
                textBox5.Text = cylinder_Equations.kzf(heightFinal).ToString("F4");
                textBox6.Text = cylinder_Equations.qzi(heightInitial).ToString("F4");
                textBox7.Text = cylinder_Equations.qzf(heightFinal).ToString("F4");
                textBox8.Text = cylinder_Equations.F(heightInitial, heightFinal, diameter).ToString("F4");
                textBox9.Text = cylinder_Equations.L(heightInitial, heightFinal).ToString("F4");
                textBox10.Text = cylinder_Equations.Mbase(heightInitial, heightFinal, diameter).ToString("F4");
            }
            else if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                Multileg_Cylinders cylinder_Equations = new Multileg_Cylinders();
                Segment_Cylinder_Equations cylinder_Equations1 = new Segment_Cylinder_Equations();


                textBox1.Text = cylinder_Equations.weightOfPedestal(heightInitial, heightFinal, diameter, thickness, segmentType).ToString("F4");

                if(segmentType == "Riser")
                {
                    textBox2.Text = cylinder_Equations1.ProjectedArea(heightInitial, heightFinal, diameter).ToString("F4");

                }
                else
                {
                    textBox2.Text = cylinder_Equations.ProjectedArea(heightInitial, heightFinal, diameter).ToString("F4");

                }
                textBox3.Text = cylinder_Equations.Centroid(heightInitial, heightFinal).ToString("F4");
                textBox4.Text = cylinder_Equations.kzi(heightInitial).ToString("F4");
                textBox5.Text = cylinder_Equations.kzf(heightFinal).ToString("F4");
                textBox6.Text = cylinder_Equations.qzi(heightInitial).ToString("F4");
                textBox7.Text = cylinder_Equations.qzf(heightFinal).ToString("F4");
                textBox8.Text = cylinder_Equations.F(heightInitial, heightFinal, diameter,segmentType).ToString("F4");
                textBox9.Text = cylinder_Equations.L(heightInitial, heightFinal).ToString("F4");
                textBox10.Text = cylinder_Equations.Mbase(heightInitial, heightFinal, diameter,segmentType).ToString("F4");
            }
        }

        private void CalculateTankValues(double heightInitial, double heightFinal)
        {
            Segment_Cylinder_Equations cylinder_Equations = new Segment_Cylinder_Equations();
            Multileg_Cylinders multileg_Cylinders = new Multileg_Cylinders();

            var tankProperties = tankData.Tanks.FirstOrDefault(data => data.Type == _selectedTankCapacity);

            if (tankProperties != null)
            {
                double projectedArea = Double.Parse(ExtractNumericValue(tankProperties.Projected_Area));
                double totalWeight = Double.Parse(ExtractNumericValue(tankProperties.Weight_of_Steel));
                double diameter = Double.Parse(ExtractNumericValue(tankProperties.Diameter));
                double f = calculateF(cylinder_Equations.qzi(heightInitial), cylinder_Equations.qzf(heightFinal), projectedArea);

                textBox1.Text = totalWeight.ToString();
                textBox2.Text = projectedArea.ToString();
                textBox3.Text = tankProperties.Centroid;
                textBox4.Text = multileg_Cylinders.kzi(heightInitial).ToString("F4");
                textBox5.Text = multileg_Cylinders.kzf(heightFinal).ToString("F4");
                textBox6.Text = multileg_Cylinders.qzi(heightInitial).ToString("F4");
                textBox7.Text = multileg_Cylinders.qzf(heightFinal).ToString("F4");
                textBox8.Text = multileg_Cylinders.F_Tank(heightInitial, heightFinal, diameter, projectedArea).ToString("F4");
                textBox9.Text = tankProperties.Centroid;
                textBox10.Text = (multileg_Cylinders.F_Tank(heightInitial, heightFinal, diameter, projectedArea) * Double.Parse(tankProperties.Centroid)).ToString("F4");
            }
            else
            {
                var _dimensions = tankData.Tanks.FirstOrDefault(data => data.Type == _selectedTankCapacity);

                double projectedArea = ExtractDoubleValue(_dimensions.Projected_Area);
                double totalWeight = ExtractDoubleValue(_dimensions.Total_Weight);
                double f = calculateF(cylinder_Equations.qzi(heightInitial), cylinder_Equations.qzf(heightFinal), projectedArea);
                double diameter = Double.Parse(ExtractNumericValue(_dimensions.Diameter));

                textBox1.Text = totalWeight.ToString();
                textBox2.Text = projectedArea.ToString();
                textBox3.Text = _dimensions.Centroid;
                textBox4.Text = multileg_Cylinders.kzi(heightInitial).ToString("F4");
                textBox5.Text = multileg_Cylinders.kzf(heightFinal).ToString("F4");
                textBox6.Text = multileg_Cylinders.qzi(heightInitial).ToString("F4");
                textBox7.Text = multileg_Cylinders.qzf(heightFinal).ToString("F4");
                textBox8.Text = multileg_Cylinders.F_Tank(heightInitial, heightFinal, diameter, projectedArea).ToString("F4");
                textBox9.Text = _dimensions.Centroid;
                textBox10.Text = (multileg_Cylinders.F_Tank(heightInitial, heightFinal, diameter, projectedArea) * Double.Parse(_dimensions.Centroid)).ToString("F4");
            }
        }

        public static double ExtractDoubleValue(string input)
        {
            string[] parts = input.Split();
            if (parts.Length == 0) throw new FormatException("Input string is empty.");
            if (double.TryParse(parts[0], out double result)) return result;
            else throw new FormatException($"Unable to parse '{parts[0]}' as a double.");
        }

        private double calculateF(double qzi, double qzf, double projectedArea)
        {
            var result = (((qzi + qzf) / 2) * projectedArea) / 1000;
            return result;
        }

        private void CalculateBaseValues(double heightInitial, double heightFinal, double diameterInitial, double diameterFinal, double thickness)
        {
            Segment_Conical_Equations conical_Equations = new Segment_Conical_Equations();
            var diameter = (diameterFinal + diameterInitial) / 2;

            textBox1.Text = conical_Equations.weight(heightInitial, heightFinal, diameterInitial, diameterFinal, thickness).ToString("F4");
            textBox2.Text = conical_Equations.ProjectedArea(heightInitial, heightFinal, diameter).ToString("F4");
            textBox3.Text = conical_Equations.Centroid(heightInitial, heightFinal, diameterInitial, diameterFinal).ToString("F4");
            textBox4.Text = conical_Equations.kzi(heightInitial).ToString("F4");
            textBox5.Text = conical_Equations.kzf(heightFinal).ToString("F4");
            textBox6.Text = conical_Equations.qzi(heightInitial).ToString("F4");
            textBox7.Text = conical_Equations.qzf(heightFinal).ToString("F4");
            textBox8.Text = conical_Equations.F(heightInitial, heightFinal, diameter).ToString("F4");
            textBox9.Text = conical_Equations.L(heightInitial, heightFinal).ToString("F4");
            textBox10.Text = conical_Equations.Mbase(heightInitial, heightFinal, diameter).ToString("F4");
        }

        public void setTextboxvalues(string value)
        {
            textBox1.Text = value;
            textBox2.Text = value;
            textBox3.Text = value;
            textBox4.Text = value;
            textBox5.Text = value;
            textBox6.Text = value;
            textBox7.Text = value;
            textBox8.Text = value;
            textBox9.Text = value;
            textBox10.Text = value;
        }

        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label25_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void maskedTextBox2_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
        private void maskedTextBox3_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
        private void maskedTextBox5_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void textBox9_TextChanged(object sender, EventArgs e) { }
        private void textBox3_TextChanged(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e) { }
        private void maskedTextBox4_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
    }
}
