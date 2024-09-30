using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace SupplyChain.Models
{
    public class CartModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public Guid UploadedBy { get; set; }
        public string ItemName { get; set; }

        public string CompanyName { get; set; }
        public string About { get; set; }
        public string Note { get; set; }
        public string Image { get; set; }
        public long Price { get; set; }
        public int Quantity { get; set; }
        public string unit { get; set; }
        public string Category { get; set; }
        public bool Paid { get; set; }
        public bool Finished { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
