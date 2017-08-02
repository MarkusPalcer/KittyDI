using System;
using System.Collections.Generic;
using System.Linq;

namespace KittyDI.GenericResolvers
{
  internal class EnumerableResolver : GenericResolver
  {
    public EnumerableResolver() : base(typeof(InternalResolver<>), typeof(IEnumerable<>))
    {
    }

    private class InternalResolver<T> : IResolver<IEnumerable<T>>
    {
      public IEnumerable<T> Resolve(ResolutionInformation resolutionInformation)
      {
        return GetRegistrations(resolutionInformation.Container, typeof(T)).Select(x => x(resolutionInformation)).Cast<T>();
      }

      private IEnumerable<Func<ResolutionInformation, object>> GetRegistrations(DependencyContainer container, Type innerType)
      {
        IEnumerable<Func<ResolutionInformation, object>> result;
        container.MultipleRegistrations.TryGetValue(innerType, out result);

        result = result ?? Enumerable.Empty<Func<ResolutionInformation, object>>();

        return result.Concat(container.Containers.SelectMany(x => GetRegistrations(x, innerType))).ToArray();
      }
    }
  }
}