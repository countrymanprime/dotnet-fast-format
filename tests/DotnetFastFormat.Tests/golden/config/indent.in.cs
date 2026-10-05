using System;
using System.Collections.Generic;

namespace Company.Product
{
  public class Outer
  {
    private readonly int count;

    public Outer(int count) { }

    // A nested type, so the members sit three levels deep.
    public class Inner
    {
      public int Value { get; set; }

      public void Describe(string prefix, string separator, int repeat, bool upperCase) { }

      public void Combine(string first, string second, int times, bool upper, char fill, int pad) { }

      /// <summary>Documentation is re-indented with the code.</summary>
      public interface IThing
      {
        void Run(int a);
      }
    }
  }
}
