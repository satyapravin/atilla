using System;

namespace BitmexCore.Dtos
{
    public class AccessTokenDto
    {
        public string Id { get; set; }
        public double? Ttl { get; set; }
        public DateTime? Created { get; set; }
        public double? UserId { get; set; }

    }
}
