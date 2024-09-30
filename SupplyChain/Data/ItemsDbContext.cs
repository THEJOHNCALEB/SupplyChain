using System.Linq.Expressions;
using SupplyChain.Models;
using DnsClient;
using MongoDB.Driver;

namespace SupplyChain.Data
{
    public class ItemsDbContext
    {
        private readonly IMongoCollection<ItemModel> _collection;

        public ItemsDbContext(IMongoDatabase mongodb)
        {
            _collection = mongodb.GetCollection<ItemModel>("Items");
        }

        public ItemModel QueryOne(Expression<Func<ItemModel, bool>> query)
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

        public List<ItemModel> QueryMany(Expression<Func<ItemModel, bool>> query)
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

        public bool Insert(ItemModel user)
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
        public bool Update(ItemModel update)
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
