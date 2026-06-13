using AmbulatoryCarePortal.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AmbulatoryCarePortal.Infrastructure.Services;

public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionService(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector("SystemSettings");
    }

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}
