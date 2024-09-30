using System.Linq.Expressions;
using SupplyChain.Models;
using DnsClient;
using MongoDB.Driver;

namespace SupplyChain.Data
{
    public class ApplicationDbContext
    {
        private readonly IMongoCollection<LoginPageModel> _collection;

        public ApplicationDbContext(IMongoDatabase mongodb)
        {
            _collection = mongodb.GetCollection<LoginPageModel>("LoginPage");
        }

        public LoginPageModel QueryOne(Expression<Func<LoginPageModel, bool>> query)
        {
            try
            {
                var user = _collection.FindSync(query).FirstOrDefault();
                return user;
            }
            catch
            {
                return null;
            }
        }

        public List<LoginPageModel> QueryMany(Expression<Func<LoginPageModel, bool>> query)
        {
            try
            {
                var allUsers = _collection.FindSync(query);
                return allUsers.ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool Insert(LoginPageModel user)
        {
            try
            {
                _collection.InsertOne(user);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Update(LoginPageModel update)
        {
            try
            {
                _collection.ReplaceOne(p => p.Id == update.Id, update);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Delete(Guid id)
        {
            try
            {
                _collection.DeleteOne(p => p.Id == id);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
