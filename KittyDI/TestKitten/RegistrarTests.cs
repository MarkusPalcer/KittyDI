using System;
using KittyDI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestClasses;

namespace TestKitten
{
  [TestClass]
  public class RegistrarTests
  {
    [TestMethod]
    public void RegisterNothingAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void RegisterContractInterfacesAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.RegisterContractsOnly,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };


      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestDisposable), false));
      /*
       * 1) This type implements the interface indirectly and is still added
       * 2) This type has the singleton attribute. Still the registrar will call RegisterImplementation with isSingleton=false
       *    This is due to the optional parameter being there for a programmer registering a type _without_ singleton attribute as a singleton.
       *    It is not possible to register a type _with_ singleton attribute as non-singleton.
       *    Thus the Registrar will ignore this attribute completely.
       */    
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestSingleton), false));

      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(NestedInterfaceImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(ImplementationOfAbstractTestImplementation), false));

      

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void RegisterAllInterfacesAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.RegisterAllImplementedInterfaces,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };


      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestDisposable), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(IDisposable), typeof(TestDisposable), false));
      /*
       * 1) This type implements the interface indirectly and is still added
       * 2) This type has the singleton attribute. Still the registrar will call RegisterImplementation with isSingleton=false
       *    This is due to the optional parameter being there for a programmer registering a type _without_ singleton attribute as a singleton.
       *    It is not possible to register a type _with_ singleton attribute as non-singleton.
       *    Thus the Registrar will ignore this attribute completely.
       */
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestSingleton), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(IDisposable), typeof(TestSingleton), false));

      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface2), typeof(ImplementationOfTestInterface2), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(NestedInterfaceImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(INestedContract), typeof(NestedInterfaceImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(ImplementationOfAbstractTestImplementation), false));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void RegisterContractTypesAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.RegisterContractsOnly,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };

      containerMock.Setup(x => x.RegisterType(typeof(ContractType)));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void RegisterAllTypesAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.RegisterAllTypes,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };

      containerMock.Setup(x => x.RegisterType(typeof(CircularDependencyA)));
      containerMock.Setup(x => x.RegisterType(typeof(CircularDependencyB)));
      containerMock.Setup(x => x.RegisterType(typeof(ContractType)));
      containerMock.Setup(x => x.RegisterType(typeof(ImplementationOfAbstractContract)));
      containerMock.Setup(x => x.RegisterType(typeof(ImplementationOfAbstractTestImplementation)));
      containerMock.Setup(x => x.RegisterType(typeof(ImplementationOfTestInterface2)));
      containerMock.Setup(x => x.RegisterType(typeof(MarkedConstructorType)));
      containerMock.Setup(x => x.RegisterType(typeof(NestedInterfaceImplementation)));
      containerMock.Setup(x => x.RegisterType(typeof(SubclassOfContractType)));
      containerMock.Setup(x => x.RegisterType(typeof(TestDisposable)));
      containerMock.Setup(x => x.RegisterType(typeof(TestImplementation)));
      containerMock.Setup(x => x.RegisterType(typeof(TestSingleton)));
      containerMock.Setup(x => x.RegisterType(typeof(TypeWithSingleConstructor)));
      containerMock.Setup(x => x.RegisterType(typeof(TypeWithUnsuitableConstructor)));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void RegisterAbstractContractImplementationsAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.RegisterContractsOnly
      };

      containerMock.Setup(x => x.RegisterImplementation(typeof(AbstractContract), typeof(ImplementationOfAbstractContract), false));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);

      containerMock.VerifyAll();
    }

    [TestMethod]
    public void RegisterAllAbstractImplementationsAutomatically()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.RegisterAllImplementations
      };

      containerMock.Setup(x => x.RegisterImplementation(typeof(AbstractContract), typeof(ImplementationOfAbstractContract), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(AbstractTestImplementation), typeof(ImplementationOfAbstractTestImplementation), false));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void AddCustomContractInterface()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.RegisterContractsOnly,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.NoRegistrationOfAbstractImplementations
      };


      // TODO: Duplicate code
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestDisposable), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(TestSingleton), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(NestedInterfaceImplementation), false));
      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface), typeof(ImplementationOfAbstractTestImplementation), false));

      sut.AddContract<ITestInterface2>();

      containerMock.Setup(x => x.RegisterImplementation(typeof(ITestInterface2), typeof(ImplementationOfTestInterface2), false));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);
    }

    [TestMethod]
    public void AddCustomAbstractContract()
    {
      var containerMock = new Mock<IDependencyContainer>(MockBehavior.Strict);

      var sut = new Registrar
      {
        TypeHandling = Registrar.TypeHandlingTypes.NoTypeRegistration,
        InterfaceHandling = Registrar.InterfaceHandlingTypes.NoInterfaceRegistration,
        AbstractImplementationHandling = Registrar.AbstractHandlingTypes.RegisterContractsOnly
      };

      containerMock.Setup(x => x.RegisterImplementation(typeof(AbstractContract), typeof(ImplementationOfAbstractContract), false));

      sut.AddContract<AbstractClassWithoutContract>();

      containerMock.Setup(x => x.RegisterImplementation(typeof(AbstractClassWithoutContract), typeof(ImplementationOfAbstractClassWithoutContract), false));

      sut.AddAssemblyOf<ITestInterface>();
      sut.RegisterToContainer(containerMock.Object);

      containerMock.VerifyAll();
    }
  }
}