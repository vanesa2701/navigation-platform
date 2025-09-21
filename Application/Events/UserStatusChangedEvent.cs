using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Events
{
    public class UserStatusChangedEvent
    {
        public Guid UserId { get; init; }
        public string OldStatus { get; init; } = default!;
        public string NewStatus { get; init; } = default!;
        public Guid ChangedBy { get; init; }
        public DateTime ChangedAtUtc { get; init; }
    }
}
