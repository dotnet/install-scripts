using System;

namespace Install_Scripts.Test.Utils
{
    [Flags]
    public enum Quality
    {
        None = 0,
        Daily = 1,
        Signed = 2,
        Validated = 4,
        Preview = 8,
        Ga = 16,
        All = Daily | Signed | Validated | Preview | Ga,
    }
}
