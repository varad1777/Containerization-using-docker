using Microsoft.AspNetCore.Identity;

namespace MyApp.Domain.Entities
{
    public class ApplicationUser : IdentityUser // application user has to inherit the IdentityUser 
                                                // as it is required for the authorisation and the authentication 
                                                // it represent the user account in your application 
    {



    }
}
