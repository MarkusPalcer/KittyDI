using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Abstract class for resolvers for generic types
  /// </summary>
  public abstract class GenericResolver : IGenericResolver
  {
    internal static readonly List<IGenericResolver> GenericResolvers = new List<IGenericResolver>
    {
      new FuncResolver(),
      new EnumerableResolver()
    };

    public static void Register(IGenericResolver resolver)
    {
      GenericResolvers.Add(resolver);
    }

    private readonly Type _internalResolverType;
    private readonly Type _resolvedType;

    protected GenericResolver(Type internalResolverType, Type resolvedType)
    {
      this._internalResolverType = internalResolverType;
      this._resolvedType = resolvedType;
    }

    public bool Matches(Type genericType, Type[] typeParameters)
    {
      return genericType == _resolvedType && typeParameters.Length == _resolvedType.GetGenericArguments().Length;
    }

    public Func<object> Resolve(DependencyContainer container, Type[] typeParameters, ISet<Type> previousResolutions)
    {
      var type = _internalResolverType
        .MakeGenericType(typeParameters);
      var method = type
        .GetMethod("Resolve");
      var instance = Activator.CreateInstance(type);
      return () => method.Invoke(instance, new object[] { container, previousResolutions });
    }
  }
}