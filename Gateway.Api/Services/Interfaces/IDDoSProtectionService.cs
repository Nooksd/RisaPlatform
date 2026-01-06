namespace Gateway.Api.Services.Interfaces;

public interface IDDoSProtectionService
{
    Task<bool> IsBlacklistedAsync(string ipAddress);
    Task AddToBlacklistAsync(string ipAddress, TimeSpan duration);
    Task RemoveFromBlacklistAsync(string ipAddress);
    Task<bool> DetectSuspiciousPatternAsync(string ipAddress, string path);
    Task<int> IncrementThreatScoreAsync(string ipAddress);
    Task ResetThreatScoreAsync(string ipAddress);
}