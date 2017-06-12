using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittyDI.Attribute;
using KittyDI.Exceptions;

namespace KittyDI
{
  public class DependencyContainer : IDependencyContainer
  {
    private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
    private readonly List<DependencyContainer> _containers = new List<DependencyContainer>();
    private readonly List<IDisposable> _disposables = new List<IDisposable>();

    public DependencyContainer()
    {
      RegisterFactory(CreateChild);
      RegisterImplementation<IDependencyContainer, DependencyContainer>();
    }

    public T Resolve<T>()
    {
      return ResolveFactory<T>()();
    }

    public Func<T> ResolveFactory<T>()
    {
      var factory = ResolveFactoryInternal(typeof(T), new HashSet<Type>());

      return () => (T) factory();
    }

    public void RegisterFactory<T>(Func<T> factory, bool isSingleton = false)
    {
      RegisterFactory<T, T>(factory, isSingleton);
    }
    
    public void RegisterFactory<TContract, TImplementation>(Func<TImplementation> factory, bool isSingleton = false)
    where TImplementation : TContract
    {
      if (!isSingleton)
      {
        _factories.Add(typeof(TContract), () => factory());
      }
      else
      {
        _factories.Add(typeof(TContract), CreateSingletonFactory(factory));
      }
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

      var disposableInstance = instance as IDisposable;
      if (disposableInstance != null)
      {
        _disposables.Add(disposableInstance);
      }
    }

    public void RegisterImplementation<TContract, TImplementation>(bool isSingleton = false)
      where TImplementation : TContract
    {
      Func<object> factory = () => ResolveFactoryInternal(typeof(TImplementation), new HashSet<Type>(new[] { typeof(TContract) }))();

      if (!isSingleton)
      {
        _factories.Add(typeof(TContract), factory);
      }
      else
      {
        _factories.Add(typeof(TContract), CreateSingletonFactory(factory));
      }
    }

    private Func<object> ResolveFactoryInternal(Type requestedType, ISet<Type> previousChainedRequests)
    {
      if (previousChainedRequests.Contains(requestedType))
      {
        throw new CircularDependencyException();
      }

      Func<object> factory;
      if (_factories.TryGetValue(requestedType, out factory))
      {
        return factory;
      }

      foreach (var container in _containers)
      {
        if (container._factories.TryGetValue(requestedType, out factory))
        {
          return factory;
        }
      }

      factory = CreateFactory(requestedType, previousChainedRequests);

      if (requestedType.GetCustomAttribute<SingletonAttribute>() != null)
      {
        factory = CreateSingletonFactory(factory);
      }

      _factories[requestedType] = factory;


      return factory;
    }

    private Func<object> CreateFactory(Type resultType, ISet<Type> previousChainedRequests)
    {
      var constructors = resultType.GetConstructors();

      if (resultType.IsInterface)
      {
        throw new NoInterfaceImplementationGivenException {InterfaceType = resultType};
      }

      var constructor = constructors.FirstOrDefault(x => x.GetParameters().Length == 0);
      if (constructor != null)
      {
        return () => constructor.Invoke(new object[] { });
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
        var chainedRequests = new HashSet<Type>(previousChainedRequests.Concat(new[] { resultType }));
        var parameterFactories = constructor.GetParameters().Select(param => ResolveFactoryInternal(param.ParameterType, chainedRequests)).ToArray();
        return () => constructor.Invoke(parameterFactories.Select(x => x()).ToArray());
      }

      throw new NoSuitableConstructorFoundException(resultType);
    }

    private Func<object> CreateSingletonFactory<T>(Func<T> factory) 
    {
      var buffer = new Lazy<T>(() =>
      {
        var value = factory();
        var disposableValue = value as IDisposable;
        if (disposableValue != null)
        {
          _disposables.Add(disposableValue);
        }

        return value;
      });

      Func<object> newFactory = () => buffer.Value;
      return newFactory;
    }

    public void AddContainer(DependencyContainer addedContainer)
    {
      _containers.Add(addedContainer);
    }

    public DependencyContainer CreateChild()
    {
      var child = new DependencyContainer();
      child.AddContainer(this);
      _disposables.Add(child);
      return child;
    }

    public void Dispose()
    {
      foreach (var singleton in _disposables) 
      {
        singleton.Dispose();
      }
    }
  }
}
