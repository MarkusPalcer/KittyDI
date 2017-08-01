using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittyDI.Attribute;
using KittyDI.Exceptions;
using KittyDI.GenericResolvers;

namespace KittyDI
{
  internal class ResolutionInformation
  {
    internal Stack<Type> ResolutionChain { get; } = new Stack<Type>();

    internal ResolutionInformation Clone()
    {
      var result = new ResolutionInformation();
      foreach (var type in ResolutionChain)
      {
        result.ResolutionChain.Push(type);
      }

      return result;
    }
  }

  /// <summary>
  /// A lightweight dependency injection container
  /// </summary>
  public class DependencyContainer : IDependencyContainer
  {
    private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
    internal readonly List<DependencyContainer> Containers = new List<DependencyContainer>();
    private readonly List<IDisposable> _disposables = new List<IDisposable>();
    private readonly List<Type> _servicesToInitialize = new List<Type>();
    internal readonly Dictionary<Type, IEnumerable<Func<object>>> MultipleRegistrations = new Dictionary<Type, IEnumerable<Func<object>>>();
    private DependencyContainerMode _mode = DependencyContainerMode.Regular;

    /// <summary>
    /// Creates a new dependency injection container
    /// </summary>
    public DependencyContainer()
    {
      RegisterFactory(CreateChild);
      RegisterImplementation<IDependencyContainer, DependencyContainer>();
    }

    public DependencyContainerMode Mode
    {
      get { return _mode; }
      set
      {
        if (_mode == DependencyContainerMode.Locked)
        {
          throw new InvalidOperationException("Locked mode can not be changed");
        }

        _mode = value;
      }
    }

    /// <summary>
    /// Registers a type to the dependency container, so it can be resolved later
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public void RegisterType<T>()
    {
      RegisterType(typeof(T));
    }

    /// <summary>
    /// Registers a type to the dependency container, so it can be resolved later
    /// </summary>
    /// <param name="registeredType">The type to register</param>
    public void RegisterType(Type registeredType)
    {
      if (Mode == DependencyContainerMode.Locked)
      {
        throw new ContainerLockedException();
      }

      // Act as if the contract has already been resolved
      var resolutionInformation = new ResolutionInformation();
      resolutionInformation.ResolutionChain.Push(registeredType);

      CreateFactory(registeredType, resolutionInformation);
    }

    /// <summary>
    /// Returns an instance of the requested type, resolving dependencies recursively
    /// </summary>
    /// <typeparam name="T">The requested type</typeparam>
    /// <returns>An instance of the requestsed type</returns>
    public T Resolve<T>()
    {
      return (T) ResolveFactoryInternal(typeof(T), new ResolutionInformation())();
    }

    /// <summary>
    /// Register a function that is used to create an instance of a type
    /// </summary>
    /// <typeparam name="T">The type that the function creates</typeparam>
    /// <param name="factory">The factory function</param>
    /// <param name="isSingleton">If set to <code>true</code> only one instance of the type will be created and then be returned on subsequent tries of resolving the type.</param>
    public void RegisterFactory<T>(Func<T> factory, bool isSingleton = false)
    {
      RegisterFactory<T, T>(factory, isSingleton);
    }

    /// <summary>
    /// Register a function that is used to create an instance of a type and registers the factory for resolution of a contract.
    /// </summary>
    /// <typeparam name="TImplementation">The type that the function creates</typeparam>
    /// <typeparam name="TContract">The contract type that the function satisfies</typeparam>
    /// <param name="factory">The factory function</param>
    /// <param name="isSingleton">If set to <code>true</code> only one instance of the type will be created and then be returned on subsequent tries of resolving the type.</param>
    public void RegisterFactory<TContract, TImplementation>(Func<TImplementation> factory, bool isSingleton = false)
    where TImplementation : TContract
    {
      if (Mode == DependencyContainerMode.Locked)
      {
        throw new ContainerLockedException();
      }

      if (!isSingleton)
      {
        AddFactory(typeof(TContract), () => factory());
      }
      else
      {
        AddFactory(typeof(TContract), CreateSingletonFactory(factory));
      }
    }

    private void AddFactory(Type contract, Func<object> factory)
    {
      if (!_factories.ContainsKey(contract))
      {
        _factories[contract] = factory;
        MultipleRegistrations[contract] = new[] {factory};
      }
      else
      {
        _factories[contract] = () =>
        {
          throw new MultipleTypesRegisteredException {RequestedType = contract};
        };
        MultipleRegistrations[contract] = MultipleRegistrations[contract].Concat(new[] {factory});
      }
    }


    /// <summary>
    /// Instantiates all types that have a <see cref="SingletonAttribute"/> attribute with <see cref="SingletonAttribute.Create"/> set to <see cref="SingletonAttribute.CreationRule.CreateDurinServiceInitialization"/> 
    /// </summary>
    public void InitializeServices()
    {
      foreach (var type in _servicesToInitialize)
      {
        var instance = _factories[type]();
        _factories[type] = () => instance;
      }
    }

    /// <summary>
    /// Registers an instance of a type as singleton
    /// If the type of the instance is resolvd, the same instance is always returned.
    /// </summary>
    /// <typeparam name="T">The type of the instance</typeparam>
    /// <param name="instance">The singleton to register</param>
    public void RegisterInstance<T>(T instance)
    {
      RegisterInstance<T, T>(instance);
    }

    /// <summary>
    /// Registers an instance of an object as singleton to be resolved when a contract is requested
    /// </summary>
    /// <param name="instance">The instance to register</param>
    /// <typeparam name="TContract">The type of the contract this instance satisfied</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation</typeparam>
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

    /// <summary>
    /// Registers a type to implement a contract
    /// </summary>
    /// <typeparam name="TContract">The contract the type satisfies</typeparam>
    /// <typeparam name="TImplementation">The type that satisfies the contract</typeparam>
    /// <param name="isSingleton">If set to <code>true</code> only a single instance of the type will be created and returned on subsequent resolutions of the contract</param>
    public void RegisterImplementation<TContract, TImplementation>(bool isSingleton = false)
      where TImplementation : TContract
    {
      RegisterImplementation(typeof(TContract), typeof(TImplementation), isSingleton);
    }

    /// <summary>
    /// Registers a type to implement a contract
    /// </summary>
    /// <param name="contractType">The contract the type satisfies</param>
    /// <param name="implementationType">The type that satisfies the contract</param>
    /// <param name="isSingleton">If set to <code>true</code> only a single instance of the type will be created and returned on subsequent resolutions of the contract</param>
    public void RegisterImplementation(Type contractType, Type implementationType, bool isSingleton = false)
    {
      if (!contractType.IsAssignableFrom(implementationType))
      {
        throw new ArgumentException(
          $"The implementation type needs to actually derive from or implement the contract type. {implementationType.Name} does not derive from or implement {contractType.Name}.");
      }

      if (Mode == DependencyContainerMode.Locked)
      {
        throw new ContainerLockedException();
      }

      RegisterType(implementationType);

      // Act as if the contract has already been resolved
      var resolutionInformation = new ResolutionInformation();
      resolutionInformation.ResolutionChain.Push(contractType);

      Func<object> factory = () => ResolveFactoryInternal(implementationType, resolutionInformation)();

      AddFactory(contractType, !isSingleton ? factory : CreateSingletonFactory(factory));
    }

    internal Func<object> ResolveFactoryInternal(Type requestedType, ResolutionInformation resolutionInformation)
    {
      if (resolutionInformation.ResolutionChain.Contains(requestedType))
      {
        throw new CircularDependencyException();
      }

      resolutionInformation.ResolutionChain.Push(requestedType);

      var factory = FindExistingFactory(requestedType) 
        ??  ResolveFactoryForGenericType(requestedType, resolutionInformation)
        ??  ResolveFactoryForUnknownType(requestedType, resolutionInformation);

      resolutionInformation.ResolutionChain.Pop();
        
      return factory;
    } 

    private Func<object> ResolveFactoryForUnknownType(Type requestedType, ResolutionInformation resolutionInformation)
    {
      if (_mode != DependencyContainerMode.Regular)
      {
        throw new ContainerLockedException();
      }

      CreateFactory(requestedType, resolutionInformation);
      return _factories[requestedType];
    }

    private void CreateFactory(Type requestedType, ResolutionInformation resolutionInformation)
    {
      var factory = FindConstructor(requestedType, resolutionInformation);

      var singletonAttribute = requestedType.GetCustomAttribute<SingletonAttribute>();
      if (singletonAttribute != null)
      {
        switch (singletonAttribute.Create)
        {
          case SingletonAttribute.CreationRule.CreateWhenFirstResolved:
            factory = CreateSingletonFactory(factory);
            break;
          case SingletonAttribute.CreationRule.CreateWhenRegistered:
            var instance = CreateSingletonFactory(factory)();
            factory = () => instance;
            break;
          case SingletonAttribute.CreationRule.CreateDurinServiceInitialization:
            factory = CreateSingletonFactory(factory);
            _servicesToInitialize.Add(requestedType);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }

      _factories[requestedType] = factory;
    }

    private Func<object> ResolveFactoryForGenericType(Type requestedType, ResolutionInformation resolutionInformation)
    {
      if (!requestedType.IsGenericType) return null;

      var genericType = requestedType.GetGenericTypeDefinition();
      var typeParameters = requestedType.GetGenericArguments();
      
      return GenericResolver.GenericResolvers
        .FirstOrDefault(x => x.Matches(genericType, typeParameters))
        ?.Resolve(this, typeParameters, resolutionInformation);
    }

    private Func<object> FindExistingFactory(Type requestedType)
    {
      Func<object> factory;
      if (_factories.TryGetValue(requestedType, out factory))
      {
        return factory;
      }

      foreach (var container in Containers)
      {
        factory = container.FindExistingFactory(requestedType);
        if (factory != null)
        {
          return factory;
        }
      }

      return null;
    }

    private Func<object> FindConstructor(Type resultType, ResolutionInformation resolutionInformation)
    {
      var constructors = resultType.GetConstructors();

      if (resultType.IsInterface)
      {
        throw new NoInterfaceImplementationGivenException { InterfaceType = resultType };
      }

      var constructor = constructors.FirstOrDefault(x => x.GetParameters().Length == 0);
      if (constructor != null)
      {
        return () => constructor.Invoke(new object[] { });
      }

      constructor = constructors.Length == 1 
        ? constructors.First() 
        : constructors.SingleOrDefault(x => x.GetCustomAttribute<ProvidingConstructorAttribute>() != null);

      if (constructor == null) throw new NoSuitableConstructorFoundException(resultType);

      var information = resolutionInformation.Clone();
      return () =>
      {
        var parameterFactories = constructor.GetParameters().Select(param => ResolveFactoryInternal(param.ParameterType, information)).ToArray();
        return constructor.Invoke(parameterFactories.Select(x => x()).ToArray());
      };
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

    /// <summary>
    /// Adds the given container to this, thus giving this container and its content access to the content of the added container
    /// </summary>
    /// <param name="addedContainer">The container to add</param>
    public void AddContainer(DependencyContainer addedContainer)
    {
      Containers.Add(addedContainer);
    }

    /// <summary>
    /// Creates a child container which can access its parents content but not vice versa
    /// </summary>
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
