using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using VirsignAPI.ContextDB.Models;
using static System.String;

namespace VirsignAPI.ContextDB
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database;

        //Docker
        //public MongoDBContext(IConfiguration configuration)
        //{
        //    var connectionString = configuration["MONGODB_CONNECTION_STRING"];
        //    var client = new MongoClient(connectionString);
        //    _database = client.GetDatabase("MachineDB");
        //}

        public MongoDBContext(string connectionString, string databaseName)
        {
            if (!IsNullOrEmpty(connectionString) && !IsNullOrEmpty(databaseName))
            {
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(databaseName);
            }
            else
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {nameof(connectionString)} or {nameof(databaseName)} is Null Or Empty");
            }
        }

        public IMongoCollection<MachineModel> MachineModel => _database.GetCollection<MachineModel>("Machines");
        public IMongoCollection<UserModel> UserModel => _database.GetCollection<UserModel>("User");
    }
}