using System;

namespace PatternRendering
{

    public static class PatternAtlasLayout
    {
        public static readonly int ColorCount = Enum.GetValues(typeof(PaletteColor)).Length;
        public static readonly int BandCount = Enum.GetValues(typeof(PatternBand)).Length;
        public static readonly int AutoColor = (int)ShadowColor.CouleurPrincipaleAssombrie;
        public static readonly int CustomColor = ColorCount + 1;
        public static readonly int SameBand = BandCount;

        public static bool IsValid(int width, int height) =>
            width >= ColorCount && height >= BandCount + 1 &&
            width % ColorCount == 0 && height % (BandCount + 1) == 0;
    }
}
