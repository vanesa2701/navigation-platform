using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.WebApiDTO.User
{
    public sealed class AdminChangeUserStatusRequestDtoApi
    {
        // Allowed: "Active" | "Suspended" | "Deactivated"
        public required string Status { get; init; }
    }
}
