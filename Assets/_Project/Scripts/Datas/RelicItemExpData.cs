using System.Collections.Generic;

public partial class D_RelicItemExpData
{
    public static List<D_RelicItemExpData> GetAllRelicItemExpData() => FindEntities(data => true);
    
    public static D_RelicItemExpData GetRelicItemExpData(int targetLevel)
    {
        var candidates = GetAllRelicItemExpData();
        foreach (var candidate in candidates)
        {
            if (candidate.f_level == targetLevel)
            {
                return candidate;
            }
        }

        return null;
    }
}