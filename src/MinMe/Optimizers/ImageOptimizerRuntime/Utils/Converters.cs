using System;
using System.Drawing;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Utils
{
    internal static class Converters
    {
        private const int EmuInPt = 12700;
        private const int TwipInPt = 20;

        public static double EmuToPt(long x) => (double)x/EmuInPt;

        public static long SmthToEmu(string s)
        {
            if (s.EndsWith("pt"))
                return PtToEmu(s.Substring(0, s.IndexOf("pt", StringComparison.Ordinal)));
            if (s.EndsWith("in")) //TODO: Need to check conversion rate
                return 72 * PtToEmu(s.Substring(0, s.IndexOf("in", StringComparison.Ordinal)));
            return PxToEmu(s);
        }

        private static long PtToEmu(string s) => PtToEmu(double.Parse(s));

        private static long PtToEmu(double x) => (long)(x * EmuInPt);

        private static long PxToEmu(string s) => PxToEmu(long.Parse(s));

        // x = 12345 for 123.45Px size; 1 Px = 4/3 Pt
        private static long PxToEmu(long x) => x * EmuInPt * 3 / 4 / 100;

        public static double TwipToPt(int x) => (double)x/TwipInPt;


        public static Size Expand (this Size a, Size b) =>
            new Size
            {
                Width = Math.Max(a.Width, b.Width),
                Height = Math.Max(a.Height, b.Height)
            };

        public static Size Restrict(this Size a, Size b) =>
            new Size
            {
                Width = Math.Min(a.Width, b.Width),
                Height = Math.Min(a.Height, b.Height)
            };

    }
}
