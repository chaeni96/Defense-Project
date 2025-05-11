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
                    return "노멀";
                case Grade.Rare:
                    return "레어";
                case Grade.Epic:
                    return "에픽";
                case Grade.Legendary:
                    return "레전더리";
                case Grade.Mythic:
                    return "신화";
                default:
                    return "Unknown";
            }
        }
    }
}