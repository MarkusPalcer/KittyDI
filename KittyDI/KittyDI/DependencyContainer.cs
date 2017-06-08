using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KittyDI
{
  [AttributeUsage(AttributeTargets.Constructor)]
  public class ProvidingConstructorAttribute : Attribute
  {
    
  }

  public interface IDependencyContainer
  {
    T Resolve<T>();
  }

  public class DependencyException : Exception { }

  public class NoSuitableConstructorFoundException : DependencyException
  {
    public NoSuitableConstructorFoundException(Type targetType)
    {
      TargetType = targetType;
    }

    public Type TargetType { get; set; }
  }

  public class DependencyContainer : IDependencyContainer
  {
    private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

    public T Resolve<T>()
    {
      return ResolveFactory<T>()();
    }

    public Func<T> ResolveFactory<T>()
    {
      var factory = ResolveFactory(typeof(T));

      return () => (T) factory();
    }

    private Func<object> ResolveFactory(Type T)
    {
      Func<object> factory;
      if (_factories.TryGetValue(T, out factory))
      {
        return factory;
      }

      factory = CreateFactory(T);
      _factories[T] = factory;
      return factory;
    }

    private Func<object> CreateFactory(Type resultType)
    {
      var constructors = resultType.GetConstructors();

      var constructor = constructors.FirstOrDefault(x => x.GetParameters().Length == 0);
      if (constructor != null)
      {
        return () => constructor.Invoke(new object[] {});
      }

      if (constructors.Length == 1)
      {
        constructor = constructors.First();
      }
      else
      {
        constructor = constructors.SingleOrDefault(x => x.GetCustomAttribute<ProvidingConstructorAttribute>() != null);
      }
      
      if (constructor != null)
      {
        var parameterFactories = constructor.GetParameters().Select(param => ResolveFactory(param.ParameterType)).ToArray();
        return () => constructor.Invoke(parameterFactories.Select(x => x()).ToArray());
      }

      throw new NoSuitableConstructorFoundException(resultType);
    }

    public void RegisterFactory<T>(Func<T> factory)
    {
      RegisterFactory<T, T>(factory);
    }

    public void RegisterFactory<TContract, TImplementation>(Func<TImplementation> factory)
    where TImplementation : TContract
    {
      _factories.Add(typeof(TContract), () => factory());
    }

    public void RegisterInstance<T>(T instance)
    {
      RegisterInstance<T, T>(instance);
    }

    /// <summary>
    /// Registers an instance of an object as singleton
    /// </summary>
    /// <param name="instance"></param>
    public void RegisterInstance<TContract, TImplementation>(TImplementation instance)
    where TImplementation : TContract
    {
      RegisterFactory<TContract, TImplementation>(() => instance);
    }

    public void RegisterImplementation<TContract, TImplementation>()
      where TImplementation : TContract
    {
      _factories.Add(typeof(TContract), ResolveFactory(typeof(TImplementation)));
    }
  }
}
