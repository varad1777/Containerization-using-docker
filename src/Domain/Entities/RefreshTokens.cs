namespace MyApp.Domain.Entities
{
    public class RefreshTokens
    {

        public int Id { get; set; }
        public string Token { get; set; }             // Random string
        public string UserId { get; set; }           // Link to ApplicationUser
        public DateTime Expires { get; set; }        // Expiry time
        public bool IsRevoked { get; set; }          // Revocation flag
        public DateTime Created { get; set; } = DateTime.UtcNow;

    }
}
