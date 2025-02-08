using MongoDB.Bson.Serialization.Attributes;

namespace VirsignAPI.ContextDB.Models
{
    public class MachineModel
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public EState State { get; set; }
    }

    public enum EState
    {
        On,
        Off
    }
}