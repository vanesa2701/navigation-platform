using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Events
{
    public record UserLoggedOutEvent(Guid UserId, DateTime AtUtc, string? Jti);
}
