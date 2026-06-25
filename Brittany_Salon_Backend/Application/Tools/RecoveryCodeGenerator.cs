using System.Security.Cryptography;
using System.Text;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Microsoft.Extensions.Options;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class RecoveryCodeGenerator : IRecoveryCodeGenerator
{
    private const string Alphabet =
        "ABCDEFGHJKMNPQRSTVWXYZ23456789";

    private readonly RecoveryCodeSettings _settings;

    public RecoveryCodeGenerator(IOptions<RecoveryCodeSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Generate(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "La longitud del código debe ser mayor que cero.");
        }

        var alphabetSize = Alphabet.Length;
        var unbiasedByteLimit = (byte)(256 - (256 % alphabetSize));

        Span<char> codeBuffer = length <= 128
            ? stackalloc char[length]
            : new char[length];

        Span<byte> randomBuffer = stackalloc byte[128];
        var produced = 0;

        while (produced < length)
        {
            RandomNumberGenerator.Fill(randomBuffer);

            for (var i = 0; i < randomBuffer.Length && produced < length; i++)
            {
                var value = randomBuffer[i];
                if (value >= unbiasedByteLimit)
                {
                    continue;
                }

                codeBuffer[produced++] = Alphabet[value % alphabetSize];
            }
        }

        return new string(codeBuffer);
    }

    public RecoveryCodeSecret CreateSecret(string plainCode)
    {
        if (string.IsNullOrWhiteSpace(plainCode))
        {
            throw new ArgumentException(
                "El código no puede ser vacío.",
                nameof(plainCode));
        }

        var salt = RandomNumberGenerator.GetBytes(_settings.SaltSizeBytes);
        var hash = ComputePbkdf2(plainCode, salt, _settings.Pbkdf2Iterations);

        return new RecoveryCodeSecret
        {
            Hash = hash,
            Salt = salt
        };
    }

    internal static string ComputePbkdf2(string code, byte[] salt, int iterations)
    {
        var codeBytes = Encoding.UTF8.GetBytes(code);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            password: codeBytes,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToHexString(derived);
    }
}
