using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class FootingEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public double? FootingSizeB { get; set; } // ft
        public double? FootingSizeL { get; set; } // ft
        public double? FootingThickness { get; set; } // in
        public double? ConcreteCover { get; set; } // in
        public double? Qallow { get; set; } // ksf
        public double? FrictionCoeff { get; set; }
        public double? BottomRebarArea { get; set; } // in2/ft
        public double? TopRebarArea { get; set; } // Provided Dowel As (in2)
        public double? RebarDiameter { get; set; } // Bottom bar diameter in (e.g. 1.0 for #8)

        public double? PedestalHeightHp { get; set; } // ft
        public double? FcPrime { get; set; } // ksi
        public double? Fy { get; set; } // ksi
        public double? AlphaS { get; set; } // 40 (Interior), 30 (Edge), 20 (Corner)
        public double? DowelDiameter { get; set; } // in (e.g. 0.75 for #6, 1.0 for #8)
    }
}
