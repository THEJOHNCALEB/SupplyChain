using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace SupplyChain.Models
{
    public class ItemModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public Guid UploadedBy { get; set; }
        public Guid LastChecked { get; set; }
        public Guid PickedBy { get; set; }
        public string CompanyName { get; set; }
        public string ItemName { get; set; }
        public string About { get; set; }
        public string Note { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
        public string unit { get; set; }
        public long Price { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
