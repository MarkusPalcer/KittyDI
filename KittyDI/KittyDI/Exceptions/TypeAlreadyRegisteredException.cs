using System;

namespace KittyDI.Exceptions
{
  public class TypeAlreadyRegisteredException : Exception
  {
    public Type ConflictingType { get; set; }
  }
}