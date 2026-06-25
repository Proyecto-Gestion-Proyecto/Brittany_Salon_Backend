using System.Collections.Concurrent;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Application.Tools.Models;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class InMemoryRecoveryCodeStore : IRecoveryCodeStore
{
    private readonly ConcurrentDictionary<string, StoreEntry> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Save(string email, RecoveryCodeRecord record)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El email es requerido.", nameof(email));
        }

        _store[email] = new StoreEntry
        {
            Record = record,
            SaltCopy = (byte[])record.Salt.Clone()
        };
    }

    public RecoveryCodeRecord? Get(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        if (!_store.TryGetValue(email, out var entry))
        {
            return null;
        }

        if (DateTime.UtcNow > entry.Record.ExpiresAtUtc)
        {
            _store.TryRemove(email, out _);
            return null;
        }

        return entry.Record;
    }

    public void Remove(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        _store.TryRemove(email, out _);
    }

    private sealed class StoreEntry
    {
        public required RecoveryCodeRecord Record { get; init; }
        public required byte[] SaltCopy { get; init; }
    }
}
