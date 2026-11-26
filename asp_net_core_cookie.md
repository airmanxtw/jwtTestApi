# ASP.NET Core Web API 精簡 Cookie 驗證機制

ASP.NET Core 內建的 **Cookie 驗證**是最精簡的做法，**不需要安裝任何額外套件，也不用建置 Identity 或資料庫**，全部寫在 `Program.cs` 只要約 40 行程式碼就能運作。

## 最精簡 `Program.cs` 範例

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. 註冊 Cookie 驗證
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        
        // Web API 關鍵設定：未登入直接回傳 401，不要預設 302 轉址到登入頁
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 2. 登入端點：呼叫 SignInAsync 自動核發加密 Cookie
app.MapPost("/api/login", async (HttpContext ctx) =>
{
    // 實務上在這裡驗證帳號密碼
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "1001"),
        new Claim(ClaimTypes.Name, "Alice")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    await ctx.SignInAsync(new ClaimsPrincipal(identity));
    return Results.Ok(new { message = "登入成功" });
});

// 3. 受保護的 API：加上 RequireAuthorization() 即可（Controller 則用 [Authorize]）
app.MapGet("/api/secret", (ClaimsPrincipal user) =>
{
    return Results.Ok(new { user = user.Identity?.Name, data = "這是受保護的資料" });
}).RequireAuthorization();

// 4. 登出端點：清除 Cookie
app.MapPost("/api/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Ok(new { message = "已登出" });
});

app.Run();
```

## 為什麼這樣寫就夠安全？

* **內建自動加解密**：`SignInAsync` 核發的 Cookie 會由 ASP.NET Core 的 Data Protection API 自動加密與簽章，前端無法竄改內容，也不用自己寫 JWT 解析邏輯。
* **省去前端 Token 管理**：瀏覽器會自動在後續請求帶上 Cookie，前端不需要寫程式把 Token 存進 `localStorage` 或手動塞進 `Authorization` 標頭。

> **跨網域提醒**：若你的前端是獨立網域的 SPA，記得在後端 CORS 加上 `.AllowCredentials()`，並在前端請求加上 `credentials: 'include'`（Axios 則為 `withCredentials: true`）。