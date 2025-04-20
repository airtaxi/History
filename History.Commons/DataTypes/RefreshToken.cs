using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
