using System.Security.Cryptography;
using System.Text;
using Brittany_Salon_Backend.Application.Tools.Models;

namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryCodeGenerator
{
    string Generate(int length);
    RecoveryCodeSecret CreateSecret(string plainCode);
}

public sealed class RecoveryCodeSecret
{
    public required string Hash { get; init; }
    public required byte[] Salt { get; init; }
}
