using System;

namespace KittyDI.Exceptions
{
  public class MultipleTypesRegisteredException : DependencyException
  {
    public Type RequestedType { get; internal set; }

    // TODO: Notify of registered types
  }
}