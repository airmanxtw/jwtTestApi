using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace jwtTestApi.Controllers;

[ApiController]
[EnableCors("PublicPolicy")]
[Route("[controller]")]
public class JwtController(Api.Interface.IClims clims) : ControllerBase
{
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok("Hello, world!");
    }
        
    [HttpPost("login")]
    public IActionResult Login([FromBody] Models.Login login)
    {
        // 在這裡可以加入驗證邏輯，例如檢查使用者名稱和密碼
        if (login.Username == "test" && login.Password == "password")
        {
            var accessclaims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, login.Username),
                new(System.Security.Claims.ClaimTypes.Role, "User")
            };
            var accesstoken = clims.GetToken(accessclaims, DateTime.Now.AddSeconds(20));

            var refreshclaims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, login.Username),
                new(System.Security.Claims.ClaimTypes.Role, "RefreshRole")
            };
            var refreshtoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(30));

            Response.Cookies.Append("refresh_20261123_token", refreshtoken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddDays(7)
            });

            return Ok(accesstoken);
        }
        return Unauthorized("Invalid credentials");
    }

    [Authorize]
    [HttpGet("secure")]
    public IActionResult Secure()
    {
        var timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return Ok(timeStr);
    }

    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("jwt")]
    public IActionResult Jwt()
    {
        return Ok("This is a JWT secure endpoint");
    }

    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("refreshAccessToken")]
    public IActionResult RefreshAccessToken()
    {
        var refreshclaims = User.Claims.ToList();
        var accesstoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(15));
        return Ok(accesstoken);
    }

    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("refreshSelf")]
    public IActionResult RefreshSelf()
    {
        var refreshclaims = User.Claims.ToList();
        var refreshtoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(30));
        Response.Cookies.Append("refresh_20261123_token", refreshtoken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.Now.AddMinutes(30)
        });
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("revokeRefreshToken")]
    public IActionResult RevokeRefreshToken()
    {
        Response.Cookies.Delete("refresh_20261123_token");
        return Ok("Refresh token revoked");
    }



}