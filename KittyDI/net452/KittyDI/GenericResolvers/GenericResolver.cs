using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
      new LazyResolver(),
      new FuncResolver1()
    };

        private readonly Type _internalResolverType;
        private readonly Type _resolvedType;

        protected GenericResolver(Type internalResolverType, Type resolvedType)
        {
            _internalResolverType = internalResolverType;
            _resolvedType = resolvedType;
        }

        public bool Matches(TypeInfo genericType, TypeInfo[] typeParameters)
        {
            return genericType.AsType() == _resolvedType;
        }

        Func<ResolutionInformation, object> IGenericResolver.Resolve(TypeInfo[] typeParameters)
        {
            var typeArguments = typeParameters.Select(x => x.AsType()).ToArray();
            var type = _internalResolverType
            .MakeGenericType(typeArguments);
            var resultType = _resolvedType.MakeGenericType(typeArguments);
            var resultTypeInfo = resultType.GetTypeInfo();

            var method = type.GetTypeInfo().GetDeclaredMethod("Resolve");
            var instance = Activator.CreateInstance(type);
            return resolutionInformation =>
            {
                if (resolutionInformation.ResolutionChain.Contains(resultTypeInfo))
                {
                    throw new CircularDependencyException();
                }


                resolutionInformation.ResolutionChain.Add(resultTypeInfo);

                var result = method.Invoke(instance, new object[] { resolutionInformation });

                resolutionInformation.ResolutionChain.RemoveAt(resolutionInformation.ResolutionChain.Count - 1);

                return result;
            };
        }
    }
}