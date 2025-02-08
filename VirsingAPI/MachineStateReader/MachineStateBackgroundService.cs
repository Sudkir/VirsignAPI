using Serilog;
using System.Diagnostics;
using VirsignAPI.Interfaces;

namespace VirsignAPI.MachineStateReader
{
    public class MachineStateBackgroundService : BackgroundService
    {
        private readonly IMachineStateReader _stateReader;

        public MachineStateBackgroundService(IMachineStateReader stateReader)
        {
            _stateReader = stateReader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} [{nameof(MachineStateBackgroundService)}] started");

            try
            {
                if (_stateReader is MockMachineStateReader mockReader)
                {
                    await mockReader.StartAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
            }
        }
    }
}