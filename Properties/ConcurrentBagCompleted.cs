using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace extractMongoApi.Properties
{
    public class ConcurrentBagCompleted<T> : ConcurrentBag<T>
    {
        public ConcurrentBagCompleted() : base() { }

        public ConcurrentBagCompleted(IEnumerable<T> collection) : base(collection)
        {
        }

        public void AddRange(IEnumerable<T> collection)
        {
            Parallel.ForEach(collection, item =>
            {
                base.Add(item);
            });
        }
    }
}
