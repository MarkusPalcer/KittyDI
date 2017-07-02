# Kitty Dependency Injection

## What it is 

KittyDI is a lightweight dependency injection container.
I built it mainly to understand how dependency injection containers work.

## Documentation

While this documentation is only a brief show of features that KittyDI offers, there is a full fledged, prosaic [Tutorial](doc/tutorial.md).

### Resolving an instance of a class

To resolve an instance of a class, use the `Resolve<T>` method. 
KittyDI will try to determine how to resolve the type in the following order:
- If the type has been resolved previously, the existing factory for the type will be used
- If the type has a parameterless constructor, it will be used
- If the type has only one constructor, its parameters will be resolved and it wil be used
- If the type has multiple constructors that use parameters and one is marked with the `ProvidingConstructor` attribute, its parameters will be resolved and it will be used
- If all of the above fails, an `NoSuitableConstructorFoundException` is thrown

As soon as the type is resolved for a first time, KittyDI knows how to instantiate the type and all the recursive dependencies.

### Resolving a factory for a type

To resolve more than one instance of a type, you can use `ResolveFactory<T>´ to retrieve a `Func<T>` which resolves an instance.
As soon as the factory is resolved for a first time, KittyDI knows how to instantiate the type and all the recursive dependencies.

### Requesting a type using an interface as contract

Trying to resolve an interface will by default throw a `NoSuitableConstructorFoundException`.
In order to resolve an instance of a type which implements the interface, use `RegisterInstance<TContract,TImplementation>`.
This tells KittyDI to resolve `TImplementation` instead of the requested `TContract`
Registering an implementation leands KittyDI to know how to instantiate the registered implementation and its recursive dependencies.

### Singletons

There are multiple ways to tell KittyDI to only create a single instance of a type and serve that instance on each resolution.

- Some `Register*`-functions have an optional boolean parameter `isSingleton`.
- Decorating the registered type with the `Singleton` attribute.
- Registering a already created instance using `RegisterInstance´

By default KittyDI will create instances of singletons when they are first resolved. This behavior can be influenced when the `Singleton`-Attribute is used by setting the `Create`-property.
There are three possible settings:

- Create the instance on first resolution of the type (default)
- Create the instance immediately on registration
- Create the instance when `InitializeServices` is called

