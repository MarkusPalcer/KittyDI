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
      public Func<T> Resolve(ResolutionInformation resolutionInformation)
      {
        var factory = resolutionInformation.Container.ResolveFactoryInternal(typeof(T));
        return () => (T) factory(resolutionInformation.Container.CreateResolutionInformation(typeof(Func<T>)));
      }
    }
  }
}