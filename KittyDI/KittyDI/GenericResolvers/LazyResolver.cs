using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  public class LazyResolver : GenericResolver
  {
    private class Resolver<T> : IResolver<Lazy<T>>
    {
      public Lazy<T> Resolve(DependencyContainer container, ISet<Type> previousResolutions)
      {
        return new Lazy<T>(container.Resolve<Func<T>>());
      }
    }

    public LazyResolver() : base(typeof(Resolver<>), typeof(Lazy<>))
    {
    }
  }
}