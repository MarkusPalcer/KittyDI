using System;

namespace KittyDI.Attribute
{
  [AttributeUsage(AttributeTargets.Constructor)]
  public class ProvidingConstructorAttribute : System.Attribute { }
}