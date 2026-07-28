using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class ReducerConeEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public double LowerRadius { get; set; } // LR
        public double UpperRadius { get; set; } // UR
        public double Height { get; set; } // HT
        public double Thickness { get; set; } // THK
        public int Quantity { get; set; } // Q
        
        // Bottom Compression Ring
        public double BottomRingInsideRadius { get; set; } // BCRIR
        public double BottomRingOutsideRadius { get; set; } // BCROR
        public double BottomRingThickness { get; set; } // BCRTHK
        public double BottomRingDegree { get; set; } // BCRDEG
        public int BottomRingQuantity { get; set; } // BCRQ
        
        // Top Compression Ring
        public double TopRingInsideRadius { get; set; } // TCRIR
        public double TopRingOutsideRadius { get; set; } // TCROR
        public double TopRingThickness { get; set; } // TCRTHK
    }
}
