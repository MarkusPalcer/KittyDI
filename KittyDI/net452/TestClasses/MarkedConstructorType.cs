using System;
using KittyDI.Attribute;

namespace TestClasses
{
  public class MarkedConstructorType
  {
    [ProvidingConstructor]
    public MarkedConstructorType(int d) { }

    public MarkedConstructorType(ITestInterface t)
    {
      throw new NotImplementedException();
    }
  }
}