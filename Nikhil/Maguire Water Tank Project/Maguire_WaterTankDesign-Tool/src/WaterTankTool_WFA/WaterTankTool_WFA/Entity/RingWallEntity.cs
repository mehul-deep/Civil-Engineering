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

        public double? ScPu { get; set; }            // Factored axial load Pu (kips), e.g. 1650.57 / 1653.556
        public double? ScMu { get; set; }            // Factored overturning moment Mu (kip-ft), e.g. 2903.6953
        public double? ScVu { get; set; }            // Factored shear Vu (kips), e.g. 53.4318
        public double? ScDcl { get; set; }           // Ring-wall centerline diameter D_CL (ft), e.g. 21.0
        public double? Hw { get; set; }              // Ringwall height Hw (ft), e.g. 9.0
        public double? ScBf { get; set; }            // Annular footing radial width Bf (ft), e.g. 6.5
        public double? ScHf { get; set; }            // Footing thickness hf (ft), e.g. 3.0
        public double? TotalUpliftT { get; set; }    // Legacy uplift force T (kips)
        public double? ScFcPrime { get; set; }       // Concrete compressive strength f'c (psi or ksi), e.g. 4000 psi / 4.0 ksi
        public double? ScFy { get; set; }            // Steel yield strength fy (ksi), e.g. 60
        public double? ScPhi { get; set; }           // Strength reduction factor phi, e.g. 0.90
        public double? ScGammaC { get; set; }        // Concrete unit weight (pcf), e.g. 150
        public double? ScGammaS { get; set; }        // Legacy steel unit weight (pcf), e.g. 490
        public double? ScQa { get; set; }            // Allowable net soil bearing pressure qa (ksf), e.g. 4.0
        public double? ScGammaSoil { get; set; }     // Soil unit weight gamma_s (pcf), e.g. 125
        public double? ScKa { get; set; }            // Active earth pressure coefficient Ka, e.g. 0.30
        public string? VertBarLabel { get; set; }    // Vertical rebar size label, e.g. "#4"
        public double? VertBarArea { get; set; }     // Vertical bar area Ab (in2), e.g. 0.20
        public int? VertBarCountPerFace { get; set; }// Vertical bars on each face, e.g. 66
        public double? HoopBarArea { get; set; }     // Horizontal hoop bar area Ab (in2), e.g. 0.20 (#4)
        public double? HoopSpacingProv { get; set; } // Provided hoop spacing sh (in), e.g. 13.5
        public string? HoopBarLabel { get; set; }    // Horizontal hoop bar label, e.g. "#4"
        public int? HoopBarCountPerFace { get; set; }// Horizontal hoop bars on each face, e.g. 8
        public string? VertRebarCallout { get; set; }// e.g. "66 #4 EF"
        public double? SlendernessK { get; set; }    // Effective length factor k (e.g. 1.0 or 2.0)
    }
}
