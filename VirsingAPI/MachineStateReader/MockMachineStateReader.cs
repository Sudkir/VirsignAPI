using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using VirsignAPI.ContextDB;
using VirsignAPI.ContextDB.Models;
using VirsignAPI.Interfaces;

namespace VirsignAPI.MachineStateReader
{
    public class MockMachineStateReader : IMachineStateReader, IDisposable
    {
        private readonly MongoDBContext _context;
        private readonly int _intervalSeconds;
        private Timer _timer;

        public MockMachineStateReader(MongoDBContext context, IConfiguration configuration)
        {
            _context = context;
            _intervalSeconds = configuration.GetValue<int>($"DataGenerator:IntervalSeconds", 30);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            Log.Information($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} [{nameof(MachineStateBackgroundService)}] started");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Log.Information(
                        $"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} [{nameof(MachineStateBackgroundService)}] started");

                    var machines = await _context.MachineModel.Find(_ => true).ToListAsync(cancellationToken: ct);
                    var random = new Random();

                    var updates = machines.Select(machine =>
                    {
                        machine.State = random.Next(0, 2) == 0 ? EState.Off : EState.On;

                        return new ReplaceOneModel<MachineModel>(
                            Builders<MachineModel>.Filter.Eq(m => m.Id, machine.Id),
                            machine);
                    }).ToList();

                    if (updates.Any())
                    {
                        await _context.MachineModel.BulkWriteAsync(updates, cancellationToken: ct);
                        Log.Information(
                            $"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} [{nameof(MachineStateBackgroundService)}] update: {updates.Count} items");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} [{nameof(MachineStateBackgroundService)}] {ex.Message}");
                }
                finally
                {
                    await Task.Delay(_intervalSeconds * 1000, ct); // second -> ms
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task UpdateMachineStatesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}