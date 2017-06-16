using System;
using KittyDI.Attribute;

namespace KittyDI
{
  public interface IDependencyContainer: IDisposable
  {
    /// <summary>
    /// Registers a type to the dependency container, so it can be resolved later
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    void RegisterType<T>();

    /// <summary>
    /// Registers a type to the dependency container, so it can be resolved later
    /// </summary>
    /// <param name="registeredType">The type to register</param>
    void RegisterType(Type registeredType);

    /// <summary>
    /// Returns an instance of the requested type, resolving dependencies recursively
    /// </summary>
    /// <typeparam name="T">The requested type</typeparam>
    /// <returns>An instance of the requestsed type</returns>
    T Resolve<T>();

    /// <summary>
    /// Returns a function that creates instances of the requested type, resolving dependencies recursively
    /// </summary>
    /// <typeparam name="T">The requested type</typeparam>
    /// <returns>A function that creates instances </returns>
    Func<T> ResolveFactory<T>();

    /// <summary>
    /// Register a function that is used to create an instance of a type
    /// </summary>
    /// <typeparam name="T">The type that the function creates</typeparam>
    /// <param name="factory">The factory function</param>
    /// <param name="isSingleton">If set to <code>true</code> only one instance of the type will be created and then be returned on subsequent tries of resolving the type.</param>
    void RegisterFactory<T>(Func<T> factory, bool isSingleton = false);

    /// <summary>
    /// Register a function that is used to create an instance of a type and registers the factory for resolution of a contract.
    /// </summary>
    /// <typeparam name="TImplementation">The type that the function creates</typeparam>
    /// <typeparam name="TContract">The contract type that the function satisfies</typeparam>
    /// <param name="factory">The factory function</param>
    /// <param name="isSingleton">If set to <code>true</code> only one instance of the type will be created and then be returned on subsequent tries of resolving the type.</param>
    void RegisterFactory<TContract, TImplementation>(Func<TImplementation> factory, bool isSingleton = false)
      where TImplementation : TContract;

    /// <summary>
    /// Instantiates all types that have a <see cref="SingletonAttribute"/> attribute with <see cref="SingletonAttribute.Create"/> set to <see cref="SingletonAttribute.CreationRule.CreateDurinServiceInitialization"/> 
    /// </summary>
    void InitializeServices();

    /// <summary>
    /// Registers an instance of a type as singleton
    /// If the type of the instance is resolvd, the same instance is always returned.
    /// </summary>
    /// <typeparam name="T">The type of the instance</typeparam>
    /// <param name="instance">The singleton to register</param>
    void RegisterInstance<T>(T instance);

    /// <summary>
    /// Registers an instance of an object as singleton to be resolved when a contract is requested
    /// </summary>
    /// <param name="instance">The instance to register</param>
    /// <typeparam name="TContract">The type of the contract this instance satisfied</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation</typeparam>
    void RegisterInstance<TContract, TImplementation>(TImplementation instance)
      where TImplementation : TContract;

    /// <summary>
    /// Registers a type to implement a contract
    /// </summary>
    /// <typeparam name="TContract">The contract the type satisfies</typeparam>
    /// <typeparam name="TImplementation">The type that satisfies the contract</typeparam>
    /// <param name="isSingleton">If set to <code>true</code> only a single instance of the type will be created and returned on subsequent resolutions of the contract</param>
    void RegisterImplementation<TContract, TImplementation>(bool isSingleton = false)
      where TImplementation : TContract;

    /// <summary>
    /// Registers a type to implement a contract
    /// </summary>
    /// <param name="contractType">The contract the type satisfies</param>
    /// <param name="implementationType">The type that satisfies the contract</param>
    /// <param name="isSingleton">If set to <code>true</code> only a single instance of the type will be created and returned on subsequent resolutions of the contract</param>
    void RegisterImplementation(Type contractType, Type implementationType, bool isSingleton = false);

    void AddContainer(DependencyContainer addedContainer);

    DependencyContainer CreateChild();
  }
}