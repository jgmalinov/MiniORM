using System.Reflection;

namespace MiniORM
{
    public abstract class DbContext
    {
        private readonly DatabaseConnection _connection;
        private readonly Dictionary<Type, PropertyInfo> _dbsetProperties;
        internal static readonly Type[] AllowedSqlTypes =
        {
            typeof(string),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(decimal),
            typeof(bool),
            typeof(DateTime)
        };

        protected DbContext(string connectionString)
        {
            _connection = new DatabaseConnection(connectionString);
            _dbsetProperties = DiscoverDbSets();
            using(new ConnectionManager(_connection))
            {
                InitializeDbSets();
            }
            MapAllRelations();
        }

        public void SaveChanges()
        {
            var dbSets = _dbSetProperties
                .Select(pi => pi.Value.GetValue(this))
                .toArray();
            
            foreach(IEnumerable<object> dbSet in dbSets)
            {
                var invalidEntities = dbSet
                    .Where(entity => !isObjectValid(entity))
                    .ToArray();

                if (invalidEntities.Any())
                {
                    throw new InvalidOperationException($"{invalidEntities.Lenghth} invalid Entities Found in {dbSet.GetType().Name}");
                }
            }

            
        }
    }
}
