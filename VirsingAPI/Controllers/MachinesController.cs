using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using VirsignAPI.ContextDB;
using VirsignAPI.ContextDB.Models;

namespace VirsignAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachinesController : ControllerBase
    {
        private readonly MongoDBContext _context;

        public MachinesController(MongoDBContext context)
        {
            _context = context;
        }

        // 1. Добавление нового агрегата
        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult AddMachine([FromBody] MachineModel machine, CancellationToken ct = default)
        {
            try
            {
                Log.Information($"[{nameof(AddMachine)}] Name: {machine.Name}");
                machine.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                _context.MachineModel.InsertOne(machine, cancellationToken: ct);
                Log.Information($"[{nameof(AddMachine)}] ID: {machine.Id}");
                return Ok(machine);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }

        // 2. Получение списка агрегатов
        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult GetMachines(CancellationToken ct = default)
        {
            try
            {
                var machines = _context.MachineModel.Find(m => true).ToList(ct);
                Log.Information($"[{nameof(GetMachines)}] found:{machines.Count} items");
                return Ok(machines);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }

        // 3. Изменение имени агрегата
        [Authorize(Roles = "user,admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateMachineName(string id, [FromBody] string newName, CancellationToken ct = default)
        {
            try
            {
                var filter = Builders<MachineModel>.Filter.Eq(m => m.Id, id);
                var update = Builders<MachineModel>.Update.Set(m => m.Name, newName);
                UpdateResult? result = _context.MachineModel.UpdateOne(filter, update, options: null, ct);

                if (result.ModifiedCount == 0)
                {
                    Log.Warning($"[{nameof(UpdateMachineName)}] ID: {id} not found!");
                    return NotFound();
                }

                Log.Information($"[{nameof(UpdateMachineName)}] updated successfully");

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }

        // 4. Получение статуса агрегата по имени
        [Authorize(Roles = "user,admin")]
        [HttpGet("status/{name}")]
        public IActionResult GetVirsign(string name, CancellationToken ct = default)
        {
            try
            {
                var machine = _context.MachineModel.Find(m => m.Name == name).FirstOrDefault(ct);
                if (machine != null) return Ok(machine.State);
                Log.Warning($"[{nameof(GetVirsign)}] name: {name} not found!");
                return NotFound();
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }
    }
}