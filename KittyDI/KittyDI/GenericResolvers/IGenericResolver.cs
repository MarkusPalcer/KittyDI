using System;
using System.Collections.Generic;

namespace KittyDI.GenericResolvers
{
  public interface IGenericResolver
  {
    bool Matches(Type genericType, Type[] typeParameters);

    Func<object> Resolve(DependencyContainer container, Type[] typeParameters, ISet<Type> previousResolutions);
  }
}