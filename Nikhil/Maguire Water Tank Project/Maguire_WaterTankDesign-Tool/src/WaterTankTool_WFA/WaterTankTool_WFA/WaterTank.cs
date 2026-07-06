 using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Custom_Design_Control;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Foundation_Design;
using WaterTankTool_WFA.Load;
using WaterTankTool_WFA.MultiColumn.Segments;
using WaterTankTool_WFA.Output.SpheroidTank;
using WaterTankTool_WFA.Solver;

namespace WaterTankTool_WFA
{


    public partial class WaterTank : Form
    {
        StructuralDrawingForm tankDesign = new StructuralDrawingForm();
        WaterTankDbContext context;
        private Image drawingImage;
        private Point lastMousePosition;
        private bool isDragging = false;
        private DataGridView dimensionGridView;
        private bool isDimensionTableVisible = false;
        private bool isSolverVisible = false;

        private ToolStripStatusLabel appStatusLabel;
        private ToolStripStatusLabel selectedMaterialLabel;
        private ToolStripStatusLabel designDetailsLabel;
        private ToolStripStatusLabel noMaterialStatus;
        private ToolStripStatusLabel noLoadStatus;
        private StartupForm _startupForm;

        private List<(RectangleF Bounds, SegmentProperties Segment)> segmentLabels = new List<(RectangleF, SegmentProperties)>();
        private Point imageOffset = new Point(0, 0);
        private float zoomFactor = 1.0f;
        private float rotationAngle = 0.0f; // Rotation angle in degrees

        private bool isExpanded = false;
        private int expandedWidth = 420; // desired width when expanded
        private DataGridView dataGridView1;

        private TankType _tankType;

        public WaterTank(StartupForm startupForm)
        {
            InitializeComponent();
            var _context = WaterTankDbContext.GetInstance();
            context = _context;
            _startupForm = startupForm;
            _tankType = AppState.CurrentTankType;

            InitializeStatusStrip2();
            InitializeDataGridView();
            panelDrawTankCapacity();

            panel1.Paint += panel1_Paint;
            panel1.MouseWheel += panel1_MouseWheel;
            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseMove += panel1_MouseMove;
            panel1.MouseUp += panel1_MouseUp;
            panel1.MouseClick += panel1_MouseClick;
            this.Resize += (s, e) => this.Invalidate();
            UpdateLoadStatus();
        }

        private void InitializeStatusStrip2()
        {
            appStatusLabel = new ToolStripStatusLabel
            {
                Text = "Status: Ready",
                TextAlign = ContentAlignment.MiddleLeft
            };

            selectedMaterialLabel = new ToolStripStatusLabel
            {
                Text = "Selected Material: None",
                TextAlign = ContentAlignment.MiddleLeft
            };

            noMaterialStatus = new ToolStripStatusLabel
            {
                Text = "Please Add the Material Type!",
                ForeColor = Color.Red,
                Visible = true,
            };

            noLoadStatus = new ToolStripStatusLabel
            {
                Text = "Please add the Load!",
                ForeColor = Color.Red,
                Visible = true,
            };

            designDetailsLabel = new ToolStripStatusLabel
            {
                Text = $"Tank: - | Total Weight: 0Kips | Area: 0ft�",
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            statusStrip2.Items.Add(appStatusLabel);
            statusStrip2.Items.Add(new ToolStripSeparator());
            statusStrip2.Items.Add(selectedMaterialLabel);
            statusStrip2.Items.Add(new ToolStripSeparator());
            statusStrip2.Items.Add(noMaterialStatus);
            statusStrip2.Items.Add(noLoadStatus);
            statusStrip2.Items.Add(designDetailsLabel);
        }

        private void ExportDiagram(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg";
                saveFileDialog.Title = "Export Diagram";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    int exportWidth = panel1.Width * 2;
                    int exportHeight = panel1.Height * 2;
                    Bitmap bitmap = new Bitmap(exportWidth, exportHeight);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.ScaleTransform(2.0f, 2.0f);
                        if (drawingImage != null)
                        {
                            int scaledWidth = (int)(drawingImage.Width * zoomFactor);
                            int scaledHeight = (int)(drawingImage.Height * zoomFactor);
                            int x = (panel1.Width - scaledWidth) / 2 + imageOffset.X;
                            int y = (panel1.Height - scaledHeight) / 2 + imageOffset.Y;
                            Rectangle destRect = new Rectangle(x, y, scaledWidth, scaledHeight);

                            g.DrawImage(drawingImage, destRect);
                            DrawSegmentLabels(g, destRect);
                        }
                    }
                    bitmap.Save(saveFileDialog.FileName, saveFileDialog.FilterIndex == 1 ? ImageFormat.Png : ImageFormat.Jpeg);
                    bitmap.Dispose();
                    MessageBox.Show("Diagram exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void panelDrawTankCapacity()
        {


            var tankCap = context.TankProperties?.FirstOrDefault();

            UpdateMaterial();
            UpdateLoadStatus();

            // ?? Image and status changes according to tank type
            Image singleImg = Properties.Resources.Tank_Design;
            Image multiImg = Properties.Resources.Multileg_Tank;

            if (tankCap?.Capacity != null)
            {
                switch (tankCap.Capacity)
                {
                    case "150,000 gallon":
                    case "250,000 gallon":
                    case "500,000 gallon":
                        drawingImage = (_tankType == TankType.SingleColumn)
                            ? singleImg
                            : multiImg;
                        UpdateAppStatus($"Tank Loaded: {tankCap.Capacity}");
                        UpdateDesignDetails(tankCap.Capacity, tankCap.TotalWeight, tankCap.ProjectedArea);
                        break;
                    default:
                        drawingImage = (_tankType == TankType.SingleColumn)
                            ? singleImg
                            : multiImg;
                        UpdateAppStatus($"Tank Loaded: {tankCap.Capacity}");
                        UpdateDesignDetails(tankCap.Capacity, tankCap.TotalWeight, tankCap.ProjectedArea);
                        break;
                }
            }
            else
            {
                drawingImage = null;
                UpdateAppStatus("No Tank Loaded");
                UpdateDesignDetails("-", "0", "0");
            }

            panel1.Invalidate();
        }

        private void UpdateDesignDetails(string tank, string weight, string area)
        {
            string tankTypeString = _tankType == TankType.SingleColumn ? "Single Pedestal" : "Multi-Leg";
            designDetailsLabel.Text = $"[{tankTypeString}] Tank: {tank} | Total Weight: {weight} | Area: {area}";
        }

        private void UpdateAppStatus(string status)
        {
            appStatusLabel.Text = $"Status: {status}";
        }

        private void UpdateMaterial()
        {
            var material = context.MaterialProperties.FirstOrDefault();
            if (material != null)
            {
                selectedMaterialLabel.Text = $"Material: {material.MaterialName}";
                noMaterialStatus.Visible = false;
            }
            else
            {
                selectedMaterialLabel.Text = "Material: None";
                noMaterialStatus.Visible = true;
            }
        }

        public void UpdateLoadStatus()
        {
            var liveLoad = context.LiveLoadEntity.FirstOrDefault();
            var seismicLoad = context.SeismicLoadEntity.FirstOrDefault();
            var snowLoad = context.SnowLoadEntity?.FirstOrDefault();
            var windLoad = context.WindLoadEntity.FirstOrDefault();

            noLoadStatus.Visible = (liveLoad == null || seismicLoad == null || snowLoad == null || windLoad == null);
        }

        public void OnSegmentAdded()
        {
            panelDrawTankCapacity();
            LoadData();
            panel1.Invalidate();
        }

        public void OnmaterialAdded() => UpdateMaterial();
        public void OnMaterialDeleted() => UpdateMaterial();
        public void OnSegmentDeleted()
        {
            panelDrawTankCapacity();
            LoadData();
            panel1.Invalidate();
        }

        private void LoadData()
        {
            // Bind or refresh grid, if needed
        }

        public List<SegmentProperties> GetSegmentsFromDatabase()
            => context.SegmentProperties.ToList();

        private void addSegmentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SegmentDialogBox dialog = new SegmentDialogBox();
            dialog.ShowDialog();
        }

        private void materialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Define_Materials dialog = new Define_Materials(this);
            dialog.ShowDialog();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Define_Segments define_Segments = new Define_Segments(this);
            define_Segments.ShowDialog();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (_tankType == TankType.SingleColumn)
            {
                Define_Segments define_Segments = new Define_Segments(this);
                define_Segments.ShowDialog();
            }
            else if (_tankType == TankType.MultiColumn)
            {
                //DefineMultiLegSegments define_ = new DefineMultiLegSegments(this);
                //define_.ShowDialog();

                Define_Segments define_Segments = new Define_Segments(this);
                define_Segments.ShowDialog();
            }

        }

        private void windLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Wind_Load wind_Load = new Wind_Load();
            wind_Load.ShowDialog();
        }

        private void liveLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Live_Load live_Load = new Live_Load();
            live_Load.ShowDialog();
        }

        private void deadLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dead_Load dead_Load = new Dead_Load();
            dead_Load.ShowDialog();
        }

        private void waterLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Water_Load water_Load = new Water_Load();
            water_Load.ShowDialog();
        }

        private void seismicLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Seismic seismic = new Seismic(this);
            seismic.ShowDialog();
        }

        private void pasteToolStripButton1_Click(object sender, EventArgs e)
        {
            zoomFactor += 0.1f;
            panel1.Invalidate();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }


        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Solver_Output solver_Output = new Solver_Output(this);
            //solver_Output.ShowDialog();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewProject();
        }

        private void CreateNewProject()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Project Files (*.proj)|*.proj";
                saveFileDialog.Title = "Create New Project";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string projectPath = saveFileDialog.FileName;
                    try
                    {
                        File.WriteAllText(projectPath, "Default project content or structure.");
                        string projectFolder = Path.GetDirectoryName(projectPath);
                        Directory.CreateDirectory(Path.Combine(projectFolder, "Assets"));
                        Directory.CreateDirectory(Path.Combine(projectFolder, "Data"));
                        InitializeNewProjectUI();

                        // Create metadata and save project.json
                        string projectName = Path.GetFileNameWithoutExtension(projectPath);
                        var metadata = new ProjectMetadata
                        {
                            ProjectName = projectName,
                            Type = _tankType, // <---- Use the current selected tank type here!
                            ProjectFilePath = projectPath
                        };
                        string metaPath = Path.Combine(projectFolder, "project.json");
                        File.WriteAllText(metaPath, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                        _startupForm.OpenProject(projectPath, _tankType); // <-- Pass tank type!
                        MessageBox.Show("New project created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating new project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void InitializeNewProjectUI()
        {
            // Add custom initialization if required
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            //Solver_Output solver = new Solver_Output(this);
            //solver.ShowDialog();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            Define_Materials define_Materials = new Define_Materials(this);
            define_Materials.ShowDialog();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            using (BufferedGraphicsContext context = new BufferedGraphicsContext())
            using (BufferedGraphics buffer = context.Allocate(e.Graphics, panel1.ClientRectangle))
            {
                Graphics g = buffer.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(panel1.BackColor);

                if (drawingImage != null)
                {
                    int scaledWidth = (int)(drawingImage.Width * zoomFactor);
                    int scaledHeight = (int)(drawingImage.Height * zoomFactor);
                    int x = (panel1.Width - scaledWidth) / 2 + imageOffset.X;
                    int y = (panel1.Height - scaledHeight) / 2 + imageOffset.Y;
                    Rectangle destRect = new Rectangle(x, y, scaledWidth, scaledHeight);

                    g.DrawImage(drawingImage, destRect);
                    //DrawSegmentLabels(g, destRect);
                }
                buffer.Render(e.Graphics);
            }
        }

        private void DrawSegmentLabels(Graphics g, Rectangle imageBounds)
        {
            segmentLabels.Clear();
            var segments = context.SegmentProperties.ToList();
            if (segments.Count == 0)
                return;

            int tankHeight = imageBounds.Height;
            int totalSegmentHeight = segments.Sum(s => (int)(s.HeightFinal - s.HeightInitial));
            double scaleFactor = (double)tankHeight / totalSegmentHeight;
            int labelX = imageBounds.Right + 40;
            List<RectangleF> labelBounds = new List<RectangleF>();

            int baseBottom = imageBounds.Bottom;
            int baseTop = imageBounds.Top + (int)(imageBounds.Height * 0.80);
            int cylinderTop = baseTop;
            int cylinderBottom = imageBounds.Top + (int)(imageBounds.Height * 0.35);
            int tankTop = imageBounds.Top;
            int tankBottom = cylinderBottom;

            foreach (var segment in segments.OrderBy(s => s.HeightInitial))
            {
                int segmentTop = imageBounds.Bottom - (int)(segment.HeightFinal * scaleFactor);
                int segmentBottom = imageBounds.Bottom - (int)(segment.HeightInitial * scaleFactor);
                int segmentHeight = segmentBottom - segmentTop;

                if (segment.SegmentType == "Tanks")
                {
                    segmentTop = tankTop;
                    segmentBottom = tankBottom;
                    segmentHeight = segmentBottom - segmentTop;
                }

                int segmentCenterY = segmentTop + (segmentHeight / 2);

                if (segmentCenterY < imageBounds.Top + 20)
                    segmentCenterY = imageBounds.Top + 20;
                if (segmentCenterY > imageBounds.Bottom - 20)
                    segmentCenterY = imageBounds.Bottom - 20;

                while (labelBounds.Any(label => label.IntersectsWith(new RectangleF(labelX, segmentCenterY, 150, 20))))
                {
                    segmentCenterY += 30;
                }

                Point arrowStart = new Point(imageBounds.Right + 20, segmentTop);
                Point arrowEnd = new Point(imageBounds.Right + 20, segmentBottom);
                DrawDoubleArrowVerticalLine(g, arrowStart, arrowEnd, Color.Black);

                string labelText = $"{segment.SegmentName}:{segment.SegmentType}, H={Math.Round((segment.HeightFinal - segment.HeightInitial), 4)}ft, D={segment.Diameter}ft, T={segment.Thickness}in";
                using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(labelText, font);
                    Point labelPosition = new Point(labelX, segmentCenterY - (int)(textSize.Height / 2));
                    g.DrawString(labelText, font, Brushes.Black, labelPosition);
                    labelBounds.Add(new RectangleF(labelPosition, textSize));
                    segmentLabels.Add((new RectangleF(labelPosition, textSize), segment));
                }
            }
        }

        private void DrawDoubleArrowVerticalLine(Graphics g, Point start, Point end, Color color)
        {
            using (Pen pen = new Pen(color, 2))
            {
                AdjustableArrowCap arrowCap = new AdjustableArrowCap(5, 5);
                pen.CustomStartCap = arrowCap;
                pen.CustomEndCap = arrowCap;
                g.DrawLine(pen, start, end);
            }
        }

        private void panel1_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoomFactor;
            zoomFactor *= (e.Delta > 0) ? 1.1f : 0.9f;
            zoomFactor = Math.Max(0.2f, Math.Min(5.0f, zoomFactor));
            float scaleChange = zoomFactor / oldZoom;
            imageOffset.X = (int)((e.X - panel1.Width / 2) * (scaleChange - 1));
            imageOffset.Y = (int)((e.Y - panel1.Height / 2) * (scaleChange - 1));
            panel1.Invalidate();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                lastMousePosition = e.Location;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripTextBox1.Text = $"{e.X}";
            toolStripTextBox2.Text = $"{e.Y}";

            if (isDragging)
            {
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;

                rotationAngle += deltaX * 0.5f;
                lastMousePosition = e.Location;
                panel1.Invalidate();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (var (bounds, segment) in segmentLabels)
            {
                if (bounds.Contains(e.Location))
                {
                    MessageBox.Show($"Segment: {segment.SegmentType}\n" +
                                    $"Height: {segment.HeightFinal - segment.HeightInitial}m\n" +
                                    $"Diameter: {segment.Diameter}m\n" +
                                    $"Thickness: {segment.Thickness}cm",
                                    "Segment Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }
            }
        }

        private void rotateButton_Click(object sender, EventArgs e)
        {
            rotationAngle = (rotationAngle + 15) % 360;
            panel1.Invalidate();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            zoomFactor -= 0.1f;
            panel1.Invalidate();
        }

        private void snowLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Snow_Load snow_Load = new Snow_Load();
            snow_Load.ShowDialog();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Project Files (*.proj)|*.proj";
                openFileDialog.Title = "Open Project";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string projectPath = openFileDialog.FileName;

                    try
                    {
                        // Optional: Load project metadata if available
                        string projectFolder = Path.GetDirectoryName(projectPath);
                        string metaPath = Path.Combine(projectFolder, "project.json");

                        TankType projectTankType = _tankType; // Default to current
                        if (File.Exists(metaPath))
                        {
                            var metadataJson = File.ReadAllText(metaPath);
                            var metadata = System.Text.Json.JsonSerializer.Deserialize<ProjectMetadata>(metadataJson);
                            if (metadata != null)
                            {
                                projectTankType = metadata.Type;
                            }
                        }

                        // Call the same startup form handler you use for new projects
                        _startupForm.OpenProject(projectPath, projectTankType);
                        MessageBox.Show("Project opened successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void splitContainer1_Panel1_Paint_1(object sender, PaintEventArgs e) { }
        private void newToolStripButton_Click(object sender, EventArgs e) { CreateNewProject(); }
        private void splitContainer2_Panel2_Paint_1(object sender, PaintEventArgs e) { }
        private void splitContainer2_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void toolStripTextBox1_Click(object sender, EventArgs e) { }
        private void toolStripTextBox2_Click(object sender, EventArgs e) { }
        private void statusStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
        private void Form1_Click(object sender, EventArgs e) { }
        private void toolStripMenuItem1_Click(object sender, EventArgs e) { }
        private void toolStripMenuItem2_Click(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { panel1.Invalidate(); }
        private void toolStripContainer1_ContentPanel_Load(object sender, EventArgs e) { }
        private void splitContainer2_Panel2_Paint(object sender, PaintEventArgs e) { }
        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e) { }
        private void splitContainer3_Panel2_Paint(object sender, PaintEventArgs e) { }
        private void splitContainer3_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) { }
        private void toolStripStatusLabel1_Click(object sender, EventArgs e) { }
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e) { }
        private void toolsToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void toolStrip3_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
        private void toolStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
        private void toolStripButton1_Click(object sender, EventArgs e) { }
        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            Help help = new Help();
            help.ShowDialog();
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Live_Load live_Load = new Live_Load();
            live_Load.ShowDialog();
        }
        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            Snow_Load snow_Load = new Snow_Load();
            snow_Load.ShowDialog();
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Wind_Load wind_Load = new Wind_Load();
            wind_Load.ShowDialog();
        }
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            Dead_Load dead_Load = new Dead_Load();
            dead_Load.ShowDialog();
        }
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            Seismic seismic = new Seismic(this);
            seismic.ShowDialog();
        }
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            ExportDiagram(sender, e);
        }

        private void InitializeDataGridView()
        {
            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            //panel2.Controls.Add(dataGridView1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isExpanded)
            {
                panel2.Width = expandedWidth;
                panel2.Visible = true;
                dataGridView1.Visible = true;
                var data = context.SegmentProperties.ToList();
                data.Sort((x, y) => y.HeightInitial.CompareTo(x.HeightInitial));
                dataGridView1.DataSource = data;
                button1.Text = "Close";
            }
            else
            {
                panel2.Width = 30;
                dataGridView1.Visible = false;
                button1.Text = "<";
            }
            isExpanded = !isExpanded;
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            Water_Load water_ = new Water_Load();
            water_.ShowDialog();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            Load_Combinations load_Combinations = new Load_Combinations();
            load_Combinations.ShowDialog();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            DesignTable designTable = new DesignTable();
            designTable.ShowDialog();
        }

        private void basePlateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BasePlateParameters basePlateParameters = new BasePlateParameters();
            basePlateParameters.ShowDialog();
        }

        private void anchorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AnchorBoltSelectionForm selectionForm = new AnchorBoltSelectionForm())
            {
                if (selectionForm.ShowDialog() == DialogResult.OK)
                {
                    AnchorBoltParameters anchorBoltParameters = new AnchorBoltParameters();
                    anchorBoltParameters.ShowDialog();
                }
            }
        }

        private void sideChairsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exportOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {


            var builder = new FabricationOutputRowBuilder(this);
            var row = builder.BuildFromCurrentDesign();


            var exporter = new ExportFabricationOutput();
            exporter.RunExport(row, this);
        }

        private void engineeringSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var exporter = new ExportEngineeringSummary();
            exporter.RunExport(this);
        }

        private void analyzeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OutputModal outputModal = new OutputModal();
            outputModal.ShowDialog();
        }

        private void designToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RingWallFoundation ringWallFoundation = new RingWallFoundation();
            ringWallFoundation.ShowDialog();
        }

        private void footingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Placeholder for Footing design logic
            MessageBox.Show("Footing Design module coming soon.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void materialToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FoundationMaterial foundationMaterial = new FoundationMaterial();
            foundationMaterial.ShowDialog();
        }

        private void geoTechnicalPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GeoTechnicalProperties geoTechnicalProperties = new GeoTechnicalProperties();
            geoTechnicalProperties.ShowDialog();
        }

        private void loadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FoundationLoad foundationLoad = new FoundationLoad();
            foundationLoad.ShowDialog();
        }
    }
}
