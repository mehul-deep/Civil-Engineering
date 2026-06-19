using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WaterTankTool_WFA.Designer_Notes;

namespace WaterTankTool_WFA
{
    static class Program
    {
        public static DIContainer _diContainer;

        [DllImport("Shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [STAThread]
        static void Main()
        {
            try { SetProcessDpiAwareness(2); } catch { /* ignore */ }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load persisted AppState BEFORE anything uses it
            AppState.Load();

            // Save on normal app exit
            Application.ApplicationExit += (_, __) =>
            {
                try { AppState.Save(); } catch { }
            };

            _diContainer = new DIContainer();
            _diContainer.Register(new Services.LoadService());
            NotesManager.LoadNotes();

            // If no tank type saved yet, ask once
            //if (AppState.CurrentTankType == TankType.None)
            //{
                using (var dlg = new TankTypeSelectionForm())
                {
                    if (dlg.ShowDialog() != DialogResult.OK ||
                        AppState.CurrentTankType == TankType.None)
                    {
                        return; // user cancelled without selecting
                    }

                    // Persist immediately after the user chooses
                    AppState.Save();
                }
            //}

            try
            {
                Application.Run(new StartupForm(_diContainer));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Application crashed: " + ex);
            }
        }

        public static DIContainer GetContainer() => _diContainer;
    }
}
