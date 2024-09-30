using System.Linq.Expressions;
using SupplyChain.Models;
using DnsClient;
using MongoDB.Driver;

namespace SupplyChain.Data
{
    public class OrderDbContext
    {
        private readonly IMongoCollection<OrderModel> _collection;

        public OrderDbContext(IMongoDatabase mongodb)
        {
            _collection = mongodb.GetCollection<OrderModel>("Orders");
        }

        public OrderModel QueryOne(Expression<Func<OrderModel, bool>> query)
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

        public List<OrderModel> QueryMany(Expression<Func<OrderModel, bool>> query)
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

        public bool Insert(OrderModel user)
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
        public bool Update(OrderModel update)
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
