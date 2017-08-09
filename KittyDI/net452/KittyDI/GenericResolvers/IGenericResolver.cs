using System;
using System.Collections.Generic;
using System.Reflection;

namespace KittyDI.GenericResolvers
{
  /// <summary>
  /// Interface for resolvers of generic types
  /// </summary>
  internal interface IGenericResolver
  {
    /// <summary>
    /// Returns a value that determines if the resolver is able to resolve the requested type.
    /// </summary>
    /// <param name="genericType">The generic type definition of the requested type</param>
    /// <param name="typeParameters">The type parameters that turn the generic type definition into the requested type</param>
    /// <returns><code>true</code>, if the resolver can resolve the generic type</returns>
    bool Matches(TypeInfo genericType, TypeInfo[] typeParameters);

    /// <summary>
    /// Returns a factory that returns the requested generic type
    /// </summary>
    /// <param name="typeParameters">The type parameters that turn the generic type definition into the requested type</param>
    /// <returns>A factory that returns the requested generic type</returns>
    Func<ResolutionInformation, object> Resolve(TypeInfo[] typeParameters);
  }
}