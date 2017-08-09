using System;

namespace KittyDI.Exceptions
{
  public class NoSuitableConstructorFoundException : DependencyException
  {
    public NoSuitableConstructorFoundException(Type targetType)
    {
      TargetType = targetType;
    }

    public Type TargetType { get; set; }
  }
}