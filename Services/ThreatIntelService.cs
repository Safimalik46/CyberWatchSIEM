using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;

namespace CyberWatchSIEM.Services;

public class ThreatIntelService
{
    private readonly ThreatIntelRepository _repo;
    private readonly AuditService _audit;

    public ThreatIntelService(ApplicationDbContext context)
    {
        _repo = new ThreatIntelRepository(context);
        _audit = new AuditService(context);
    }

    public Task<List<MaliciousIP>> GetIPsAsync() => _repo.GetAllIPsAsync();
    public Task<List<SuspiciousDomain>> GetDomainsAsync() => _repo.GetAllDomainsAsync();
    public Task<List<MalwareSignature>> GetSignaturesAsync() => _repo.GetAllSignaturesAsync();

    public async Task AddIPAsync(MaliciousIP entity)
    {
        await _repo.AddIPAsync(entity);
        await _audit.LogCreateAsync("ThreatIntel", $"Added IP {entity.IPAddress}");
    }

    public async Task AddDomainAsync(SuspiciousDomain entity)
    {
        await _repo.AddDomainAsync(entity);
        await _audit.LogCreateAsync("ThreatIntel", $"Added domain {entity.Domain}");
    }

    public async Task AddSignatureAsync(MalwareSignature entity)
    {
        await _repo.AddSignatureAsync(entity);
        await _audit.LogCreateAsync("ThreatIntel", $"Added signature {entity.Signature}");
    }

    public async Task DeleteIPAsync(int id)
    {
        var item = (await _repo.GetAllIPsAsync()).FirstOrDefault(i => i.Id == id);
        if (item == null) return;
        await _repo.DeleteIPAsync(item);
        await _audit.LogDeleteAsync("ThreatIntel", item.IPAddress);
    }

    public async Task DeleteDomainAsync(int id)
    {
        var item = (await _repo.GetAllDomainsAsync()).FirstOrDefault(d => d.Id == id);
        if (item == null) return;
        await _repo.DeleteDomainAsync(item);
        await _audit.LogDeleteAsync("ThreatIntel", item.Domain);
    }

    public async Task DeleteSignatureAsync(int id)
    {
        var item = (await _repo.GetAllSignaturesAsync()).FirstOrDefault(s => s.Id == id);
        if (item == null) return;
        await _repo.DeleteSignatureAsync(item);
        await _audit.LogDeleteAsync("ThreatIntel", item.Signature);
    }
}
