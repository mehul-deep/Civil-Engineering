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
        public double? BottomRebarArea { get; set; } // in2
        public double? TopRebarArea { get; set; } // in2
        public double? RebarDiameter { get; set; } // in (e.g. 1.0 for #8)
    }
}
