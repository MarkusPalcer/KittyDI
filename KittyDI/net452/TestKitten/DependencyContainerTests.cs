using System;
using FluentAssertions;
using KittyDI;
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

      sut.Resolve<TestImplementation>().Should().Be(mock);
      sut.Resolve<Func<TestImplementation>>()().Should().Be(mock);
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

      sut.Resolve<Func<ITestInterface>>()().Should().Be(mock);
      sut.Resolve<ITestInterface>().Should().Be(mock);

      calls.Should().Be(2);
    }

    [TestMethod]
    public void ManuallyProvidingInstance()
    {
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterInstance(mock);

      sut.Resolve<Func<TestImplementation>>()().Should().Be(mock);
      sut.Resolve<TestImplementation>().Should().Be(mock);
    }

    [TestMethod]
    public void ManuallyProvidingInstanceForInterface()
    {
      var mock = new TestImplementation();
      var sut = new DependencyContainer();
      sut.RegisterInstance<ITestInterface, TestImplementation>(mock);

      sut.Resolve<Func<ITestInterface>>()().Should().Be(mock);
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
      sut.Resolve<ITestInterface>().Should().BeOfType<TestImplementation>();
    }

    [TestMethod]
    public void TypeRedirection2()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation(typeof(ITestInterface), typeof(TestImplementation));
      sut.Resolve<ITestInterface>().Should().BeOfType<TestImplementation>();
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
    public void MultipleImplementationsBeingRegisteredThrowsException()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestImplementation>();
      sut.Invoking(x => x.RegisterImplementation<ITestInterface, TestDisposable>()).ShouldThrow<TypeAlreadyRegisteredException>();
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

      sut.RegisterImplementation<ITestInterface2, ImplementationOfTestInterface2>();
      child.RegisterImplementation<ITestInterface, NestedResolutionType<ITestInterface2>>();

      sut.Invoking(x => x.Resolve<ITestInterface>()).ShouldThrow<NoInterfaceImplementationGivenException>();
      child.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType<ITestInterface2>>();
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

      sut.Dispose();
      ((TestDisposable) instance1).IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void RegisteringSingletonImplementationsManually()
    {
      var sut = new DependencyContainer();
      sut.RegisterImplementation<ITestInterface, TestDisposable>(true);

      var instance1 = sut.Resolve<ITestInterface>();

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
      sut.Invoking(x => x.RegisterImplementation<ITestInterface, TestDisposable>()).ShouldThrow<TypeAlreadyRegisteredException>();
    }

    [TestMethod]
    public void FactoriesCanBeResolvedThroughConstructor()
    {
      var sut = new DependencyContainer();
      var result = sut.Resolve<TestClassWithFactoryDependency>();
      result.Should().NotBeNull();
      result.TestFactory().Should().NotBeNull();
    }

    [TestMethod]
    public void GenericLazyCanBeResolved()
    {
      var sut = new DependencyContainer();
      var instance = new Mock<ITestInterface>().Object;
      sut.RegisterInstance(instance);
      var result = sut.Resolve<Lazy<ITestInterface>>();

      result.IsValueCreated.Should().BeFalse();
      result.Value.Should().Be(instance);
      result.IsValueCreated.Should().BeTrue();
    }

    [TestMethod]
    public void InStrictModeOnlyRegisteredTypesCanBeResolved()
    {
      var sut = new DependencyContainer();

      sut.RegisterType<TestImplementation>();
      sut.Mode = DependencyContainerMode.Strict;

      sut.Invoking(x => x.Resolve<TestImplementation>()).ShouldNotThrow();
      sut.Invoking(x => x.Resolve<TestDisposable>()).ShouldThrow<ContainerLockedException>();

      sut.RegisterType<TestDisposable>();
      sut.Invoking(x => x.Resolve<TestDisposable>()).ShouldNotThrow();
    }

    [TestMethod]
    public void InLockedModeRegistrationIsNotPossible()
    {
      var sut = new DependencyContainer();

      sut.RegisterType<TestImplementation>();
      sut.Mode = DependencyContainerMode.Locked;

      sut.Invoking(x => x.Resolve<TestImplementation>()).ShouldNotThrow();
      sut.Invoking(x => x.Resolve<TestDisposable>()).ShouldThrow<ContainerLockedException>();
      sut.Invoking(x => x.RegisterType<TestDisposable>()).ShouldThrow<ContainerLockedException>();
    }

    [TestMethod]
    public void LeavingLockedModeIsForbidden()
    {
      var sut = new DependencyContainer();

      sut.Mode = DependencyContainerMode.Locked;
      sut.Invoking(x => x.Mode = DependencyContainerMode.Regular).ShouldThrow<InvalidOperationException>();
      sut.Invoking(x => x.Mode = DependencyContainerMode.Strict).ShouldThrow<InvalidOperationException>();
    }

    [TestMethod]
    public void LeavingStrictModeIsAllowed()
    {
      var sut = new DependencyContainer();

      sut.Mode = DependencyContainerMode.Strict;
      sut.Invoking(x => x.Mode = DependencyContainerMode.Regular).ShouldNotThrow();
    }

    [TestMethod]
    public void ResolvingWithOneGivenParameter()
    {
      var sut = new DependencyContainer();

      var factory = sut.Resolve<Func<ITestInterface, NestedResolutionType<ITestInterface>>>();
      var mock = new Mock<ITestInterface>();
      var result = factory(mock.Object);

      result.Value.Should().BeSameAs(mock.Object);
    }
  }
}