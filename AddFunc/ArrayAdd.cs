using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor2.AddFunc
{
    public static class ArrayAdd
    {
        /// <summary>
        /// 过滤数组元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T[] Fliter<T>(this T[] ts, Func<T, bool> func)
        {
            return ts.Where(func).ToArray();
        }

        /// <summary>
        /// 转换数组元素类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="ts"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static R[] Map<T, R>(this T[] ts, Func<T, R> func)
        {
            return Map(ts, (t, _) => func(t));
        }

        /// <summary>
        /// 遍历数组元素并执行操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this T[] ts, Action<T> action)
        {
            ForEach(ts, (t, _) => action(t));
        }

        public static void ForEach<T>(this T[] ts, Action<T, int> action)
        {
            if (ts == null || action == null) return;
            for (int i = 0; i < ts.Length; i++)
            {
                action(ts[i], i);
            }
        }

        /// <summary>
        /// 转换数组元素类型并获取索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="ts"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static R[] Map<T, R>(this T[] ts, Func<T, int, R> func)
        {
            if (ts == null || func == null) return Array.Empty<R>();
            R[] result = new R[ts.Length];
            for (int i = 0; i < ts.Length; i++)
            {
                result[i] = func(ts[i], i);
            }
            return result;
        }

        /// <summary>
        /// 获取第一个非空元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static T FirstNotNull<T>(this T[] ts) {
            return ts.Fliter(t =>
            {
                return t != null;
            }).First();
        }
    }
}
