using System;

namespace KittyDI.Exceptions
{
  public class MultipleTypesRegisteredException : DependencyException
  {
    public Type RequestedType { get; internal set; }
  }
}