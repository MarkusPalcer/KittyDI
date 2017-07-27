using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Abstract class for resolvers for generic types
  /// </summary>
  public abstract class GenericResolver : IGenericResolver
  {
    /// <summary>
    /// Interface to help implementing the Resolve method in the internal resolver
    /// </summary>
    /// <typeparam name="TResolved">The resolved type</typeparam>
    public interface IResolver<out TResolved>
    {
      /// <summary>
      /// Resolves the actual factory
      /// </summary>
      /// <param name="container">The container to scan for resolution</param>
      /// <param name="previousResolutions">
      /// A set containing all previously requested types (including the current one).
      /// It is used for circular dependency detection.
      /// </param>
      /// <returns>A factory that returns the requested generic type</returns>
      TResolved Resolve(DependencyContainer container, ISet<Type> previousResolutions);
    }

    internal static readonly List<IGenericResolver> GenericResolvers = new List<IGenericResolver>
    {
      new FuncResolver(),
      new EnumerableResolver(), 
      new LazyResolver()
    };

    /// <summary>
    /// Registers a custom resolver for generic types
    /// </summary>
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