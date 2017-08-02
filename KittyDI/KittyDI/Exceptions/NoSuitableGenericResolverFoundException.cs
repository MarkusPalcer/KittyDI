using System;

namespace KittyDI.Exceptions
{
  public class NoSuitableGenericResolverFoundException : Exception
  {
    public Type RequestedType { get; set; }
  }
}