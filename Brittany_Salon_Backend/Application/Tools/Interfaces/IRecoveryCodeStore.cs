namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryCodeStore
{
    void Save(string email, Models.RecoveryCodeRecord record);
    Models.RecoveryCodeRecord? Get(string email);
    void Remove(string email);
}
