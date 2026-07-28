using Microsoft.EntityFrameworkCore;
using WaterTankTool_WFA.Entity;
using System;
using System.Data;
using System.Linq;

public class WaterTankDbContext : DbContext
{
    private static WaterTankDbContext _instance;
    private static readonly object _lock = new object();
    //private static string _defaultConnectionString = "Data Source=default_project_path\\project_data.db"; // Set this to a default path

    private static string _defaultConnectionString;

    public DbSet<SegmentProperties> SegmentProperties { get; set; }
    public DbSet<MaterialProperties> MaterialProperties { get; set; }
    public DbSet<TankProperties> TankProperties { get; set; }

    public DbSet<WindLoadEntity> WindLoadEntity {  get; set; }

    public DbSet<LiveLoadEntity> LiveLoadEntity { get; set; }
    public DbSet<SnowLoadEntity> SnowLoadEntity { get; set; }
    public DbSet<SeismicLoadEntity> SeismicLoadEntity { get; set; }   

    public DbSet<DeadLoadEntity> DeadLoadEntity { get; set; }


    //Foundations
    public DbSet<AnchorBoltEntity> AnchorBoltEntity { get; set; }

    public DbSet<BasePlateEntity> BasePlateEntity { get; set; }

    // Spheroid Tank Components
    public DbSet<TransitionsEntity> TransitionsEntity { get; set; }
    public DbSet<KnuckleEntity> KnuckleEntity { get; set; }
    public DbSet<RoofFingerEntity> RoofFingerEntity { get; set; }
    public DbSet<ReducerConeEntity> ReducerConeEntity { get; set; }
    public DbSet<DrywellEntity> DrywellEntity { get; set; }


    // Private constructor to prevent direct instantiation

    public WaterTankDbContext() : base() { }
    public WaterTankDbContext(string connectionString)
    {
        _defaultConnectionString = connectionString;
    }


    public static WaterTankDbContext GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new WaterTankDbContext(_defaultConnectionString);
                }
            }
        }
        return _instance;
    }

    public static void SetConnectionString(string connectionString)
    {
        lock (_lock)
        {
            _defaultConnectionString = connectionString;
            _instance = null; // Reset the instance so it uses the new connection string on the next call
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(_defaultConnectionString);
        }
    }

    public void EnsureDatabaseCreated()
    {
        try
        {
            Database.EnsureCreated();
            UpdateBasePlateSchema();
            UpdateAnchorBoltSchema();
            UpdateSpheroidSchema();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ensuring database is created: {ex.Message}");
        }
    }

    private void UpdateSpheroidSchema()
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                string[] commands = new string[]
                {
                    "CREATE TABLE IF NOT EXISTS TransitionsEntity (Id INTEGER PRIMARY KEY AUTOINCREMENT, TransitionNumber INTEGER NOT NULL, OutsideRadius REAL NOT NULL, LowerRadius REAL NOT NULL, UpperRadius REAL NOT NULL, Height REAL NOT NULL, Thickness REAL NOT NULL, SegmentDegree REAL NOT NULL, Quantity INTEGER NOT NULL);",
                    "CREATE TABLE IF NOT EXISTS KnuckleEntity (Id INTEGER PRIMARY KEY AUTOINCREMENT, KnuckleType TEXT NULL, Thickness REAL NOT NULL, LowerRadius REAL NOT NULL, CenterHeight REAL NOT NULL, UpperStartRadius REAL NOT NULL, UpperExtendRadius REAL NOT NULL, ExtendRadius REAL NOT NULL, StartDegree REAL NOT NULL, EndDegree REAL NOT NULL, SectionRadius REAL NOT NULL, Quantity INTEGER NOT NULL, ExtraDimension REAL NOT NULL, Diameter REAL NOT NULL);",
                    "CREATE TABLE IF NOT EXISTS RoofFingerEntity (Id INTEGER PRIMARY KEY AUTOINCREMENT, Thickness REAL NOT NULL, Radius REAL NOT NULL, Quantity INTEGER NOT NULL, SegmentDegree REAL NOT NULL, EndDegree REAL NOT NULL, ExtraDimension REAL NOT NULL, Diameter REAL NOT NULL);",
                    "CREATE TABLE IF NOT EXISTS ReducerConeEntity (Id INTEGER PRIMARY KEY AUTOINCREMENT, LowerRadius REAL NOT NULL, UpperRadius REAL NOT NULL, Height REAL NOT NULL, Thickness REAL NOT NULL, Quantity INTEGER NOT NULL, BottomRingInsideRadius REAL NOT NULL, BottomRingOutsideRadius REAL NOT NULL, BottomRingThickness REAL NOT NULL, BottomRingDegree REAL NOT NULL, BottomRingQuantity INTEGER NOT NULL, TopRingInsideRadius REAL NOT NULL, TopRingOutsideRadius REAL NOT NULL, TopRingThickness REAL NOT NULL);",
                    "CREATE TABLE IF NOT EXISTS DrywellEntity (Id INTEGER PRIMARY KEY AUTOINCREMENT, LowerDiameter REAL NOT NULL, LowerHeight REAL NOT NULL, LowerThickness REAL NOT NULL, UpperDiameter REAL NOT NULL, UpperHeight REAL NOT NULL, UpperThickness REAL NOT NULL, StiffenerOutsideRadius REAL NOT NULL, StiffenerInsideRadius REAL NOT NULL, StiffenerThickness REAL NOT NULL, StiffenerQuantity INTEGER NOT NULL);"
                };

                foreach (var cmd in commands)
                {
                    try
                    {
                        command.CommandText = cmd;
                        command.ExecuteNonQuery();
                    }
                    catch { /* Table might already exist or other harmless error */ }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Spheroid schema: {ex.Message}");
        }
    }

    private void UpdateAnchorBoltSchema()
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                string[] columns = new string[]
                {
                    "PedestalSize", "BoltSpacing", "WasherSize", "Dcone", "Pu"
                };

                foreach (var col in columns)
                {
                    try
                    {
                        command.CommandText = $"ALTER TABLE AnchorBoltEntity ADD COLUMN {col} REAL NULL;";
                        command.ExecuteNonQuery();
                    }
                    catch { /* Column probably already exists */ }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating AnchorBoltEntity schema: {ex.Message}");
        }
    }

    private void UpdateBasePlateSchema()
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                // List of columns to add if they don't exist
                string[] columns = new string[]
                {
                    "Fy", "Fc_prime", "A2", "Pu", "ShellRadius", "OverturningMoment",
                    "Fp", "Phi_Pp", "BearingUtilization",
                    "L", "Mu", "T_req", "ThicknessUtilization", "Wrw"
                };

                foreach (var col in columns)
                {
                    try
                    {
                        command.CommandText = $"ALTER TABLE BasePlateEntity ADD COLUMN {col} REAL NULL;";
                        command.ExecuteNonQuery();
                    }
                    catch { /* Column probably already exists */ }
                }

                try
                {
                    command.CommandText = "ALTER TABLE BasePlateEntity ADD COLUMN Nb INTEGER NULL;";
                    command.ExecuteNonQuery();
                }
                catch { /* Column probably already exists */ }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating BasePlateEntity schema: {ex.Message}");
        }
    }
}
