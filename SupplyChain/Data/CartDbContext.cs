using System.Linq.Expressions;
using SupplyChain.Models;
using DnsClient;
using MongoDB.Driver;

namespace SupplyChain.Data
{
    public class CartDbContext
    {
        private readonly IMongoCollection<CartModel> _collection;

        public CartDbContext(IMongoDatabase mongodb)
        {
            _collection = mongodb.GetCollection<CartModel>("Cart");
        }

        public CartModel QueryOne(Expression<Func<CartModel, bool>> query)
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

        public List<CartModel> QueryMany(Expression<Func<CartModel, bool>> query)
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

        public bool Insert(CartModel user)
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
        public bool Update(CartModel update)
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
