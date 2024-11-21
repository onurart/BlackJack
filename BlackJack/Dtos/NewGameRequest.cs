using System.Reflection.PortableExecutable;
using System.Security.AccessControl;

namespace BlackJack.Dtos;

    public class NewGameRequest
    {
        public string SessionId { get; set; }
        public decimal BetAmount { get; set; }
    }