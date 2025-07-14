using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor2.AddFunc
{
    /// <summary>
    /// 列表追加方法
    /// </summary>
    public static class ListAdd
    {
        /// <summary>
        /// 遍历行操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this List<T> list, Action<T, int> action)
        {
            if (list == null || action == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                action(list[i],i);
            }
        }
    }
}
