using System.Collections.Generic;
using Trusses.Core.Models;

namespace Trusses.Core.Models
{
    public class Truss
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public List<Node> Nodes { get; set; } = new();
        public List<Member> Members { get; set; } = new();
    }
}