﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace KittyDI.GenericResolvers
{
  public class EnumerableResolver : GenericResolver
  {
    public EnumerableResolver() : base(typeof(InternalResolver<>), typeof(IEnumerable<>))
    {
    }

    private class InternalResolver<T> : IResolver<IEnumerable<T>>
    {
      public IEnumerable<T> Resolve(DependencyContainer container, ISet<Type> previousResolutions)
      {
        return GetRegistrations(container, typeof(T)).Select(x => x()).Cast<T>();
      }

      private IEnumerable<Func<object>> GetRegistrations(DependencyContainer container, Type innerType)
      {
        IEnumerable<Func<object>> result;
        container.MultipleRegistrations.TryGetValue(innerType, out result);

        result = result ?? Enumerable.Empty<Func<object>>();

        return result.Concat(container.Containers.SelectMany(x => GetRegistrations(x, innerType))).ToArray();
      }
    }
  }
}