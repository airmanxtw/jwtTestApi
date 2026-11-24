using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace jwtTestApi.Controllers;

[ApiController]
[EnableCors("PublicPolicy")]
[Route("[controller]")]
public class JwtController(Api.Interface.IClims clims,IConfiguration configuration) : ControllerBase
{
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok("Hello, world!");
    }
        
    // 2. 登入取得AccessToken與RefreshToken
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
                new(System.Security.Claims.ClaimTypes.Role, "RefreshRole"),
                // 加一個自定義的ip
                //new(System.Security.Claims.ClaimTypes.UserData, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "")
            };
            var refreshtoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(20));

            Response.Cookies.Append("refresh_20261123_token", refreshtoken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = configuration["SameSite"] == "Strict" ? SameSiteMode.Strict : SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddMinutes(20),
                Path = "/jwtTestApi"
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

    // 3. 重新取得AccessToken, 使用RefreshToken驗證
    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("refreshAccessToken")]
    public IActionResult RefreshAccessToken()
    {
        var refreshclaims = User.Claims.ToList();
        var accesstoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(15));
        return Ok(accesstoken);
    }

    // 4. *** 重新取得RefreshToken, 使用RefreshToken驗證
    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("refreshSelf")]
    public IActionResult RefreshSelf()
    {
        var refreshclaims = User.Claims.ToList();
        // 從claims中取得使用者的資訊，例如IP地址
        //var userIp = refreshclaims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.UserData)?.Value;

        var refreshtoken = clims.GetToken(refreshclaims, DateTime.Now.AddMinutes(30));
        Response.Cookies.Append("refresh_20261123_token", refreshtoken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = configuration["SameSite"] == "Strict" ? SameSiteMode.Strict : SameSiteMode.None,
            Expires = DateTimeOffset.Now.AddMinutes(20),
            Path = "/jwtTestApi"
        });
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "CookieJwt", Roles = "RefreshRole")]
    [HttpGet("revokeRefreshToken")]
    public IActionResult RevokeRefreshToken()
    {
        Response.Cookies.Delete("refresh_20261123_token", new CookieOptions
        {
            Path = "/jwtTestApi"
        });
        return Ok("Refresh token revoked");
    }



}