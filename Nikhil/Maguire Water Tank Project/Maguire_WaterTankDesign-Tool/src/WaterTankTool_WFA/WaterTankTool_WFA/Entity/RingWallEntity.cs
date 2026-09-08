using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class RingWallEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Ring wall thickness (ft)
        /// </summary>
        public double Trw { get; set; }

        /// <summary>
        /// Footing base width (ft)
        /// </summary>
        public double B { get; set; }

        /// <summary>
        /// Radius to foundation centerline (ft)
        /// </summary>
        public double Rcl { get; set; }

        /// <summary>
        /// Top of concrete elevation (ft)
        /// </summary>
        public double TCE { get; set; }

        /// <summary>
        /// Footing edge thickness (ft)
        /// </summary>
        public double Tedge { get; set; }

        /// <summary>
        /// Concrete cover to reinforcement (in)
        /// </summary>
        public double Cc { get; set; }

        /// <summary>
        /// Inner radius (ft)
        /// </summary>
        public double Rin { get; set; }

        /// <summary>
        /// Outer radius (ft)
        /// </summary>
        public double Rout { get; set; }

        /// <summary>
        /// Footing plan area (sq. ft)
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Ring wall centerline radius (ft)
        /// </summary>
        public double Rrw { get; set; }

        // --- Multi-Leg Pedestal Properties ---

        public double? FcPrime { get; set; }
        public double? Fy { get; set; }
        public double? Hef { get; set; }
        public double? Hp { get; set; }
        public double? GammaC { get; set; }
        public double? PedestalSizeB { get; set; }
        public double? PedestalSizeL { get; set; }

        public double? PedestalAsProv { get; set; } // e.g. 7.92
        public double? FootingAsProv { get; set; }  // e.g. 5.28
    }
}