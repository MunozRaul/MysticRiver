using System;
using System.IO;
using System.Text.Json;

namespace MysticRiver.Client.Services;

public sealed class GuestIdentityService {
    private readonly string _filePath;

    public GuestIdentityService() {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MysticRiver");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "guest.json");
    }

    public (string PlayerId, string DisplayName) GetOrCreateIdentity() {
        try {
            if (File.Exists(_filePath)) {
                var txt = File.ReadAllText(_filePath);
                var doc = JsonSerializer.Deserialize<GuestRecord?>(txt);
                if (doc is not null && !string.IsNullOrWhiteSpace(doc.PlayerId) && !string.IsNullOrWhiteSpace(doc.DisplayName)) {
                    return (doc.PlayerId, doc.DisplayName);
                }
            }
        }
        catch {
            // ignore and create new
        }

        var newId = Guid.NewGuid().ToString("N");
        var newName = "Guest-" + newId.Substring(0, 6);
        var record = new GuestRecord { PlayerId = newId, DisplayName = newName };
        try {
            var txt = JsonSerializer.Serialize(record);
            File.WriteAllText(_filePath, txt);
        }
        catch {
            // ignore persistence errors
        }

        return (record.PlayerId!, record.DisplayName!);
    }

    private sealed class GuestRecord {
        public string? PlayerId { get; set; }
        public string? DisplayName { get; set; }
    }
}
