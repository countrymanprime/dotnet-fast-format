#nullable enable
#pragma warning disable CS0168
using   System;
#if NET8
using   System.Buffers;
#endif

namespace N
{
    #region Fields
    class A
    {
        int   x;
#if DEBUG
        int   debugOnly;
#else
        int   releaseOnly;
#endif
        #endregion
#if FEATURE
              int   odd   =   1 ;
#endif
    }
}
