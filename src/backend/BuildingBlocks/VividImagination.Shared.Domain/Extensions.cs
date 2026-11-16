using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Domain
{
    public static class Extensions
    {
        public static bool IsNullOrDefault<T>(this T? value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }

        public static Type GetUnproxiedEntityType<T>(this BaseEntity<T> entity)
        {
            var userType = entity.GetType();
            return userType;
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> list, int totalSize, int chunkSize)
        {
            int i = 0;
            while (i < totalSize)
            {
                IEnumerable<T> enumerable = list as T[] ?? list.ToArray();
                yield return enumerable.Skip(i).Take(chunkSize);
                i += chunkSize;
            }
        }
        // convenience for "countable" lists
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this ICollection<T> list, int chunkSize)
        {
            return Chunk(list, list.Count, chunkSize);
        }
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunkSize)
        {
            var enumerable = list as T[] ?? list.ToArray();
            return Chunk(enumerable, enumerable.Count(), chunkSize);
        }


        public static T ParseEnum<T>(this string? value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        
        
        public static T As<T>(this object obj)
            where T : class
        {
            return (T)obj;
        }
    }
}
