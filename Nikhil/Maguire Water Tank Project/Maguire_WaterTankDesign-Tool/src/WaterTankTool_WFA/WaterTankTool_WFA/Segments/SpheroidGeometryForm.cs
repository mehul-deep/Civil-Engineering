using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Segments
{
    public class SpheroidGeometryForm : Form
    {
        private TabControl tabControl;
        
        private DataGridView dgvTransitions;
        private DataGridView dgvKnuckles;
        private PropertyGrid pgRoofFinger;
        private PropertyGrid pgReducerCone;
        private PropertyGrid pgDrywell;
        
        private Button btnSave;
        
        private WaterTankDbContext context;

        public SpheroidGeometryForm()
        {
            context = WaterTankDbContext.GetInstance();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Spheroid Geometry Parameters";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            tabControl = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 5) };
            
            // Tab 1: Transitions
            TabPage tabTransitions = new TabPage("Transitions");
            dgvTransitions = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            tabTransitions.Controls.Add(dgvTransitions);
            tabControl.TabPages.Add(tabTransitions);
            
            // Tab 2: Knuckles
            TabPage tabKnuckles = new TabPage("Knuckles");
            dgvKnuckles = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            tabKnuckles.Controls.Add(dgvKnuckles);
            tabControl.TabPages.Add(tabKnuckles);
            
            // Tab 3: Roof Fingers
            TabPage tabRoof = new TabPage("Roof Fingers");
            pgRoofFinger = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.NoSort };
            tabRoof.Controls.Add(pgRoofFinger);
            tabControl.TabPages.Add(tabRoof);
            
            // Tab 4: Reducer Cone
            TabPage tabReducer = new TabPage("Reducer Cone");
            pgReducerCone = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.NoSort };
            tabReducer.Controls.Add(pgReducerCone);
            tabControl.TabPages.Add(tabReducer);
            
            // Tab 5: Drywell
            TabPage tabDrywell = new TabPage("Drywell");
            pgDrywell = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.NoSort };
            tabDrywell.Controls.Add(pgDrywell);
            tabControl.TabPages.Add(tabDrywell);
            
            this.Controls.Add(tabControl);
            
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.WhiteSmoke };
            btnSave = new Button { 
                Text = "Save Dimensions", 
                Width = 140, 
                Height = 35, 
                Left = this.Width - 170, 
                Top = 7, 
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(75, 156, 211),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            bottomPanel.Controls.Add(btnSave);
            
            this.Controls.Add(bottomPanel);
        }
        
        private void LoadData()
        {
            context.TransitionsEntity.Load();
            if(!context.TransitionsEntity.Any()) {
                for(int i=1; i<=6; i++) {
                    context.TransitionsEntity.Add(new TransitionsEntity { TransitionNumber = i });
                }
                context.SaveChanges();
            }
            
            context.KnuckleEntity.Load();
            if(!context.KnuckleEntity.Any()) {
                context.KnuckleEntity.Add(new KnuckleEntity { KnuckleType = "KnuckleKnuckle" });
                context.KnuckleEntity.Add(new KnuckleEntity { KnuckleType = "BottomKnuckle" });
                context.KnuckleEntity.Add(new KnuckleEntity { KnuckleType = "TopKnuckle" });
                context.SaveChanges();
            }

            var roof = context.RoofFingerEntity.FirstOrDefault();
            if (roof == null) { roof = new RoofFingerEntity(); context.RoofFingerEntity.Add(roof); context.SaveChanges(); }
            
            var reducer = context.ReducerConeEntity.FirstOrDefault();
            if (reducer == null) { reducer = new ReducerConeEntity(); context.ReducerConeEntity.Add(reducer); context.SaveChanges(); }
            
            var drywell = context.DrywellEntity.FirstOrDefault();
            if (drywell == null) { drywell = new DrywellEntity(); context.DrywellEntity.Add(drywell); context.SaveChanges(); }

            dgvTransitions.DataSource = context.TransitionsEntity.Local.ToBindingList();
            dgvKnuckles.DataSource = context.KnuckleEntity.Local.ToBindingList();
            
            pgRoofFinger.SelectedObject = roof;
            pgReducerCone.SelectedObject = reducer;
            pgDrywell.SelectedObject = drywell;

            FormatGrid(dgvTransitions);
            FormatGrid(dgvKnuckles);
        }

        private void FormatGrid(DataGridView dgv)
        {
            dgv.RowHeadersVisible = false;
            dgv.BackgroundColor = Color.White;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Force PropertyGrid to commit current edit
                pgRoofFinger.Focus();
                
                context.SaveChanges();
                MessageBox.Show("Dimensions saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
