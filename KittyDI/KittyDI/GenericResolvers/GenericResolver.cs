using System;
using System.Collections.Generic;
using KittyDI.Exceptions;

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
      _internalResolverType = internalResolverType;
      _resolvedType = resolvedType;
    }

    public bool Matches(Type genericType, Type[] typeParameters)
    {
      return genericType == _resolvedType && typeParameters.Length == _resolvedType.GetGenericArguments().Length;
    }

    Func<ResolutionInformation, object> IGenericResolver.Resolve(Type[] typeParameters)
    {
      var type = _internalResolverType
        .MakeGenericType(typeParameters);
      var resultType = _resolvedType.MakeGenericType(typeParameters);

      var method = type
        .GetMethod("Resolve");
      var instance = Activator.CreateInstance(type);
      return resolutionInformation =>
      {
        if (resolutionInformation.ResolutionChain.Contains(resultType))
        {
          throw new CircularDependencyException();
        }

        resolutionInformation.ResolutionChain.Push(resultType);

        var result = method.Invoke(instance, new object[] { resolutionInformation });

        resolutionInformation.ResolutionChain.Pop();

        return result;
      };
    }
  }
}