using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace SupplyChain.Models
{
    public class LoginPageModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public List<string> Roles { get; set; }
        public string Surname { get; set; }
        public string OtherNames { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string TelephoneNumber { get; set; }
        public string UserRoleString { get; set; }
        public string Image { get; set; }
        public long Balance { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime RegistrationDate { get; set; }
        public bool IsActive { get; set; }

        public string DateOfBirth { get; set; }
        public string Password { get; set; }
    }
}
