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
    }
}