# Kitty Dependency-Injection

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

To resolve more than one instance of a type, you can resolve a `Func<T>` which is a function that resolves an instance each time it is called.
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

### Automatic registration

The class `Registrar` can be used to scan Assemblies for types to register.
The short usage instructions are:

- Use `Add` or `AddAssemblyOf<T>` to add Assemblies to the Registrar to be scanned
- Set `InterfaceHandling` to specify when implementing an interface triggers registration of a type
  - `NoInterfaceRegistration`: The registrar will ignore all interfaces 
  - `RegisterContractsOnly` (default): Types that implement interfaces which are decorated with the `Contract` attribute will be registered as implementation of the interface
  - `RegisterAllImplementedInterfaces`: Treat all interfaces as if they have the `Contract` attribute.
- Set `AbstractImplementationHandling` to specify when implementing an abstract class triggers registration of a type
  - `NoRegistrationOfAbstractImplementations`: The registrar will ignore all abstract classes 
  - `RegisterContractsOnly` (default): Types that implement abstract classes which are decorated with the `Contract` attribute will be registered as implementation of the abstract class
  - `RegisterAllImplementations`: Treat all abstract classes as if they have the `Contract` attribute.
- Set `TypeHandling` to specify when types will be registered (regardless of their implemented interfaces or abstract classes)
  - `NoTypeRegistration`: Don't register types (except for registering an implementation of a contract - see the other two properties)
  - `RegisterContractsOnly` (default): Register all types that have a `Contract` attribute
  - `RegisterAllTypes`: Register every type found in the assemblies
- Call either `CreateContainer` to create a new container with the current settings or call `RegisterToContainer` to perform registration of types and implementations to an existing container

If you don't set any of the properties, the default behavior will be to only consider types, interfaces and abstract classes that have been decorated with the `Contract` attribute



