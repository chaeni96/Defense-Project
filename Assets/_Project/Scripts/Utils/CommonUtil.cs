using System;
using System.Collections.Generic;
using System.Linq;
using BGDatabaseEnum;

namespace AutoBattle.Scripts.Utils
{
    public static class CommonUtil
    {
        public static string GetGradeName(Grade grade)
        {
            switch (grade)
            {
                case Grade.Normal:
                    return "���";
                case Grade.Rare:
                    return "����";
                case Grade.Epic:
                    return "����";
                case Grade.Legendary:
                    return "��������";
                case Grade.Mythic:
                    return "��ȭ";
                default:
                    return "Unknown";
            }
        }
        
        public static T RandomRatePick<T>(
            this IEnumerable<T> list,
            Func<T, decimal> selector)
        {
            decimal sum = list.Sum(selector);

            Random random = new System.Random();
            decimal randomValue = Convert.ToDecimal(random.NextDouble()) * sum;

            foreach (T item in list)
            {
                decimal rate = selector(item);
                if (rate > randomValue)
                {
                    return item;
                }

                randomValue -= rate;
            }

            return default(T);
        }
    }
}