using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class TransitionsEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        // 1 for T1, 2 for T2, etc.
        public int TransitionNumber { get; set; }
        
        public double OutsideRadius { get; set; } // OR
        public double LowerRadius { get; set; } // LR
        public double UpperRadius { get; set; } // UR
        public double Height { get; set; } // HT
        public double Thickness { get; set; } // THK
        public double SegmentDegree { get; set; } // DEG / SEG
        public int Quantity { get; set; } // Q
    }
}
