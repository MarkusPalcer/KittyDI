using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Resolves requests for <code>Func&lt;T&gt;</code>
  /// </summary>
  public class FuncResolver : GenericResolver
  {
    public FuncResolver() 
      : base(typeof(InternalResolver<>), typeof(Func<>))
    {
    }

    private class InternalResolver<T> : IResolver<Func<T>>
    {
      public Func<T> Resolve(DependencyContainer container, ISet<Type> previousResolutions)
      {
        var factory = container.ResolveFactoryInternal(typeof(T), previousResolutions);
        return () => (T) factory();
      }
    }
  }
}