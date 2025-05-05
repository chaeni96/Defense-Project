using System.Collections.Generic;

public static class ListExtensions
{
    public static void RemoveAtSwapBack<T>(this List<T> list, int index)
    {
        int last = list.Count - 1;
        if (index < 0 || index > last)
            return;

        if (index != last)
        {
            list[index] = list[last];
        }

        list.RemoveAt(last);
    }
}
