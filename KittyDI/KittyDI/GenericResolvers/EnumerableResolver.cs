using System;
using System.Collections.Generic;
using System.Linq;

namespace KittyDI.GenericResolvers
{
  public class EnumerableResolver :IGenericResolver
  {
    public Func<object> Resolve(DependencyContainer container, Type[] typeParameters, ISet<Type> previousResolutions)
    {
      var innerType = typeParameters.Single();
      var converterType = typeof(FactoryExecutor<>).MakeGenericType(innerType);
      var converterMethod = converterType.GetMethod("Work");
      var converter = Activator.CreateInstance(converterType);

      return () => converterMethod.Invoke(converter, new object[] { GetRegistrations(container, innerType) });
    }

    private IEnumerable<Func<object>> GetRegistrations(DependencyContainer container, Type innerType)
    {
      IEnumerable<Func<object>> result;
      container.MultipleRegistrations.TryGetValue(innerType, out result);

      result = result ?? Enumerable.Empty<Func<object>>();

      return result.Concat(container.Containers.SelectMany(x => GetRegistrations(x, innerType))).ToArray();
    }

    public bool Matches(Type genericType, Type[] typeParameters)
    {
      return genericType == typeof(IEnumerable<>) && typeParameters.Length == 1;
    }

    /// <summary>
    /// Helper class to execute a list of factories, transforming it into a list of instances.
    /// </summary>
    /// <typeparam name="T">The instance type</typeparam>
    private class FactoryExecutor<T>
    {
      public IEnumerable<T> Work(IEnumerable<Func<object>> factories)
      {
        return factories.Select(x => x()).Cast<T>();
      }
    }
  }
}