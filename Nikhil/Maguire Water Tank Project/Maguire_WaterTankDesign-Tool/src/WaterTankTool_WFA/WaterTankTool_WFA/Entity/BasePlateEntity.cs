using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterTankTool_WFA.Entity
{
    public class BasePlateEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Dbp = base plate diameter (ft or in)
        public double Dbp { get; set; }

        // Ro = base plate outside radius
        public double Ro { get; set; }

        // Ri = base plate inside radius
        public double Ri { get; set; }

        // θ = base plate segment angle (deg)
        public double Theta { get; set; }

        // t = base plate thickness (in)
        public double T { get; set; }

        // n = number of base plate segments
        public int N { get; set; }

        // rs = steel unit weight = 490 pcf
        public double Rs { get; set; }

        // Nh = number of anchor bolt holes in one segment
        public int Nh { get; set; }

        // dh = diameter of anchor bolt hole
        public double Dh { get; set; }

        // α = start angle of segment (deg), if you want location in plan
        public double? A { get; set; }

        // rb = bolt circle radius, if bolt layout is needed
        public double? Rb { get; set; }

        // Wrw = width of ring wall (ft)
        public double? Wrw { get; set; }

        // Nb = total number of anchor bolts
        public int? Nb { get; set; }

        // --- Structural Design Parameters ---

        // Fy = steel yield strength (ksi)
        public double Fy { get; set; }

        // fc_prime = concrete compressive strength (psi)
        public double Fc_prime { get; set; }

        // A2 = supporting area of concrete (sq in)
        public double? A2 { get; set; }

        // Pu = factored axial load (kips)
        public double? Pu { get; set; }

        // OverturningMoment = Governing overturning moment (kip-ft)
        public double? OverturningMoment { get; set; }

        // ShellRadius = radius where the tank shell sits (ft)
        public double? ShellRadius { get; set; }

        // --- Structural Design Results ---

        // fp = bearing stress demand (ksi)
        public double? Fp { get; set; }

        // phi_Pp = design bearing strength (kips)
        public double? Phi_Pp { get; set; }

        // bearing_utilization = fp / capacity_ratio
        public double? BearingUtilization { get; set; }

        // l = cantilever length (in)
        public double? L { get; set; }

        // Mu = required flexural strength (kip-in/in)
        public double? Mu { get; set; }

        // t_req = required plate thickness (in)
        public double? T_req { get; set; }

        // thickness_utilization = t_req / t
        public double? ThicknessUtilization { get; set; }
    }
}
