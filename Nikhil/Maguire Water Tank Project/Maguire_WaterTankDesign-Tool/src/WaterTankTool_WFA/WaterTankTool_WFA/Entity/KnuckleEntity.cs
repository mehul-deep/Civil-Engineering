using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class KnuckleEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        // Type: "KnuckleKnuckle", "BottomKnuckle", "TopKnuckle"
        public string KnuckleType { get; set; }
        
        public double Thickness { get; set; } // THK
        
        // Common radii properties
        public double LowerRadius { get; set; } // LR or R
        public double CenterHeight { get; set; } // CHT
        
        public double UpperStartRadius { get; set; } // USR
        public double UpperExtendRadius { get; set; } // UER
        public double ExtendRadius { get; set; } // ER
        
        public double StartDegree { get; set; } // SDEG
        public double EndDegree { get; set; } // EDEG
        public double SectionRadius { get; set; } // SECR
        
        // BK / TK specific
        public int Quantity { get; set; } // Q
        public double ExtraDimension { get; set; } // EDIM
        public double Diameter { get; set; } // DIA
    }
}
