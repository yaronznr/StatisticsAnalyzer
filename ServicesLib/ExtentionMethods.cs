using System.Collections.Generic;

namespace ServicesLib
{
    public static class ExtentionMethods
    {
        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> list, int partitionCount)
        {
            var partitionList = new List<T>();
            foreach (var item in list)
            {
                partitionList.Add(item);

                if (partitionList.Count == partitionCount)
                {
                    yield return partitionList;
                    partitionList = new List<T>();
                }
            }

            if (partitionList.Count > 0)
            {
                yield return partitionList;
            }
        }
    }
}
