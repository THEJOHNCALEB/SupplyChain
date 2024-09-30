using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace SupplyChain.Models
{
    public class OrderModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public Guid OrderedBy { get; set; }
        public string CompanyName { get; set; }
        public string ItemName { get; set; }
        public string About { get; set; }
        public string Note { get; set; }
        public string Image { get; set; }
        public long Price { get; set; }
        public string PaymentReference { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
