using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("GuestPolicy",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    options.AddPolicy("PublicPolicy",
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:5173",                                          
                                            "http://portal.stust.edu.tw")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                    });
});

var issuret = "your_issuer_here";
var audience = "your_audience_here";

var AccessRSA = RSA.Create(2048);
var AccessPublic = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(AccessRSA);
var AccessPrivate = AccessRSA.ExportRSAPrivateKeyPem();

// var RefreshRSA = RSA.Create(2048);
// var RefreshPublic = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(RefreshRSA);
// var RefreshPrivate = AccessRSA.ExportRSAPrivateKeyPem();

builder.Services.AddScoped<jwtTestApi.Api.Interface.IClims>(sp =>
{
    return new jwtTestApi.Api.Clims(AccessPrivate, issuret, audience);
});

// 1. 提供兩種api驗證模式，一種是預設的JwtBearer，一種是CookieJwt
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        //是否驗證發行人
        ValidateIssuer = true,
        ValidIssuer = issuret,//發行人
                              //是否驗證受眾人
        ValidateAudience = true,
        ValidAudience = audience,//受眾人
                                 //是否驗證金鑰
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = AccessPublic,
        ValidateLifetime = true, //驗證生命週期
        RequireExpirationTime = true, //過期時間
        ClockSkew = TimeSpan.Zero
    };    

    // 設定JwtBearer事件，用於處理驗證失敗和Token驗證成功的情況
    option.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = c =>
        {
            Console.WriteLine("OnAuthenticationFailed: " + c.Exception.Message);
            return System.Threading.Tasks.Task.CompletedTask;
        },
        OnTokenValidated = c =>
        {
            Console.WriteLine("OnTokenValidated: " + c.SecurityToken);
            // 在這裡可以加入額外的驗證邏輯
            //c.Fail("這個Token是被拒絕的");
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };
}).AddJwtBearer("CookieJwt", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuret,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = AccessPublic,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // 設定CookieJwt事件，用於從Cookie中讀取Token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["refresh_20261123_token"];
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // 如果要到資料庫檢查token，可以在這裡加入邏輯，例如查詢token是否存在或是否被撤銷
            Console.WriteLine("OnTokenValidated: " + context.SecurityToken);

            // 如果Token不合法，可以呼叫context.Fail("Token無效");
            // context.Fail("Token無效");
            
            return Task.CompletedTask;

            
            
        }
        
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UsePathBase("/jwtTestApi");

app.UseRouting();

app.UseCors();

//1.先開啟認證
app.UseAuthentication();
//2.再開啟授權
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();


app.Run();


