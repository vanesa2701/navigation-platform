using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utilities
{
    public class JwtUser
    {
        public JwtUser()
        {
        }

        public string Role { get; set; }
        public Guid Id { get; set; }
        public string? Password { get; set; }
        public string Email { get; set; }
    }
}

