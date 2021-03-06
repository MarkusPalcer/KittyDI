using System;
using System.Reflection;

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
        var factory = resolutionInformation.Container.ResolveFactoryInternal(typeof(T).GetTypeInfo());
        return () => (T) factory(resolutionInformation.Container.CreateResolutionInformation(typeof(Func<T>).GetTypeInfo()));
      }
    }
  }

  internal class FuncResolver1 : GenericResolver
  {
    public FuncResolver1() : base(typeof(InternalResolver<,>), typeof(Func<,>))
    {
    }

    private class InternalResolver<TIn, TOut> : IResolver<Func<TIn, TOut>>
    {
      public Func<TIn, TOut> Resolve(ResolutionInformation resolutionInformation)
      {
        var factory = resolutionInformation.Container.ResolveFactoryInternal(typeof(TOut).GetTypeInfo());
        return p1 =>
        {
          var ri = resolutionInformation.Container.CreateResolutionInformation(typeof(Func<TIn, TOut>).GetTypeInfo());
          ri.GivenInstances[typeof(TIn).GetTypeInfo()] = p1;

          return (TOut) factory(ri);
        };
      }
    }
  }
}