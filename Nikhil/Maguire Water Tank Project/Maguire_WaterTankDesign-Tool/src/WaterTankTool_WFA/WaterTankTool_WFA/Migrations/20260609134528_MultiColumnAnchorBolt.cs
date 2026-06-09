using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterTankTool_WFA.Migrations
{
    /// <inheritdoc />
    public partial class MultiColumnAnchorBolt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WindLoadEnitity",
                table: "WindLoadEnitity");

            migrationBuilder.RenameTable(
                name: "WindLoadEnitity",
                newName: "WindLoadEntity");

            migrationBuilder.AddColumn<string>(
                name: "Centroid",
                table: "TankProperties",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WindLoadEntity",
                table: "WindLoadEntity",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AnchorBoltEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nb = table.Column<int>(type: "INTEGER", nullable: false),
                    Db = table.Column<double>(type: "REAL", nullable: false),
                    Dh = table.Column<double>(type: "REAL", nullable: false),
                    Rb = table.Column<double>(type: "REAL", nullable: false),
                    Ab = table.Column<double>(type: "REAL", nullable: false),
                    ThetaSeg = table.Column<double>(type: "REAL", nullable: true),
                    Ns = table.Column<int>(type: "INTEGER", nullable: true),
                    Tbp = table.Column<double>(type: "REAL", nullable: false),
                    Fy = table.Column<double>(type: "REAL", nullable: true),
                    Fu = table.Column<double>(type: "REAL", nullable: true),
                    Tu = table.Column<double>(type: "REAL", nullable: false),
                    Vu = table.Column<double>(type: "REAL", nullable: false),
                    Mu = table.Column<double>(type: "REAL", nullable: true),
                    FcPrime = table.Column<double>(type: "REAL", nullable: true),
                    Hef = table.Column<double>(type: "REAL", nullable: true),
                    Phi = table.Column<double>(type: "REAL", nullable: true),
                    E = table.Column<double>(type: "REAL", nullable: true),
                    S = table.Column<double>(type: "REAL", nullable: true),
                    Nbs = table.Column<int>(type: "INTEGER", nullable: true),
                    DistributionMethod = table.Column<string>(type: "TEXT", nullable: true),
                    PedestalSize = table.Column<double>(type: "REAL", nullable: true),
                    BoltSpacing = table.Column<double>(type: "REAL", nullable: true),
                    WasherSize = table.Column<double>(type: "REAL", nullable: true),
                    Dcone = table.Column<double>(type: "REAL", nullable: true),
                    Pu = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnchorBoltEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BasePlateEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Dbp = table.Column<double>(type: "REAL", nullable: false),
                    Ro = table.Column<double>(type: "REAL", nullable: false),
                    Ri = table.Column<double>(type: "REAL", nullable: false),
                    Theta = table.Column<double>(type: "REAL", nullable: false),
                    T = table.Column<double>(type: "REAL", nullable: false),
                    N = table.Column<int>(type: "INTEGER", nullable: false),
                    Rs = table.Column<double>(type: "REAL", nullable: false),
                    Nh = table.Column<int>(type: "INTEGER", nullable: false),
                    Dh = table.Column<double>(type: "REAL", nullable: false),
                    A = table.Column<double>(type: "REAL", nullable: true),
                    Rb = table.Column<double>(type: "REAL", nullable: true),
                    Fy = table.Column<double>(type: "REAL", nullable: false),
                    Fc_prime = table.Column<double>(type: "REAL", nullable: false),
                    A2 = table.Column<double>(type: "REAL", nullable: true),
                    Pu = table.Column<double>(type: "REAL", nullable: true),
                    OverturningMoment = table.Column<double>(type: "REAL", nullable: true),
                    ShellRadius = table.Column<double>(type: "REAL", nullable: true),
                    Fp = table.Column<double>(type: "REAL", nullable: true),
                    Phi_Pp = table.Column<double>(type: "REAL", nullable: true),
                    BearingUtilization = table.Column<double>(type: "REAL", nullable: true),
                    L = table.Column<double>(type: "REAL", nullable: true),
                    Mu = table.Column<double>(type: "REAL", nullable: true),
                    T_req = table.Column<double>(type: "REAL", nullable: true),
                    ThicknessUtilization = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasePlateEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeadLoadEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Miscellaneous_Load = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLoadEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiveLoadEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Live_Load = table.Column<double>(type: "REAL", nullable: false),
                    Roof_Live_Load = table.Column<double>(type: "REAL", nullable: false),
                    Design_Roof_Live_Load = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveLoadEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeismicLoadEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ss = table.Column<double>(type: "REAL", nullable: false),
                    S1 = table.Column<double>(type: "REAL", nullable: false),
                    SiteClass = table.Column<string>(type: "TEXT", nullable: false),
                    Fa = table.Column<double>(type: "REAL", nullable: false),
                    Fv = table.Column<double>(type: "REAL", nullable: false),
                    Sds = table.Column<double>(type: "REAL", nullable: false),
                    Sd1 = table.Column<double>(type: "REAL", nullable: false),
                    Ri = table.Column<double>(type: "REAL", nullable: false),
                    Ie = table.Column<double>(type: "REAL", nullable: false),
                    Tl = table.Column<double>(type: "REAL", nullable: false),
                    Ti = table.Column<double>(type: "REAL", nullable: false),
                    Ts = table.Column<double>(type: "REAL", nullable: false),
                    Sa = table.Column<double>(type: "REAL", nullable: false),
                    Lambda = table.Column<double>(type: "REAL", nullable: false),
                    Ai = table.Column<double>(type: "REAL", nullable: false),
                    V = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeismicLoadEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnowLoadEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeightToConsider = table.Column<double>(type: "REAL", nullable: false),
                    GroundSnowLoad = table.Column<double>(type: "REAL", nullable: false),
                    RiskCategory = table.Column<string>(type: "TEXT", nullable: false),
                    ImportanceFactor = table.Column<double>(type: "REAL", nullable: false),
                    Exposure = table.Column<string>(type: "TEXT", nullable: false),
                    ExposureFactor = table.Column<double>(type: "REAL", nullable: false),
                    AreaSubjectedToSnow = table.Column<double>(type: "REAL", nullable: false),
                    TotalSnowLoad = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnowLoadEntity", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnchorBoltEntity");

            migrationBuilder.DropTable(
                name: "BasePlateEntity");

            migrationBuilder.DropTable(
                name: "DeadLoadEntity");

            migrationBuilder.DropTable(
                name: "LiveLoadEntity");

            migrationBuilder.DropTable(
                name: "SeismicLoadEntity");

            migrationBuilder.DropTable(
                name: "SnowLoadEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WindLoadEntity",
                table: "WindLoadEntity");

            migrationBuilder.DropColumn(
                name: "Centroid",
                table: "TankProperties");

            migrationBuilder.RenameTable(
                name: "WindLoadEntity",
                newName: "WindLoadEnitity");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WindLoadEnitity",
                table: "WindLoadEnitity",
                column: "Id");
        }
    }
}
