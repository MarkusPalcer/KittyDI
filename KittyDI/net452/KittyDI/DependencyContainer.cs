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
        public ResolutionInformation(DependencyContainer container)
        {
            Container = container;
        }

        internal List<TypeInfo> ResolutionChain { get; } = new List<TypeInfo>();
        internal DependencyContainer Container { get; }

        internal Dictionary<TypeInfo, object> GivenInstances { get; } = new Dictionary<TypeInfo, object>();
    }

    /// <summary>
    /// A lightweight dependency injection container
    /// </summary>
    public class DependencyContainer : IDependencyContainer
    {
        private readonly Dictionary<TypeInfo, Func<ResolutionInformation, object>> _factories = new Dictionary<TypeInfo, Func<ResolutionInformation, object>>();
        internal readonly List<DependencyContainer> Containers = new List<DependencyContainer>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly List<TypeInfo> _servicesToInitialize = new List<TypeInfo>();
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
        /// Returns an instance of the requested type, resolving dependencies recursively
        /// </summary>
        /// <typeparam name="T">The requested type</typeparam>
        /// <returns>An instance of the requestsed type</returns>
        public T Resolve<T>()
        {
            return (T)ResolveFactoryInternal(typeof(T).GetTypeInfo())(CreateResolutionInformation());
        }

        #region Registration
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
            RegisterType(registeredType.GetTypeInfo());
        }

        private void RegisterType(TypeInfo registeredType)
        {
                        if (Mode == DependencyContainerMode.Locked)
            {
                throw new ContainerLockedException();
            }

            CreateFactory(registeredType);
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

            var contract = typeof(TContract).GetTypeInfo();

            if (!isSingleton)
            {
                AddFactory(contract, _ => (TContract)factory());
            }
            else
            {
                AddFactory(contract, CreateSingletonFactory(_ => (TContract)factory()));
            }
        }

        private void AddFactory(TypeInfo contract, Func<ResolutionInformation, object> factory)
        {
            if (_factories.ContainsKey(contract))
                throw new TypeAlreadyRegisteredException { ConflictingType = contract.AsType() };

            _factories[contract] = factory;
        }

        /// <summary>
        /// Instantiates all types that have a <see cref="SingletonAttribute"/> attribute with <see cref="SingletonAttribute.Create"/> set to <see cref="SingletonAttribute.CreationRule.CreateDurinServiceInitialization"/> 
        /// </summary>
        public void InitializeServices()
        {
            var resolutionInformation = CreateResolutionInformation();

            foreach (var type in _servicesToInitialize)
            {
                _factories[type](resolutionInformation);
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
            RegisterImplementation(contractType.GetTypeInfo(), implementationType.GetTypeInfo(), isSingleton);
        }

        private void RegisterImplementation(TypeInfo contractType, TypeInfo implementationType, bool isSingleton)
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

            // If there already is a factory for the implementation, use that
            if (!_factories.ContainsKey(implementationType))
                RegisterType(implementationType);

            // Act as if the contract has already been resolved
            var resolutionInformation = CreateResolutionInformation();
            resolutionInformation.ResolutionChain.Add(contractType);

            Func<ResolutionInformation, object> factory = ResolveFactoryInternal(implementationType);

            AddFactory(contractType, !isSingleton ? factory : CreateSingletonFactory(factory));
        }
        #endregion

        #region Resolution internals
        internal ResolutionInformation CreateResolutionInformation(TypeInfo resolvedType = null)
        {
            var resolutionInformation = new ResolutionInformation(this);
            if (resolvedType != null)
            {
                resolutionInformation.ResolutionChain.Add(resolvedType);
            }

            return resolutionInformation;
        }

        internal Func<ResolutionInformation, object> ResolveFactoryInternal(TypeInfo requestedType)
        {
            return FindExistingFactory(requestedType)
                   ?? ResolveFactoryForGenericType(requestedType)
                   ?? ResolveFactoryForUnknownType(requestedType);
        }

        private Func<ResolutionInformation, object> ResolveFactoryForUnknownType(TypeInfo requestedType)
        {
            if (_mode != DependencyContainerMode.Regular)
            {
                throw new ContainerLockedException();
            }

            CreateFactory(requestedType);
            return _factories[requestedType];
        }

        private void CreateFactory(TypeInfo requestedType)
        {
            var factory = FindConstructor(requestedType);

            var singletonAttribute = requestedType.GetCustomAttribute<SingletonAttribute>();
            if (singletonAttribute != null)
            {
                switch (singletonAttribute.Create)
                {
                    case SingletonAttribute.CreationRule.CreateWhenFirstResolved:
                        factory = CreateSingletonFactory(factory);
                        break;
                    case SingletonAttribute.CreationRule.CreateWhenRegistered:
                        var instance = CreateSingletonFactory(factory)(CreateResolutionInformation());
                        factory = _ => instance;
                        break;
                    case SingletonAttribute.CreationRule.CreateDurinServiceInitialization:
                        factory = CreateSingletonFactory(factory);
                        _servicesToInitialize.Add(requestedType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            AddFactory(requestedType, factory);
        }

        private Func<ResolutionInformation, object> ResolveFactoryForGenericType(TypeInfo requestedType)
        {
            if (!requestedType.IsGenericType) return null;

            var genericType = requestedType.GetGenericTypeDefinition().GetTypeInfo();
            var typeParameters = requestedType.GenericTypeArguments.Select(x => x.GetTypeInfo()).ToArray();

            return GenericResolver.GenericResolvers
              .FirstOrDefault(x => x.Matches(genericType, typeParameters))?.Resolve(typeParameters);
        }

        private Func<ResolutionInformation, object> FindExistingFactory(TypeInfo requestedType)
        {
            Func<ResolutionInformation, object> factory;
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

        private Func<ResolutionInformation, object> FindConstructor(TypeInfo resultType)
        {
            var constructors = resultType.DeclaredConstructors.ToArray();

            if (resultType.IsInterface)
            {
                throw new NoInterfaceImplementationGivenException { InterfaceType = resultType.AsType() };
            }

            var constructor = constructors.FirstOrDefault(x => x.GetParameters().Length == 0);
            if (constructor != null)
            {
                return _ => constructor.Invoke(new object[] { });
            }

            constructor = constructors.Length == 1
              ? constructors.First()
              : constructors.SingleOrDefault(x => x.GetCustomAttribute<ProvidingConstructorAttribute>() != null);

            if (constructor == null) throw new NoSuitableConstructorFoundException(resultType.AsType());

            return resolutionInformation =>
            {
                if (resolutionInformation.ResolutionChain.Contains(resultType))
                {
                    throw new CircularDependencyException();
                }

                resolutionInformation.ResolutionChain.Add(resultType);

                var parameters = constructor.GetParameters()
            .Select(param => ResolveDependency(param.ParameterType.GetTypeInfo(), resolutionInformation))
            .ToArray();

                var result = constructor.Invoke(parameters);

                resolutionInformation.ResolutionChain.RemoveAt(resolutionInformation.ResolutionChain.Count - 1);

                return result;
            };
        }

        private object ResolveDependency(TypeInfo dependencyType, ResolutionInformation ri)
        {
            object result;
            return ri.GivenInstances.TryGetValue(dependencyType, out result)
              ? result
              : ri.Container.ResolveFactoryInternal(dependencyType)(ri);
        }

        private Func<ResolutionInformation, object> CreateSingletonFactory<T>(Func<ResolutionInformation, T> factory)
        {
            var value = default(T);
            var created = false;

            return rI =>
            {
                if (created) return value;

                value = factory(rI);
                created = true;

                var disposableValue = value as IDisposable;
                if (disposableValue != null)
                {
                    _disposables.Add(disposableValue);
                }

                return value;
            };
        }
        #endregion

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
