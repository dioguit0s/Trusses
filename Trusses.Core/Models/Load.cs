using System.ComponentModel.DataAnnotations.Schema;

namespace Trusses.Core.Models // Namespace corrigido
{
    public class Load // Alterado de internal para public
    {
        public long Id { get; set; }
        public double Magnitude { get; set; }
        public double Angle { get; set; }

        // Chave estrangeira para o Node
        public long NodeId { get; set; }
        public Node Node { get; set; }
    }
}