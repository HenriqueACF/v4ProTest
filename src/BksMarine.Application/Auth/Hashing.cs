using System.Security.Cryptography;
using System.Text;

namespace BksMarine.Application.Auth;

public static class Hashing
{
    public static string Sha256(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
