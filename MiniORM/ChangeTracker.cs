using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace MiniORM
{
    public class ChangeTracker<T>
        where T : class, new()
    {
        private readonly List<T> _allEntities;
        private readonly List<T> _added;
        private readonly List<T> _removed;

        public IReadOnlyCollection<T> AllEntities => _allEntities.AsReadOnly();
        public IReadOnlyCollection<T> Added => _added.AsReadOnly();
        public IReadOnlyCollection<T> Removed => _removed.AsReadOnly();
        public void Add(T item) => _added.Add(item);
        public void Remove(T item) => _removed.Remove(item);
        public ChangeTracker(IEnumerable<T> entities)
        {
            _added = new List<T>();
            _removed = new List<T>();
            _allEntities = CloneEntities(entities);
        }

        public IEnumerable<T> GetModifiedEntities(DbSet<T> dbSet)
        {
            var modifiedEntities = new List<T>();
            var primaryKeyes = typeof(T).GetProperties()
                               .Where(pi => pi.HasAttribute<KeyAttribute>())
                               .ToArray();
            foreach(var proxyEntity in AllEntities)
            {
                var primaryKeyValues = GetPrimaryKeyValues(primaryKeyes, proxyEntity).ToArray();
                var entity = dbSet.Entities
                            .Single(e => GetPrimaryKeyValues(primaryKeyes, e).SequenceEqual(primaryKeyValues));
                var isModified = IsModified(proxyEntity, entity);
                if (isModified)
                {
                    modifiedEntities.Add(entity);
                }
            }
            return modifiedEntities;
        }

        private static bool IsModified(T proxyEntity, T entity)
        {
            var monitoredProperties = typeof(T).GetProperties()
                                      .Where(pi => DbContext.AllowedSqlTypes.Contains(pi.PropertyType));
            var modifiedProperties = monitoredProperties
                                      .Where(pi => !Equals(pi.GetValue(proxyEntity), pi.GetValue(entity)))
                                      .ToArray();
            var isModified = modifiedProperties.Any();
            return isModified;
        }

        private static List<T> CloneEntities(IEnumerable<T> entities)
        {
            var clonedEntities = new List<T>();
            var propertiesToClone = typeof(T).GetProperties()
                                    .Where(pi => DbContext.AllowedSqlTypes.Contains(pi.PropertyType))
                                    .ToArray();
            foreach ( var entity in entities)
            {
                var clonedEntity = Activator.CreateInstance<T>();
                foreach (var property in propertiesToClone)
                {
                    var value = property.GetValue(entity);
                    property.SetValue(clonedEntity, value);
                }
                clonedEntities.Add(clonedEntity);
            }
            return clonedEntities;
        }
    }
}