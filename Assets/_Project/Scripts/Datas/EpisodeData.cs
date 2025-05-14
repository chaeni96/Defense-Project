using System.Linq;

public partial class D_EpisodeData
{
    public static int GetMaxEpisodeNumber()
    {
        return FindEntities(data => true).Max(e => e.f_episodeNumber);
    }
}