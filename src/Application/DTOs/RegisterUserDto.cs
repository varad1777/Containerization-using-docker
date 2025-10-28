using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.DTOs
{
    

        public class RegisterDto
        {

            [Required]
            [Length(3, 10, ErrorMessage = "userName must be between {1} and {2} character.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(10, MinimumLength = 3, ErrorMessage = "Password must be between {2} and {1} characters.")]
            public string Password { get; set; }
            public string Role { get; set; } // Admin or User
        



    }
}
