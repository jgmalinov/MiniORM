namespace MiniORM
{
    public class DbSet<TEntity>: ICollection<TEntity>
        where TEntity : class, new()
    {
        internal ChangeTracker<TEntity> ChangeTracker { get; set; }
        internal IList<TEntity> Entities { get; set; }

        internal DbSet(IEnumerable<TEntity> entities)
        {
            Entities = entities.ToList();
            ChangeTracker = new ChangeTracker<TEntity>(entities);
        }

        public void Add(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException("Item cannot be null");
            Entities.Add(entity);
            ChangeTracker.Add(entity);
        }

        public void Clear()
        {
            while (Entities.Any())
            {
                var entity = Entities.First();
                Remove(entity);
            }
        }
        public bool Contains(TEntity entity) => Entities.Contains(entity);
        public void CopyTo(TEntity[] arr, int arrIndex) => Entities.CopyTo(arr, arrIndex);
        public int Count => Entities.Count;
        public bool IsReadOnly => Entities.IsReadOnly;
        public bool Remove(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException("Item cannot be null");
            var removedSuccessfully = Entities.Remove(entity);
            if (removedSuccessfully)
            {
                ChangeTracker.Remove(entity);
            }
            return removedSuccessfully;
        }
    }
}
