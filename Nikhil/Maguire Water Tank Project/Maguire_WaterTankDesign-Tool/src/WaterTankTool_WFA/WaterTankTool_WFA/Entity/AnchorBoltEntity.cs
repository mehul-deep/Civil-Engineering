using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterTankTool_WFA.Entity
{
    public class AnchorBoltEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Nb = total number of anchor bolts
        public int Nb { get; set; }

        // db = nominal anchor bolt diameter (in)
        public double Db { get; set; }

        // dh = anchor bolt hole diameter (in)
        public double Dh { get; set; }

        // rb = anchor bolt circle radius (ft or in)
        public double Rb { get; set; }

        // αb = start angle of first anchor bolt (deg)
        public double Ab { get; set; }

        // θseg = segment angle (deg), if bolts are checked by segment
        public double? ThetaSeg { get; set; }

        // ns = number of segments
        public int? Ns { get; set; }

        // tbp = base plate thickness (in)
        public double Tbp { get; set; }

        // fy = bolt yield strength, if needed
        public double? Fy { get; set; }

        // fu = bolt ultimate strength, if needed
        public double? Fu { get; set; }

        // Tu = design uplift/tension demand on one bolt or bolt group
        public double Tu { get; set; }

        // Vu = design shear demand on one bolt or bolt group
        public double Vu { get; set; }

        // Mu = Governing Overturning Moment (kip-ft)
        public double? Mu { get; set; }

        // Concrete Properties
        public double? FcPrime { get; set; } // Concrete Compressive Strength (psi)
        public double? Hef { get; set; } // Effective Embedment Depth (in)

        // φ = resistance factor, if your software includes strength check
        public double? Phi { get; set; }

        // e = edge distance from hole to plate edge, if required
        public double? E { get; set; }

        // s = bolt spacing, if user enters directly
        public double? S { get; set; }

        // nbs = number of bolts per segment
        public int? Nbs { get; set; }

        public string? DistributionMethod { get; set; }

        // Multi-Column Specific Fields
        public double? PedestalSize { get; set; } // B x L (in) - assuming square so a single value
        public double? BoltSpacing { get; set; } // b x l (in) - assuming square so a single value
        public double? WasherSize { get; set; } // square plate washer side length (in)
        public double? Dcone { get; set; } // Cone Diameter
        public double? Pu { get; set; } // Factored axial load
    }
}
