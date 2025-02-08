namespace VirsignAPI.Interfaces;

public interface IMachineStateReader
{
    Task StartAsync(CancellationToken cancellationToken);
}