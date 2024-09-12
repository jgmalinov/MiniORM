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

            using (new ConnectionManager(_connection))
            {
                using (var transaction = _connection.StartTransaction())
                {
                    foreach(IEnumerable dbSet in dbSets)
                    {
                        var persistMethod = typeof(DbContext)
                            .GetMethod("Persist", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(dbSet.GetType());
                        try
                        {
                            try
                            {
                                persistMethod.Invoke(this, new object[] { dbSet });
                            }
                            catch (TargetInvocationException e) when (e.InnerException is not null)
                            {
                                throw e.InnerException;
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Rollback!!!");
                            transaction.Rollback();
                            throw;
                        }
                    }
                    transaction.Commit();
                }
            }
            
        }
    }
}
