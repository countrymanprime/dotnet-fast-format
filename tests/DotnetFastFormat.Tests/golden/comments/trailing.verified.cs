using System; // system
using System.Linq; /* linq */

class A
{
    int x; // note   with   spaces
    int y; /* block */

    void M() { } // after method

    void Wrapped(string firstParameterName, string secondParameterName, int third) { } // long comment that would otherwise force wrapping

    int z;
} // after class
