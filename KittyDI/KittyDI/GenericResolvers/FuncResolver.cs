using System;
using System.Collections.Generic;
using System.Linq;

namespace KittyDI.GenericResolvers
{
  public class FuncResolver : IGenericResolver
  {
    public bool Matches(Type genericType, Type[] typeParameters)
    {
      return genericType == typeof(Func<>) && typeParameters.Length == 1;
    }

    public Func<object> Resolve(DependencyContainer container, Type[] typeParameters, ISet<Type> previousResolutions)
    {
      var type = typeof(InternalResolver<>)
        .MakeGenericType(typeParameters);
      var method = type
        .GetMethod("Resolve");
      var instance = Activator.CreateInstance(type);
      return () => method.Invoke(instance, new object[] {container, previousResolutions});
    }

    private class InternalResolver<T>
    {
      public Func<T> Resolve(DependencyContainer container, ISet<Type> previousResolutions)
      {
        var factory = container.ResolveFactoryInternal(typeof(T), previousResolutions);
        return () => (T) factory();
      }
    }
  }
}