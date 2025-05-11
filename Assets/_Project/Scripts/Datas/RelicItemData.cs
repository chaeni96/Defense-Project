using System.Collections.Generic;

public partial class D_RelicItemData
{
    public static List<D_RelicItemData> GetAllRelicItems() 
    {
        return D_RelicItemData.FindEntities(data => true);
    }
    
    public static List<D_RelicItemData> GetOwnedRelicItems() 
    {
        return D_RelicItemData.FindEntities(data => data.f_level > 0);
    }
}
