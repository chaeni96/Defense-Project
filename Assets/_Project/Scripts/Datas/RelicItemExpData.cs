using System.Collections.Generic;

public partial class D_RelicItemExpData
{
    public static List<D_RelicItemExpData> GetAllRelicItemExpData() => FindEntities(data => true);
    
    public static D_RelicItemExpData GetRelicItemExpData(D_RelicItemData relicItemData)
    {
        var candidates = GetAllRelicItemExpData();
        foreach (var candidate in candidates)
        {
            if (candidate.f_level == relicItemData.f_level)
            {
                return candidate;
            }
        }

        return null;
    }
}