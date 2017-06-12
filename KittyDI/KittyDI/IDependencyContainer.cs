using System;

namespace KittyDI
{
  public interface IDependencyContainer: IDisposable
  {
    T Resolve<T>();
    Func<T> ResolveFactory<T>();
    void RegisterFactory<T>(Func<T> factory, bool isSingleton = false);

    void RegisterFactory<TContract, TImplementation>(Func<TImplementation> factory, bool isSingleton = false)
      where TImplementation : TContract;

    void RegisterInstance<T>(T instance);

    /// <summary>
    /// Registers an instance of an object as singleton
    /// </summary>
    /// <param name="instance"></param>
    void RegisterInstance<TContract, TImplementation>(TImplementation instance)
      where TImplementation : TContract;

    void RegisterImplementation<TContract, TImplementation>(bool isSingleton = false)
      where TImplementation : TContract;

    void AddContainer(DependencyContainer addedContainer);
    DependencyContainer CreateChild();
  }
}