using System.ComponentModel.DataAnnotations.Schema;
using Trusses.Core.Models;

namespace Trusses.Core.Models
{
    public class Node
    {
        public long Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public double ReactionX { get; set; }
        public double ReactionY { get; set; }

        // Relacionamentos
        public long TrussId { get; set; }
        public Truss Truss { get; set; }

        public Support Support { get; set; }
        public Load Load { get; set; }
    }
}