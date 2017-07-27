# KittyDI Tutorial
## Resolving types

Before you can resolve any type, you need to create an instance of the `DependencyContainer` class.
This class is the heart of KittyDI.

To resolve a type, simply use the method `Resolve<T>`:

```C#
var container = new DependencyContainer();
var someThing = container.Resolve<Thing>();
```

But wait. How does KittyDI know how to do that?
If you don't tell it how to resolve `Thing` it will use reflection to take a look at its constructor(s).

Well, in the most simplest case, the constructor signature looks like this:
```C#
public Thing() {
```
The kitty is lazy, so when it encounters a parameterless constructor, it automatically uses it to resolve the requested type.

Now I hear you say *"What? Why would I need an DI-Container for calling a parameterless constructor?"*
Yes, you're right, you can do that with `new Thing()` too. But bear with me, we'll be getting into more complex scenarios.
The main reason to use DependencyInjection is - as the name suggests - to inject dependencies.
So how about `Thing` not having an empty constructor. How about it has a constructor that relies on dependencies:
```C#
public Thing(SomeDependency dep) {
```
If that's the only constructor in the class, KittyDI will use it. 
In order to satisfy the dependencies (`SomeDependency` in this case) it will resolve each parameter of the constructor as if you had called `Resolve<T>` for it.
So in this case it will call `Resolve<SomeDependency>()`, take the result and pass it as first parameter for the constructor of `Thing`.

For now we only talked about classes with a single constructor. But even for classes with multiple constructors, KittyDI can help you.
If there's more than one constructor in your class and no parameterless constructor present (remember: your kitty is lazy and always uses a parameterless constructor if present), KittyDI doesn't know what to do.
It'll panic and throw an `NoSuitableConstructorFoundException`.
Don't make the kitty panic. It's so easy to tell it which constructor to use. Just decorate it with the `ProvidingConstructorAttribute` like this:
```C#
public Thing(SomeDependency dep) {
	// Do something
}

[ProvidingConstructor]
public Thing(SomeDependency dep1, AnotherDependency dep2) {
	// Do something
}
```
In this sample KittyDI will use the constructor with two parameters to create an instance of `Thing` and reolve `SomeDependency` and `AnotherDependency` on the way.

Another thing that is confusing to a DI framework is circular dependencies.
Well, luckily you're using KittyDI. PuppyDi for example would now spin around in circles, endlessly chasing its tail.
But KittyDI is more intelligent: It leaves cute little pawprints where it walks while resolving a type, so it recognizes that it runs in circles.
If it does, it will throw a `CircularDependencyException`.

A typical scenario is that DI is used to resolve services which only have a single instance throughout the whole application.
To tell KittyDI that it should only give birth a single instance, you can use the attribute `Singleton` to decorate a type with.
Once KittyDI encounters this attribute, it will yield the same instance on each resolution of the type.

Sometimes you don't need a single instance of a type, but you need to pass a factory function into a method.
Since KittyDI stores factory functions internally, there's no need to wrap `Resolve<T>` into a lambda. Instead you can directly `Resolve<Func<T>>` which returns the stored factory function.

If a class that is resolved using KittyDI needs to create multiple instances of a type (like a master ViewModel creating detail ViewModels) you can even add `Func<T>` to its constructor parameters.

And resolving IDependencyContainer just leads us to the question on how you can make KittyDI resolve a type by its interface.
Since an interface (or abstract class even) can't be instantiated, by default KittyDI will throw a `NoSuitableConstructorFoundException`.
But of course there is a way to tell KittyDI what to do if it encounters an interface:

## Manually setting up KittyDI

If the automatic resolution is not enough for you, which it most certainly won't be, you can set up KittyDI manually.
You can even just do the manual set up where the automatic fails and let KittyDI do the rest.
In the case you mentioned, you simply need to tell KittyDI which implementation of the interface or abstract class to use:
```C#
container.RegisterImplementation<IDependency, DependencyImplementation>();
```
From now on, if KittyDI needs to resolve `IDependency` it will instead resolve `DependencyImplementation` and return that.
You may notice that `RegisterImplementation` has an optional boolean parameter.
This can be used to tell KittyDI that the type is to be treated as a singleton even though it is not marked as such. 
Doing so can be helpful to register types from other libraries (where you can't use the `Singleton`-Attribute).
The attribute however has higher priority than the boolean parameter.

As you may notice there are more methods starting with `Register` and all of them are used to manually set up KittyDI.

If you have your own way of creating an instance of a type, you can use `RegisterFactory` to register a function that returns a specific type.
`RegisterFactory` also has the optional boolean parameter to mark the returned instance as a singleton.
When you use `RegisterFactory` you need to manually resolve the dependencies, but you know that you can access KittyDI even from within a lambda, right?
`RegisterFactory` has two overloaded versions. One registers the type that the function returns, the other is a combination of `RegisterImplementation` and `RegisterFactory`: It registers a factory function for a type but also tells the kitten to use that function when an interface is being resolved.

If you're working with a library that somehow automatically provides you with an instance to use as a singleton, you can provide it directly using `RegisterInstance<T>`. 
KittyDI will store the instance like has just been resolved as a singleton and return it each time it is resolved.
Similar to `RegisterFactory` there's a version of `RegisterInstance` that registers the instance as implementation of an interface, so it can be resolved using the interface.

The function you might need to use the least (or not at all for now) is `RegisterType<T>`. 
It is used to tell KittyDI that the type `T` exists and can be useful for controlling when KittyDI decides how to resolve a type:
Each call to a `Register*`-method causes KittyDI to determine _how_ the registered type is resolved.
Using `RegisterType` thus can be used to do all the reflection-type-scanning up front during initialization of the application.

