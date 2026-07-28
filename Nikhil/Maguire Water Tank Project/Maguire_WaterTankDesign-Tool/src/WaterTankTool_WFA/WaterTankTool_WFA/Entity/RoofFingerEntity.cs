using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class RoofFingerEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public double Thickness { get; set; } // THK
        public double Radius { get; set; } // R
        public int Quantity { get; set; } // Q
        public double SegmentDegree { get; set; } // SDEG
        public double EndDegree { get; set; } // EDEG
        public double ExtraDimension { get; set; } // EDIM
        public double Diameter { get; set; } // DIA
    }
}
