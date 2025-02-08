using VirsignAPI.Interfaces;

namespace VirsignAPI.MachineStateReader
{
    public class RealMachineStateReader : IMachineStateReader
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            //TODO RealMachineStateReader code
            return Task.CompletedTask;
        }
    }
}