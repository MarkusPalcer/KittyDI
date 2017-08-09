using System;

namespace KittyDI.Exceptions
{
  public class NoInterfaceImplementationGivenException : DependencyException
  {
    public Type InterfaceType { get; set; }
  }
}