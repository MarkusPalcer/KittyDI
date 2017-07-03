using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KittyDI;
using KittyDI.Attribute;
using KittyDI.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestClasses;

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
      sut.Resolve<IEnumerable<ITestInterface>>().Should().BeEquivalentTo(mock);

      calls.Should().Be(3);
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
      sut.Resolve<IEnumerable<ITestInterface>>().Should().BeEquivalentTo(mock);
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
      sut.Resolve<ITestInterface>().Should().BeOfType<TestImplementation>();
      sut.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<TestImplementation>();
    }

    [TestMethod]
    public void TypeRedirection2()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation(typeof(ITestInterface), typeof(TestImplementation));
      sut.Resolve<ITestInterface>().Should().BeOfType<TestImplementation>();
      sut.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<TestImplementation>();
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
      sut.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<NestedResolutionType<TypeWithSingleConstructor>>();
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
    public void MultipleImplementationsBeingRegisteredThrowsException()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestImplementation>();
      sut.RegisterImplementation<ITestInterface, TestDisposable>();

      sut.Invoking(x => x.Resolve<ITestInterface>())
        .ShouldThrow<MultipleTypesRegisteredException>()
        .Which.RequestedType.Should()
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
      sut.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
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
      child.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
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
      child.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
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
      child.Resolve<IEnumerable<ITestInterface>>().Single().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
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

    [TestMethod]
    public void RegisteringSingletonFactoriesManually()
    {
      var sut = new DependencyContainer();
      sut.RegisterFactory(() => new TestDisposable(), true);

      var instance1 = sut.Resolve<TestDisposable>();
      var instance2 = sut.Resolve<TestDisposable>();

      instance1.Should().BeSameAs(instance2);

      sut.Dispose();
      instance1.IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void RegisteringSingletonFactoriesForInterfaceManually()
    {
      var sut = new DependencyContainer();
      sut.RegisterFactory<ITestInterface>(() => new TestDisposable(), true);

      var instance1 = sut.Resolve<ITestInterface>();
      var instance2 = sut.Resolve<IEnumerable<ITestInterface>>().Single();

      instance1.Should().BeSameAs(instance2);

      sut.Dispose();
      ((TestDisposable) instance1).IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void RegisteringSingletonImplementationsManually()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestDisposable>(true);

      var instance1 = sut.Resolve<ITestInterface>();
      var instance2 = sut.Resolve<IEnumerable<ITestInterface>>().Single();

      instance1.Should().BeSameAs(instance2);

      sut.Dispose();
      ((TestDisposable) instance1).IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void ResolvingTypeMarkedAsSingleton()
    {
      var sut = new DependencyContainer();
      var instance1 = sut.Resolve<TestSingleton>();
      var instance2 = sut.Resolve<TestSingleton>();

      instance1.Should().BeSameAs(instance2);

      sut.Dispose();
      instance1.IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void RegisteringImplementationMarkedAsSingleton()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestSingleton>();

      var instance1 = sut.Resolve<ITestInterface>();
      var instance2 = sut.Resolve<IEnumerable<ITestInterface>>().Single();

      instance1.Should().BeSameAs(instance2);

      sut.Dispose();
      ((TestDisposable)instance1).IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void SingletonRegistrationWithImmediateInstantiation()
    {
      ImmediatelyInstantiatedSingleton.InstanceCounter = 0;

      var sut = new DependencyContainer();
      sut.RegisterType<ImmediatelyInstantiatedSingleton>();

      ImmediatelyInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.InitializeServices();

      ImmediatelyInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.Resolve<ImmediatelyInstantiatedSingleton>();

      ImmediatelyInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.Resolve<ImmediatelyInstantiatedSingleton>();

      ImmediatelyInstantiatedSingleton.InstanceCounter.Should().Be(1);
    }

    [TestMethod]
    public void SingletonRegistrationWithLazyInstantiation()
    {
      LazilyInstantiatedSingleton.InstanceCounter = 0;

      var sut = new DependencyContainer();
      sut.RegisterType<LazilyInstantiatedSingleton>();

      LazilyInstantiatedSingleton.InstanceCounter.Should().Be(0);

      sut.InitializeServices();

      LazilyInstantiatedSingleton.InstanceCounter.Should().Be(0);

      sut.Resolve<LazilyInstantiatedSingleton>();

      LazilyInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.Resolve<LazilyInstantiatedSingleton>();

      LazilyInstantiatedSingleton.InstanceCounter.Should().Be(1);
    }

    [TestMethod]
    public void SingletonRegistrationWithExplicitInstantiation()
    {
      ExplicitInstantiatedSingleton.InstanceCounter = 0;

      var sut = new DependencyContainer();
      sut.RegisterType<ExplicitInstantiatedSingleton>();

      ExplicitInstantiatedSingleton.InstanceCounter.Should().Be(0);

      sut.InitializeServices();

      ExplicitInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.Resolve<ExplicitInstantiatedSingleton>();

      ExplicitInstantiatedSingleton.InstanceCounter.Should().Be(1);

      sut.Resolve<ExplicitInstantiatedSingleton>();

      ExplicitInstantiatedSingleton.InstanceCounter.Should().Be(1);
    }

    [TestMethod]
    public void MultipleImplementationsCanBeResolved()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestImplementation>();
      sut.RegisterImplementation<ITestInterface, TestDisposable>();

      var result = sut.Resolve<IEnumerable<ITestInterface>>();

      result.Select(x => x.GetType()).Should().BeEquivalentTo(typeof(TestImplementation), typeof(TestDisposable));
    }

    [Singleton(Create = SingletonAttribute.CreationRule.CreateWhenRegistered)]
    public class ImmediatelyInstantiatedSingleton
    {
      public static int InstanceCounter = 0;

      public ImmediatelyInstantiatedSingleton()
      {
        InstanceCounter++;
      }
    }

    [Singleton(Create = SingletonAttribute.CreationRule.CreateWhenFirstResolved)]
    public class LazilyInstantiatedSingleton
    {
      public static int InstanceCounter = 0;

      public LazilyInstantiatedSingleton()
      {
        InstanceCounter++;
      }
    }

    [Singleton(Create = SingletonAttribute.CreationRule.CreateDurinServiceInitialization)]
    public class ExplicitInstantiatedSingleton
    {
      public static int InstanceCounter = 0;

      public ExplicitInstantiatedSingleton()
      {
        InstanceCounter++;
      }
    }

    /* TODO:
     * DEPENDENCY CONTAINER:
     * - Resolve Func<T>-Constructor-Parameters
     * - Resolve Lazy<T>-Constructor-Parameters (using Func<T>-Resolution)
     * - Test combination: Type resolves Non-Singleton and Container, adds instance to container and resolves new object - instance is now treated as singleton within the nested scope but not within the outer scope
     * - Be able to register more than one factory per type (complain when more than one is registered)
     * - Register one factory as "default" (uses this when multiple are registered, complains as soon as two defaults are registered)
     * - Resolve IEnumerable<T>, IEnumerable<Func<T>> and IEnumerable<Lazy<T>>
     * - Strict mode that throws instead of creating factories on the fly
     * - Locked mode that throws when an attempt to alter registration is made
     */
  }
}