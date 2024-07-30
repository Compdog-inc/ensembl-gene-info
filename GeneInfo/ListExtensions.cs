using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Standard AddRange but with nullable support.
        /// <para>
        /// Adds the elements of the given collection to the end of this list. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.
        /// </para>
        /// </summary>
        public static void AddRangeNullable<T>(this List<T> list, IEnumerable<T>? collection)
        {
            if (collection != null)
            {
                list.AddRange(collection);
            }
        }
    }
}
