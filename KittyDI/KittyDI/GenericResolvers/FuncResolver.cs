using System;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Resolves requests for <code>Func&lt;T&gt;</code>
  /// </summary>
  internal class FuncResolver : GenericResolver
  {
    public FuncResolver() 
      : base(typeof(InternalResolver<>), typeof(Func<>))
    {
    }

    private class InternalResolver<T> : IResolver<Func<T>>
    {
      public Func<T> Resolve(DependencyContainer container, ResolutionInformation resolutionInformation)
      {
        var factory = container.ResolveFactoryInternal(typeof(T), resolutionInformation);
        return () => (T) factory();
      }
    }
  }
}