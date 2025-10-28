
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using  MyApp.Application.DTOs;

namespace EF_core_assignment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        // user manager build in class provided by the ASP.net core 
        // we dont create the user directly , insted to use the user manager class
        // to get user related operations 

        private readonly IConfiguration _configuration;
        // and this is required to get the configuration setting of applications 

        private readonly ITokenService _tokenService;
        // for accessing the token services 

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration, ITokenService tokenService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _tokenService = tokenService;
        }


        // now lets create the actions 
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // validate the model also 

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var user = new ApplicationUser { UserName = model.Username, Email = model.Username };
            var result = await _userManager.CreateAsync(user, model.Password); // create the user
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Assign Role
            if (!await _userManager.IsInRoleAsync(user, model.Role))// chech that user belogs to role, that specified in Model 
                await _userManager.AddToRoleAsync(user, model.Role); // if not add this perticular user role 

            return Ok("User registered successfully!");
        }






        // login action

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)

        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }



            var user = await _userManager.FindByNameAsync(model.Username); // find by nusername 
            if (user == null) return Unauthorized(new { message = "Username not found" });

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password)) // chech the password 
                return Unauthorized(new { message = "Invalid Credentials..." });

            var jwtToken = await _tokenService.CreateJwtToken(user);
            var refreshToken = await _tokenService.GenerateRefreshToken(user.Id);

            // Send JWT and Refresh Token in HttpOnly cookies
            Response.Cookies.Append("jwtToken", jwtToken, new CookieOptions
            {
                HttpOnly = true, // prevent JS access, so prevent XXS attack
                Secure = true, // cookie only send on HTTPS
                SameSite = SameSiteMode.None, //not same site 
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            }); 
            
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "Logged in successfully",
                user = user.UserName,
                roles = roles // <-- this will be a list of roles: ["Admin"], ["User"], or multiple
            });
        }



        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var token))
            {
                var refreshToken = await _tokenService.GetRefreshToken(token);
                if (refreshToken != null)
                    await _tokenService.RevokeRefreshToken(refreshToken); // revoking RT
            }

            Response.Cookies.Delete("jwtToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
            Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

            return Ok(new { message = "Logged out successfully" });
        }



        // refresh tokens 

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var token))
                return Unauthorized();

            var refreshToken = await _tokenService.GetRefreshToken(token);
            if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(refreshToken.UserId);

            // Optionally: revoke old refresh token and issue new one
            await _tokenService.RevokeRefreshToken(refreshToken);
            var newRefreshToken = await _tokenService.GenerateRefreshToken(user.Id);

            var jwtToken = await _tokenService.CreateJwtToken(user);

            // Send new cookies
            Response.Cookies.Append("jwtToken", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", newRefreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new { message = "Token refreshed" , user = user.UserName });
        }
    }
}
