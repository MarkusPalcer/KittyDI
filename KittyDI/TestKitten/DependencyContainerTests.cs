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
      sut.RegisterImplementation<ITestInterface, NestedResolutionType>();
      sut.Resolve<ITestInterface>().Should().BeOfType<NestedResolutionType>();
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
  }
}
