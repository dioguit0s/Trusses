using System.ComponentModel.DataAnnotations.Schema;

namespace Trusses.Core.Models // Namespace corrigido
{
    public class Support // Alterado de internal para public
    {
        public long Id { get; set; }

        // Define se o apoio trava o movimento em X ou Y
        public bool RestrainX { get; set; }
        public bool RestrainY { get; set; }

        // Chave estrangeira para o Node
        public long NodeId { get; set; }
        public Node Node { get; set; }
    }
}