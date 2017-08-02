using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Abstract class for resolvers for generic types
  /// </summary>
  internal abstract class GenericResolver : IGenericResolver
  {
    /// <summary>
    /// Interface to help implementing the Resolve method in the internal resolver
    /// </summary>
    /// <typeparam name="TResolved">The resolved type</typeparam>
    internal interface IResolver<out TResolved>
    {
      /// <summary>
      /// Resolves the actual factory
      /// </summary>
      /// <param name="resolutionInformation">
      /// An object containing all information about the current resolution process
      /// </param>
      /// <returns>A factory that returns the requested generic type</returns>
      TResolved Resolve(ResolutionInformation resolutionInformation);
    }

    internal static readonly List<IGenericResolver> GenericResolvers = new List<IGenericResolver>
    {
      new FuncResolver(),
      new EnumerableResolver(), 
      new LazyResolver()
    };
    
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

    Func<object> IGenericResolver.Resolve(Type[] typeParameters, ResolutionInformation resolutionInformation)
    {
      var type = _internalResolverType
        .MakeGenericType(typeParameters);
      var method = type
        .GetMethod("Resolve");
      var instance = Activator.CreateInstance(type);
      return () => method.Invoke(instance, new object[] { resolutionInformation });
    }
  }
}