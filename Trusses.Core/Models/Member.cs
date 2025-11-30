using Trusses.Core.Models;

namespace Trusses.Core.Models
{
    public class Member
    {
        public long Id { get; set; }
        public double? Force { get; set; }

        public long StartNodeId { get; set; }
        public Node StartNode { get; set; }

        public long EndNodeId { get; set; }
        public Node EndNode { get; set; }

        public long TrussId { get; set; }
        public Truss Truss { get; set; }
    }
}