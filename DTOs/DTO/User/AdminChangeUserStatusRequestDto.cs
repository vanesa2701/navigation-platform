using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.User
{
    public sealed class AdminChangeUserStatusRequestDto
    {
        public required string Status { get; init; }
    }
}
