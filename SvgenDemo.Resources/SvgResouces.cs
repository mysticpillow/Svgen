using Avalonia.Svg.Skia;

namespace SvgenDemo.Resources
{
    public static class SvgResources
    {
        private static SvgImage? _a;
        public static SvgImage A
        {
            get
            {
                _a ??= new() { Source = SvgSource.Load<SvgSource>(APath, new Uri(APath)) };
                return _a;
            }
        }

        private static SvgImage? _b;
        public static SvgImage B
        {
            get
            {
                _b ??= new() { Source = SvgSource.Load<SvgSource>(BPath, new Uri(BPath)) };
                return _b;
            }
        }

        private static SvgImage? _c;
        public static SvgImage C
        {
            get
            {
                _c ??= new() { Source = SvgSource.Load<SvgSource>(CPath, new Uri(CPath)) };
                return _c;
            }
        }

        public static string APath => "avares://SvgenDemo.Resources/SvgImages/A.svg";
        public static string BPath => "avares://SvgenDemo.Resources/SvgImages/B.svg";
        public static string CPath => "avares://SvgenDemo.Resources/SvgImages/C.svg";
    }
}