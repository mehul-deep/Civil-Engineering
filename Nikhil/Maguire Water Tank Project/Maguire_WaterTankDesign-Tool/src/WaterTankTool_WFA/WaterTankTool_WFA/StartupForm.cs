using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA
{
    public enum TankType { None, SingleColumn, MultiColumn }

    public class ProjectMetadata
    {
        public string ProjectName { get; set; }
        public TankType Type { get; set; }
        public string ProjectFilePath { get; set; }
    }

    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }

    public partial class StartupForm : Form
    {
        private readonly DIContainer _diContainer;
        private List<ProjectMetadata> recentProjects = new List<ProjectMetadata>();
        private TankType _selectedTankType = TankType.None;

        private Panel tankTypeSelectionPanel;
        private Panel mainStartupPanel;
        private DoubleBufferedFlowLayoutPanel recentProjectsPanel;
        private DoubleBufferedFlowLayoutPanel buttonPanel;


        private Image backGroundImageRight;
        private Image backGroundImageLeft;

        public StartupForm(DIContainer diContainer)
        {
            InitializeComponent();
            _diContainer = diContainer;
            _selectedTankType = AppState.CurrentTankType;

            // --- Main window setup (startup panel for projects) ---
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowIcon = false;
            this.BackColor = ColorTranslator.FromHtml("#F9F9F9");
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.AutoScaleMode = AutoScaleMode.None;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.Font = new Font("Segoe UI", 10);


            mainStartupPanel = CreateMainStartupPanel();
            this.Controls.Add(mainStartupPanel);
            mainStartupPanel.Dock = DockStyle.Fill;
            mainStartupPanel.Visible = true;

            // Immediately show the recent projects for selected tank type
            LoadRecentProjects();
            DisplayRecentProjects(recentProjectsPanel);
            UpdateBackgroundImages();
        }

        #region Tank Type Selection UI

        private Panel CreateTankTypeSelectionPanel()
        {
            // Set 1.5x larger base dimensions for visibility
            int width = (int)(900 );   // ~1350px
            int height = (int)(400);  // ~600px

            var panel = new Panel
            {
                Size = new Size(width, height),
                BackColor = ColorTranslator.FromHtml("#292C36"),
                Anchor = AnchorStyles.None,
                Dock = DockStyle.None,
                Location = new Point(
                    (Screen.PrimaryScreen.WorkingArea.Width - width) / 2,
                    (Screen.PrimaryScreen.WorkingArea.Height - height) / 2
                )
            };

            // Use TableLayoutPanel for vertical centering and separation
            var outerLayout = new TableLayoutPanel
            {
                RowCount = 3,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(40, 30, 40, 30)
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            panel.Controls.Add(outerLayout);

            // --- Title ---
            var title = new Label
            {
                Text = "Select Tank Type",
                Font = new Font("Segoe UI", 38, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0)
            };
            outerLayout.Controls.Add(title, 0, 0);

            // --- Button Layout ---
            var btnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(30, 0, 30, 0)
            };
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            outerLayout.Controls.Add(btnLayout, 0, 1);

            // --- Single Pedestal Button ---
            var btnSingle = new Button
            {
                Text = "Single Column\n\n🗼",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                Size = new Size(260, 220),
                BackColor = Color.FromArgb(75, 156, 211),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(10, 20, 10, 20),
                Anchor = AnchorStyles.None
            };
            btnSingle.FlatAppearance.BorderSize = 0;
            btnSingle.Click += (s, e) => { OnTankTypeSelected(TankType.SingleColumn); };
            btnLayout.Controls.Add(btnSingle, 0, 0);

            // --- Multi Column Button ---
            var btnMulti = new Button
            {
                Text = "Multi-Leg\n\n🏗️",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                Size = new Size(260, 220),
                BackColor = Color.FromArgb(120, 89, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(10, 20, 10, 20),
                Anchor = AnchorStyles.None
            };
            btnMulti.FlatAppearance.BorderSize = 0;
            btnMulti.Click += (s, e) => { OnTankTypeSelected(TankType.MultiColumn); };
            btnLayout.Controls.Add(btnMulti, 1, 0);

            // Optional footer (can be empty, or copyright etc)
            var footer = new Label
            {
                Text = "© 2024 SDSU - Iron Maguire. All Rights Reserved.",
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0)
            };
            outerLayout.Controls.Add(footer, 0, 2);

            // Center the panel on form startup
            panel.Left = (this.ClientSize.Width - panel.Width) / 2;
            panel.Top = (this.ClientSize.Height - panel.Height) / 2;

            return panel;
        }


        private void OnTankTypeSelected(TankType type)
        {
            _selectedTankType = type;
            AppState.CurrentTankType = type;
            if (type == TankType.MultiColumn && AppState.NoOfColumns <= 1)
                AppState.NoOfColumns = 4;
            // Load projects for this type
            LoadRecentProjects();
            DisplayRecentProjects(recentProjectsPanel);
            // Show main panel, hide selection panel
            tankTypeSelectionPanel.Visible = false;
            mainStartupPanel.Visible = true;
            mainStartupPanel.BringToFront();
        }

        #endregion

        #region Main Startup Panel UI




        private Panel CreateMainStartupPanel()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainPanel.Controls.Add(mainLayout);

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                //backGroundImageRight = Properties.Resources.Katy;
                backGroundImageRight = Properties.Resources.Sheldon_IA_New_Tank_Paint_2;

                backGroundImageLeft = Properties.Resources.SIRWA_New_Tank_No;
            }
            else
            {
                backGroundImageRight = Properties.Resources.Sheldon_IA_New_Tank_Paint_2;
                backGroundImageLeft = Properties.Resources.Tea__SD_9;
            }

            // Recent Projects
            recentProjectsPanel = new DoubleBufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                BackgroundImage = backGroundImageLeft,
                BackgroundImageLayout = ImageLayout.Stretch,
                WrapContents = false
            };
            mainLayout.Controls.Add(recentProjectsPanel, 0, 0);

            // Copyright
            Panel copyrightPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.Transparent
            };
            Label copyrightText = new Label
            {
                Text = "© 2024 SDSU - Iron Maguire. All Rights Reserved.\n",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };
            copyrightPanel.Controls.Add(copyrightText);
            recentProjectsPanel.Controls.Add(copyrightPanel);



                // Buttons (Right Panel)
                buttonPanel = new DoubleBufferedFlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    FlowDirection = FlowDirection.TopDown,
                    BackgroundImage = backGroundImageRight,
                    BackgroundImageLayout = ImageLayout.Stretch,
                    AutoSize = true,
                    WrapContents = false
                };
            mainLayout.Controls.Add(buttonPanel, 1, 0);

            Label headerLabel = new Label
            {
                Text = "Get Started",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            buttonPanel.Controls.Add(headerLabel);

            Button openProjectButton = CreateStyledButton("Open a Project");
            openProjectButton.Click += OpenProjectButton_Click;
            buttonPanel.Controls.Add(openProjectButton);

            Button newProjectButton = CreateStyledButton("Create New Project");
            newProjectButton.Click += NewProjectButton_Click;
            buttonPanel.Controls.Add(newProjectButton);

            // Back button to reselect type
            Button backBtn = CreateStyledButton("← Change Tank Type");
            backBtn.Click +=  ChangeTankType_Click;
            buttonPanel.Controls.Add(backBtn);

            return mainPanel;
        }

        // Opens the modal selector again and refreshes the UI
        private void ChangeTankType_Click(object sender, EventArgs e)
        {
            using (var dlg = new TankTypeSelectionForm())
            {
                // if user picked something and AppState was updated inside the dialog
                if (dlg.ShowDialog() == DialogResult.OK &&
                    AppState.CurrentTankType != TankType.None)
                {
                    _selectedTankType = AppState.CurrentTankType;

                    // reload recent list + background images
                    LoadRecentProjects();
                    UpdateBackgroundImages();            // see step 3
                    DisplayRecentProjects(recentProjectsPanel);
                }
            }
        }

        private void UpdateBackgroundImages()
        {
            if (_selectedTankType == TankType.MultiColumn)
            {
                backGroundImageRight = Properties.Resources.Sheldon_IA_New_Tank_Paint_2;
                backGroundImageLeft = Properties.Resources.SIRWA_New_Tank_No;
            }
            else            // SingleColumn
            {
                backGroundImageRight = Properties.Resources.Sheldon_IA_New_Tank_Paint_2;
                backGroundImageLeft = Properties.Resources.Tea__SD_9;
            }

            // apply to panels (may already be null in ctor)
            if (recentProjectsPanel != null) recentProjectsPanel.BackgroundImage = backGroundImageLeft;
            if (buttonPanel != null) buttonPanel.BackgroundImage = backGroundImageRight;
        }



        #endregion

        #region Recent Projects Filtering & Display

        private void LoadRecentProjects()
        {
            string recentProjectsFile = "recent_projects.json";
            if (File.Exists(recentProjectsFile))
            {
                try
                {
                    string json = File.ReadAllText(recentProjectsFile);
                    recentProjects = JsonSerializer.Deserialize<List<ProjectMetadata>>(json) ?? new List<ProjectMetadata>();
                }
                catch
                {
                    recentProjects = new List<ProjectMetadata>();
                }
            }
        }

        private void DisplayRecentProjects(FlowLayoutPanel panel)
        {
            panel.Controls.Clear();
            Label lbl = new Label
            {
                Text = $"Open Recent ({(_selectedTankType == TankType.SingleColumn ? "Single Column" : "Multi-Column")})",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(2, 5, 0, 5)
            };
            panel.Controls.Add(lbl);

            foreach (var proj in recentProjects.Where(p => p.Type == _selectedTankType))
            {
                if (!File.Exists(proj.ProjectFilePath))
                    continue;
                Button projectButton = new Button
                {
                    Text = Path.GetFileNameWithoutExtension(proj.ProjectFilePath),
                    Font = new Font("Segoe UI", 10),
                    Width = 850,
                    Height = 50,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Popup,
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5),
                    Margin = new Padding(0, 5, 0, 5)
                };
                projectButton.MouseEnter += (s, e) => { projectButton.BackColor = Color.FromArgb(78, 104, 132); };
                projectButton.MouseLeave += (s, e) => { projectButton.BackColor = Color.Transparent; };
                projectButton.Click += async (s, e) => { await OpenProject(proj.ProjectFilePath, proj.Type); };

                panel.Controls.Add(projectButton);
            }
        }

        #endregion

        private Button CreateStyledButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 12),
                Size = new Size(570, 50),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Popup,
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 5, 0, 5)
            };
            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += (s, e) => { button.BackColor = Color.FromArgb(78, 104, 132); };
            button.MouseLeave += (s, e) => { button.BackColor = Color.Transparent; };
            return button;
        }

        #region Project Open/Create

        private void OpenProjectButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Project Files (*.proj)|*.proj";
                openFileDialog.Title = "Open Existing Project";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string projectPath = openFileDialog.FileName;
                    // Check metadata for type
                    TankType fileType = GetProjectTypeFromMetadata(projectPath);
                    if (fileType != _selectedTankType)
                    {
                        MessageBox.Show("Selected project does not match chosen tank type.", "Type Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _ = OpenProject(projectPath, fileType);
                }
            }
        }

        private void NewProjectButton_Click(object sender, EventArgs e)
        {
            CreateNewProject();
        }

        private void CreateNewProject()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolderPath = folderDialog.SelectedPath;

                    if (Directory.Exists(selectedFolderPath) && Directory.EnumerateFileSystemEntries(selectedFolderPath).Any())
                    {
                        MessageBox.Show("The selected folder is not empty. Choose an empty folder.",
                                        "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string projectName = Prompt.ShowDialog("Enter Workspace Folder Name", "Create New Project");
                    if (string.IsNullOrEmpty(projectName))
                    {
                        MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string projectFolderPath = Path.Combine(selectedFolderPath, projectName);
                    Directory.CreateDirectory(projectFolderPath);

                    string projectFilePath = Path.Combine(projectFolderPath, $"{projectName}.proj");
                    File.WriteAllText(projectFilePath, "Default project content.");

                    // -- Save metadata --
                    var metadata = new ProjectMetadata
                    {
                        ProjectName = projectName,
                        Type = _selectedTankType,
                        ProjectFilePath = projectFilePath
                    };
                    string metaPath = Path.Combine(projectFolderPath, "project.json");
                    File.WriteAllText(metaPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

                    InitializeProjectDatabase(projectFolderPath);
                    AddToRecentProjects(metadata);

                    _ = OpenProject(projectFilePath, _selectedTankType); // Automatically open after creation
                }
            }
        }

        private void AddToRecentProjects(ProjectMetadata metadata)
        {
            if (!recentProjects.Any(p => p.ProjectFilePath == metadata.ProjectFilePath))
            {
                recentProjects.Insert(0, metadata);
                string json = JsonSerializer.Serialize(recentProjects, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("recent_projects.json", json);
            }
        }

        private TankType GetProjectTypeFromMetadata(string projectPath)
        {
            string metaPath = Path.Combine(Path.GetDirectoryName(projectPath), "project.json");
            if (!File.Exists(metaPath)) return TankType.None;
            try
            {
                var md = JsonSerializer.Deserialize<ProjectMetadata>(File.ReadAllText(metaPath));
                return md?.Type ?? TankType.None;
            }
            catch { return TankType.None; }
        }

        public async Task OpenProject(string projectPath, TankType tankType)
        {
            using (LoadingWindow loading = new LoadingWindow())
            {
                loading.Show();
                Application.DoEvents();

                try
                {
                    await Task.Run(() =>
                    {
                        string dbFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projectPath), "project_data.db");
                        string connectionString = $"Data Source={dbFilePath};";
                        //EnsureDatabaseSchema(dbFilePath);
                        var dbContext = new WaterTankDbContext(connectionString);
                        dbContext.EnsureDatabaseCreated();
                        _diContainer.Register<WaterTankDbContext>(dbContext);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    loading.Close();
                }
            }

            // Open main window based on type
            this.Hide();
            var mainForm = new WaterTank(this); // tankType is either SingleColumn or MultiColumn
            mainForm.Show();
        }

        #endregion

        // ---- DB setup and Prompt unchanged below ----

        private void InitializeProjectDatabase(string projectFolderPath)
        {
            string dbFilePath = Path.Combine(projectFolderPath, "project_data.db");
            if (File.Exists(dbFilePath)) File.Delete(dbFilePath);
            using (var connection = new System.Data.SQLite.SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
            {
                connection.Open();
                string createTableSQL = @"
                    CREATE TABLE IF NOT EXISTS SegmentProperties (
                        SegmentNumber INTEGER PRIMARY KEY AUTOINCREMENT,
                        SegmentName TEXT NOT NULL,
                        SegmentType TEXT NOT NULL,
                        Diameter REAL CHECK(Diameter >= 0),
                        Thickness REAL CHECK(Thickness >= 0),
                        HeightInitial REAL CHECK(HeightInitial >= 0),
                        HeightFinal REAL CHECK(HeightFinal >= 0),
                        DiameterInitial REAL CHECK(DiameterInitial >= 0),
                        DiameterFinal REAL CHECK(DiameterFinal >= 0)
                    );
                    CREATE TABLE IF NOT EXISTS MaterialProperties (
                        MaterialNumber INTEGER PRIMARY KEY AUTOINCREMENT,
                        MaterialName TEXT NOT NULL,
                        MaterialType TEXT NOT NULL,
                        Density INTEGER NOT NULL CHECK(Density >= 0),
                        ModulusOfElasticity INTEGER NOT NULL CHECK(ModulusOfElasticity >= 0),
                        TensileYieldStress INTEGER NOT NULL CHECK(TensileYieldStress >= 0),
                        TensileUltimateStress INTEGER NOT NULL CHECK(TensileUltimateStress >= 0)
                    );
                    CREATE TABLE IF NOT EXISTS TankProperties (
                        TankNumber INTEGER PRIMARY KEY AUTOINCREMENT,
                        Capacity TEXT,
                        WeightOfWater TEXT,
                        WeightOfSteel TEXT,
                        TotalWeight TEXT,
                        ProjectedArea TEXT,
                        Centroid TEXT
                    );
                    CREATE TABLE IF NOT EXISTS WindLoadEntity (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Exposure TEXT NOT NULL,
                        Kzt REAL NOT NULL CHECK(Kzt >= 0),
                        Ke REAL NOT NULL CHECK(Ke >= 0),
                        Kd REAL NOT NULL CHECK(Kd >= 0),
                        G REAL NOT NULL CHECK(G >= 0),
                        I REAL NOT NULL CHECK(I >= 0),
                        V REAL NOT NULL CHECK(V >= 0),
                        Zg REAL NOT NULL CHECK(Zg >= 0),
                        alpha REAL NOT NULL CHECK(alpha >= 0),
                        lambda REAL NOT NULL CHECK(lambda >= 0),
                        Cf REAL NOT NULL CHECK(Cf >= 0),
                        Q REAL NOT NULL CHECK(Q >= 0)
                    );
                    CREATE TABLE IF NOT EXISTS SeismicLoadEntity (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Ss REAL NOT NULL CHECK(Ss >= 0),
                        S1 REAL NOT NULL CHECK(S1 >= 0),
                        SiteClass TEXT NOT NULL,
                        Fa REAL NOT NULL CHECK(Fa >= 0),
                        Fv REAL NOT NULL CHECK(Fv >= 0),
                        Sds REAL NOT NULL CHECK(Sds >= 0),
                        Sd1 REAL NOT NULL CHECK(Sd1 >= 0),
                        Ri REAL NOT NULL CHECK(Ri >= 0),
                        Ie REAL NOT NULL CHECK(Ie >= 0),
                        Tl REAL NOT NULL CHECK(Tl >= 0),
                        Ti REAL NOT NULL CHECK(Ti >= 0),
                        Ts REAL NOT NULL CHECK(Ts >= 0),
                        Sa REAL NOT NULL CHECK(Sa >= 0),
                        Lambda REAL NOT NULL CHECK(Lambda >= 0),
                        Ai REAL NOT NULL CHECK(Ai >= 0),
                        V REAL NOT NULL CHECK(V >= 0)

                    );
                    CREATE TABLE IF NOT EXISTS LiveLoadEntity (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Live_Load REAL NOT NULL CHECK(Live_Load >= 0),
                        Roof_Live_Load REAL NOT NULL CHECK(Roof_Live_Load >= 0),
                        Design_Roof_Live_Load REAL NOT NULL CHECK(Design_Roof_Live_Load >= 0)

                    );
                   CREATE TABLE IF NOT EXISTS DeadLoadEntity (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Miscellaneous_Load REAL NOT NULL CHECK(Miscellaneous_Load >= 0)

                    );
                    CREATE TABLE IF NOT EXISTS SnowLoadEntity (
                        Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                        HeightToConsider    REAL    NOT NULL CHECK(HeightToConsider    >= 0),
                        GroundSnowLoad      REAL    NOT NULL CHECK(GroundSnowLoad      >= 0),
                        RiskCategory        TEXT    NOT NULL,
                        ImportanceFactor    REAL    NOT NULL CHECK(ImportanceFactor    >= 0),
                        Exposure            TEXT    NOT NULL,
                        ExposureFactor      REAL    NOT NULL CHECK(ExposureFactor      >= 0),
                        AreaSubjectedToSnow REAL    NOT NULL CHECK(AreaSubjectedToSnow >= 0),
                        TotalSnowLoad       REAL    NOT NULL CHECK(TotalSnowLoad       >= 0)
                    );
                    
                    CREATE TABLE IF NOT EXISTS AnchorBoltEntity (
                        Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nb      INTEGER NOT NULL CHECK(Nb >= 0),
                        Db      REAL    NOT NULL CHECK(Db >= 0),
                        Dh      REAL    NOT NULL CHECK(Dh >= 0),
                        Rb      REAL    NOT NULL CHECK(Rb >= 0),
                        Ab      REAL    NOT NULL,
                        ThetaSeg REAL,
                        Ns      INTEGER,
                        Tbp     REAL    NOT NULL CHECK(Tbp >= 0),
                        Fy      REAL,
                        Fu      REAL,
                        Tu      REAL    NOT NULL CHECK(Tu >= 0),
                        Vu      REAL    NOT NULL CHECK(Vu >= 0),
                        Mu      REAL,
                        FcPrime REAL,
                        Hef     REAL,
                        Phi     REAL,
                        E       REAL,
                        S       REAL,
                        Nbs     INTEGER,
                        DistributionMethod TEXT,
                        PedestalSize REAL,
                        BoltSpacing REAL,
                        WasherSize REAL,
                        Dcone REAL,
                        Pu REAL
                    );

                    CREATE TABLE IF NOT EXISTS BasePlateEntity (
                        Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                        Dbp     REAL    NOT NULL CHECK(Dbp >= 0),
                        Ro      REAL    NOT NULL CHECK(Ro >= 0),
                        Ri      REAL    NOT NULL CHECK(Ri >= 0),
                        Theta   REAL    NOT NULL,
                        T       REAL    NOT NULL CHECK(T >= 0),
                        N       INTEGER NOT NULL CHECK(N >= 0),
                        Rs      REAL    NOT NULL CHECK(Rs >= 0),
                        Nh      INTEGER NOT NULL CHECK(Nh >= 0),
                        Dh      REAL    NOT NULL CHECK(Dh >= 0),
                        A       REAL,
                        Rb      REAL
                    );
                ";

                using (var command = new System.Data.SQLite.SQLiteCommand(createTableSQL, connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }
      
        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                Form prompt = new Form()
                {
                    Width = 400,
                    Height = 220,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = ColorTranslator.FromHtml("#F9F9F9"),
                    Font = new Font("Segoe UI", 10)
                };
                Label textLabel = new Label()
                {
                    Left = 20,
                    Top = 20,
                    Text = text,
                    AutoSize = true,
                    ForeColor = ColorTranslator.FromHtml("#383D46")
                };
                TextBox inputBox = new TextBox()
                {
                    Left = 20,
                    Top = 60,
                    Width = 340,
                    Font = new Font("Segoe UI", 10),
                    BackColor = ColorTranslator.FromHtml("#E5E5E5"),
                    ForeColor = ColorTranslator.FromHtml("#383D46"),
                    BorderStyle = BorderStyle.FixedSingle
                };
                Button confirmation = new Button()
                {
                    Text = "OK",
                    Left = 280,
                    Width = 90,
                    Height = 40,
                    Top = 110,
                    DialogResult = DialogResult.OK,
                    BackColor = ColorTranslator.FromHtml("#2A2D34"),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                confirmation.FlatAppearance.BorderSize = 0;
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;
                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : "";
            }
        }

        private void StartupForm_Load(object sender, EventArgs e) { }
        private void StartupForm_FormClosing(object sender, FormClosingEventArgs e) { }
    }
}
