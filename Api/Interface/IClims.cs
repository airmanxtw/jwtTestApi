using System.Security.Claims;

namespace jwtTestApi.Api.Interface;

public interface IClims
{
    string GetToken(List<Claim> claims, DateTime exp);
}