using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace jwtTestApi.Api;

public class Clims(string _privateKeyPem, string _issuret, string _audience) : Interface.IClims
{
    public string GetToken(List<Claim> claims, DateTime exp)
    {
        var nbf = DateTime.Now;
        //expires   //過期時間              
        var privateKey = RSA.Create();
        privateKey.ImportFromPem(_privateKeyPem); //for pem 
        var key = new RsaSecurityKey(privateKey);
        var signcreds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var jwt = new JwtSecurityToken(_issuret, _audience, claims: claims, notBefore: nbf, expires: exp, signingCredentials: signcreds);
        var JwtHander = new JwtSecurityTokenHandler();
        var token = JwtHander.WriteToken(jwt);
        return token;
    }
}