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

        // --- Multi-Leg Pedestal Properties (DO NOT TOUCH FOR SINGLE-LEG) ---

        public double? FcPrime { get; set; }
        public double? Fy { get; set; }
        public double? Hef { get; set; }
        public double? Hp { get; set; }
        public double? GammaC { get; set; }
        public double? PedestalSizeB { get; set; }
        public double? PedestalSizeL { get; set; }

        public double? PedestalAsProv { get; set; } // e.g. 7.92
        public double? FootingAsProv { get; set; }  // Provided development length l_prov (in), e.g. 30.0
        public double? PhiCompression { get; set; } // 0.65 (Tied) or 0.75 (Spirals)

        // --- Single-Leg Annular Ring Wall Properties ---

        public double? ScPu { get; set; }            // Vertical load on foundation P (kips), e.g. 1650.2553
        public double? ScMu { get; set; }            // Governing overturning moment Mu (kip-ft), e.g. 4847.67
        public double? Hw { get; set; }              // Ringwall height hw (ft), e.g. 11.5
        public double? TotalUpliftT { get; set; }    // Total uplift force T (kips), e.g. 344.54
        public double? ScFcPrime { get; set; }       // Concrete compressive strength f'c (psi), e.g. 4000
        public double? ScFy { get; set; }            // Steel yield strength fy (ksi), e.g. 60
        public double? ScPhi { get; set; }           // Strength reduction factor phi, e.g. 0.90
        public double? ScGammaC { get; set; }        // Concrete unit weight (pcf), e.g. 150
        public double? ScGammaS { get; set; }        // Steel unit weight (pcf), e.g. 490
        public double? HoopBarArea { get; set; }     // Hoop bar area Ab (in2), e.g. 0.31 (#5)
        public double? HoopSpacingProv { get; set; } // Provided hoop spacing s (in), e.g. 6.0
        public string? HoopBarLabel { get; set; }    // e.g. "#5"
        public string? VertRebarCallout { get; set; }// e.g. "#6 @ 12\" EF"
    }
}
