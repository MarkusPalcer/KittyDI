using FluentAssertions;
using KittyDI;
using KittyDI.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestKitten.TestClasses;

namespace TestKitten
{
  [TestClass]
  public class DependencyContainerTests
  {
    [TestMethod]
    public void ManuallyProvidingFactory()
    {
      var calls = 0;
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterFactory(() =>
      {
        calls++;
        return mock;
      });

      sut.ResolveFactory<TestImplementation>()().Should().Be(mock);
      sut.Resolve<TestImplementation>().Should().Be(mock);
      calls.Should().Be(2);
    }

    [TestMethod]
    public void ManuallyProvidingFactoryForInterface()
    {
      var calls = 0;
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterFactory<ITestInterface, TestImplementation>(() =>
      {
        calls++;
        return mock;
      });

      sut.ResolveFactory<ITestInterface>()().Should().Be(mock);
      sut.Resolve<ITestInterface>().Should().Be(mock);
      calls.Should().Be(2);
    }

    [TestMethod]
    public void ManuallyProvidingInstance()
    {
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterInstance(mock);

      sut.ResolveFactory<TestImplementation>()().Should().Be(mock);
      sut.Resolve<TestImplementation>().Should().Be(mock);
    }

    [TestMethod]
    public void ManuallyProvidingInstanceForInterface()
    {
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterInstance<ITestInterface, TestImplementation>(mock);

      sut.ResolveFactory<ITestInterface>()().Should().Be(mock);
      sut.Resolve<ITestInterface>().Should().Be(mock);
    }

    [TestMethod]
    public void AutoFactoryCreationForParameterlessConstructors()
    {
      var sut = new DependencyContainer();
      sut.Resolve<TestImplementation>().Should().BeOfType<TestImplementation>();
    }

    [TestMethod]
    public void TypeRedirection()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestImplementation>();
      sut.Resolve<TestImplementation>().Should().BeOfType<TestImplementation>();
    }

    [TestMethod]
    public void NoSuitableConstructorPresent()
    {
      var sut = new DependencyContainer(); 
      sut.Invoking(x => x.Resolve<TypeWithUnsuitableConstructor>())
        .ShouldThrow<NoSuitableConstructorFoundException>();
    }

    [TestMethod]
    public void AutoResolvingOfSingleConstructorParameters()
    {
      var sut = new DependencyContainer();
      sut.RegisterInstance(2);
      sut.Invoking(x => x.Resolve<TypeWithSingleConstructor>())
        .ShouldNotThrow(); 
    }

    [TestMethod]
    public void NestedRedirectedResolution()
    {
      var sut = new DependencyContainer();
      sut.RegisterInstance(2);
      sut.RegisterImplementation<ITestInterface, NestedResolutionType<TypeWithSingleConstructor>>();
      sut.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<TypeWithSingleConstructor>>();
    }

    [TestMethod]
    public void MultipleConstructorsNeedAttribute()
    {
      var sut = new DependencyContainer(); 
      sut.RegisterInstance(2);
      sut.Invoking(x => x.Resolve<MarkedConstructorType>())
        .ShouldNotThrow();
    }

    [TestMethod]
    public void CircularDependenciesAreDetected()
    {
      var sut = new DependencyContainer();
      sut.Invoking(x => x.Resolve<CircularDependencyA>()).ShouldThrow<CircularDependencyException>();
    }

    [TestMethod]
    public void UnknownInterfaceImplementationThrowsException()
    {
      var sut = new DependencyContainer();
      sut.Invoking(x => x.Resolve<ITestInterface>())
        .ShouldThrow<NoInterfaceImplementationGivenException>()
        .Which.InterfaceType.Should()
        .Be(typeof(ITestInterface));
    }

    [TestMethod]
    public void AddingContainersMakesTheirContentAccessible()
    {
      var sut = new DependencyContainer();
      var added = new DependencyContainer(); 
      sut.RegisterImplementation<ITestInterface, NestedResolutionType<ITestInterface2>>();
      added.RegisterInstance(new Mock<ITestInterface2>().Object);

      sut.AddContainer(added);
      sut.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
      added.Invoking(x => x.Resolve<ITestInterface>()).ShouldThrow<NoInterfaceImplementationGivenException>();
    }

    [TestMethod]
    public void ChildContainersShouldBeAbleToAccessTheirParentsContents()
    {
      var sut = new DependencyContainer();
      var child = sut.CreateChild();

      child.RegisterImplementation<ITestInterface, NestedResolutionType<ITestInterface2>>();
      sut.RegisterInstance(new Mock<ITestInterface2>().Object);

      child.AddContainer(sut);
      child.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
      sut.Invoking(x => x.Resolve<ITestInterface>()).ShouldThrow<NoInterfaceImplementationGivenException>();
    }

    [TestMethod]
    public void ResolvingAContainerCreatesAChild()
    {
      var sut = new DependencyContainer();
      var child = sut.Resolve<DependencyContainer>();

      child.RegisterImplementation<ITestInterface, NestedResolutionType<ITestInterface2>>();
      sut.RegisterInstance(new Mock<ITestInterface2>().Object);

      child.AddContainer(sut);
      child.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
      sut.Invoking(x => x.Resolve<ITestInterface>()).ShouldThrow<NoInterfaceImplementationGivenException>();
    }

    [TestMethod]
    public void ResolvingAContainerViaInterfaceCreatesAChild()
    {
      var sut = new DependencyContainer();
      var child = sut.Resolve<IDependencyContainer>();

      child.RegisterImplementation<ITestInterface, NestedResolutionType<ITestInterface2>>();
      sut.RegisterInstance(new Mock<ITestInterface2>().Object);

      child.AddContainer(sut);
      child.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
      sut.Invoking(x => x.Resolve<ITestInterface>()).ShouldThrow<NoInterfaceImplementationGivenException>();
    }

    [TestMethod]
    public void DisposingContainersDisposesSingletonsAddedToThem()
    {
      var sut = new DependencyContainer();

      var testDisposable = new TestDisposable();
      var testInterface = new TestDisposable();

      sut.RegisterInstance(testDisposable);
      sut.RegisterInstance<ITestInterface>(testInterface);

      sut.Dispose();

      testDisposable.IsDisposed.Should().BeTrue();
      testInterface.IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void DisposingContainerDisposesChildren()
    {
      var sut = new DependencyContainer();
      var child = sut.CreateChild();

      var testDisposable = new TestDisposable();
      var testInterface = new TestDisposable();

      sut.RegisterInstance(testDisposable);
      child.RegisterInstance<ITestInterface>(testInterface);

      sut.Dispose();

      testDisposable.IsDisposed.Should().BeTrue();
      testInterface.IsDisposed.Should().BeTrue();
    }

    /* TODO:
     * DEPENDENCY CONTAINER:
     * - Manually Register factories for singletons
     * - Register types as singletons via attribute
     * - Automatically dispose created singletons
     * - Resolve Func<T>-Constructor-Parameters
     * - Resolve Lazy<T>-Constructor-Parameters (using Func<T>-Resolution)
     * - Test combination: Type resolves Non-Singleton and Container, adds instance to container and resolves new object - instance is now treated as singleton within the nested scope but not within the outer scope
     * - Be able to register more than one factory per type (complain when more than one is registered)
     * - Register one factory as "default" (uses this when multiple are registered, complains as soon as two defaults are registered)
     * - Resolve IEnumerable<T>, IEnumerable<Func<T>> and IEnumerable<Lazy<T>>
     * 
     * REGISTRAR:
     * - Register all instantiable types in an assembly using reflection
     * - Register those types also as their base classes and interfaces if those have the Contract-Attribute
     */ 
  }
}
