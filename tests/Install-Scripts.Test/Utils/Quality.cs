#nullable disable

using System;

namespace Install_Scripts.Test.Utils
{
    [Flags]
    public enum Quality
    {
        None = 0,
        Daily = 1,
        Preview = 8,
        Ga = 16,
        All = Daily | Preview | Ga,
    }
}
