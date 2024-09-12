using System.ComponentModel.DataAnnotations;
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
            using (new ConnectionManager(_connection))
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

            foreach (IEnumerable<object> dbSet in dbSets)
            {
                var invalidEntities = dbSet
                    .Where(entity => !IsValid(entity))
                    .ToArray();

                if (invalidEntities.Any())
                {
                    throw new InvalidOperationException($"{invalidEntities.Length} invalid Entities Found in {dbSet.GetType().Name}");
                }
            }

            using (new ConnectionManager(_connection))
            {
                using (var transaction = _connection.StartTransaction())
                {
                    foreach (IEnumerable dbSet in dbSets)
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

        private void Persist<T>(DbSet<T> dbSet)
            where T : class, new()
        {
            var entityType = typeof(T);
            var tableName = GetTableName(entityType);
            var columnNames = GetColumnNames(tableName).ToArray();

            if (dbSet.ChangeTracker.Added.Count > 0)
            {
                this._connection.InsertEntities(dbSet.ChangeTracker.Added, tableName, columnNames);
            }

            var modifiedEntities = dbSet.ChangeTracker.GetModifiedEntities(dbSet).ToArray();
            if (modifiedEntities.Length > 0)
            {
                this._connection.UpdateEntities(modifiedEntities, tableName, columnNames);
            }

            if (dbSet.ChangeTracker.Removed.Count > 0)
            {
                this._connection.DeleteEntities(dbSet.ChangeTracker.Removed, tableName, columnNames);
            }
        }

        private static bool IsValid(object entity)
        {
            var validationContext = new ValidationContext(entity);
            return Validator.TryValidateObject(entity, validationContext, null);
        }

        private IDictionary<Type, PropertyInfo> DiscoverDbSets()
        {
            var dbSetProperties = this.GetType().GetProperties()
                .Where(pi => pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            var result = new Dictionary<Type, PropertyInfo>();
            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GenericTypeArguments[0];
                result[entityType] = property;
            }
            return result;
        }
    }
}
