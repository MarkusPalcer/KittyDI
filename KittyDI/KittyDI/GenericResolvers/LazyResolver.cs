using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  internal class LazyResolver : GenericResolver
  {
    private class Resolver<T> : IResolver<Lazy<T>>
    {
      public Lazy<T> Resolve( ResolutionInformation resolutionInformation)
      {
        return new Lazy<T>(resolutionInformation.Container.Resolve<Func<T>>());
      }
    }

    public LazyResolver() : base(typeof(Resolver<>), typeof(Lazy<>))
    {
    }
  }
}