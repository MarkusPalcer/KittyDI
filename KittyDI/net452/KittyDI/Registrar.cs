﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittyDI.Attribute;

namespace KittyDI
{
  /// <summary>
  /// Registers the content of an assembly to a dependency container
  /// </summary>
  public class Registrar : List<Assembly>
  {
    private readonly Dictionary<TypeInfo, object> _contracts = new Dictionary<TypeInfo, object>();

    public void AddAssemblyOf<T>()
    {
      Add(typeof(T).GetTypeInfo().Assembly);
    }

    public void RegisterToContainer(IDependencyContainer container)
    {
      RegisterTypes(container);

      RegisterImplementations(container);

      RegisterAbstractImplementations(container);
    }

    private IEnumerable<TypeInfo> GetBaseTypes(TypeInfo type)
    {
      while (type != null)
      {
        type = type.BaseType?.GetTypeInfo();
        if (type != null) yield return type;
      }
    }

    private void RegisterAbstractImplementations(IDependencyContainer container)
    {
      var typesWithAbstractBaseClasses = this
        .SelectMany(assembly => assembly.DefinedTypes)
        .Where(type => !type.IsGenericTypeDefinition)
        .Where(type => !type.IsInterface)
        .Where(type => !type.IsAbstract)
        .SelectMany(type => GetBaseTypes(type).Where(baseType => baseType.IsAbstract).Select(baseType => Tuple.Create(baseType, type)));

      switch (AbstractImplementationHandling)
      {
        case AbstractHandlingTypes.RegisterAllImplementations:
          break;
        case AbstractHandlingTypes.RegisterContractsOnly:
          typesWithAbstractBaseClasses = typesWithAbstractBaseClasses.Where(x => (x.Item1.GetCustomAttribute<ContractAttribute>() != null) || (_contracts.ContainsKey(x.Item1)));
          break;
        case AbstractHandlingTypes.NoRegistrationOfAbstractImplementations:
          typesWithAbstractBaseClasses = Enumerable.Empty<Tuple<TypeInfo, TypeInfo>>();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      foreach (var tuple in typesWithAbstractBaseClasses)
      {
        container.RegisterImplementation(tuple.Item1.AsType(), tuple.Item2.AsType());
      }
    }

    private void RegisterImplementations(IDependencyContainer container)
    {
      var typesWithInterfaces = this
        .SelectMany(assembly => assembly.DefinedTypes)
        .Where(type => !type.IsGenericTypeDefinition)
        .Where(type => !type.IsInterface)
        .Where(type => !type.IsAbstract)
        .SelectMany(type => type.ImplementedInterfaces.Select(@interface => Tuple.Create(@interface.GetTypeInfo(), type)));

      switch (InterfaceHandling)
      {
        case InterfaceHandlingTypes.RegisterAllImplementedInterfaces:
          // Leave typesWithInterfaces as it is
          break;
        case InterfaceHandlingTypes.RegisterContractsOnly:
          typesWithInterfaces =
            typesWithInterfaces.Where(tuple => (tuple.Item1.GetCustomAttribute<ContractAttribute>() != null) || (_contracts.ContainsKey(tuple.Item1)));
          break;
        case InterfaceHandlingTypes.NoInterfaceRegistration:
          typesWithInterfaces = Enumerable.Empty<Tuple<TypeInfo, TypeInfo>>();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      foreach (var tuple in typesWithInterfaces)
      {
        container.RegisterImplementation(tuple.Item1.AsType(), tuple.Item2.AsType());
      }
    }

    private void RegisterTypes(IDependencyContainer container)
    {
      IEnumerable<TypeInfo> types;
      switch (TypeHandling)
      {
        case TypeHandlingTypes.RegisterAllTypes:
          types = this.SelectMany(x => x.DefinedTypes);
          break;
        case TypeHandlingTypes.RegisterContractsOnly:
          types = this.SelectMany(x => x.DefinedTypes)
                      .Where(x => x.GetCustomAttribute<ContractAttribute>(false) != null);
          break;
        case TypeHandlingTypes.NoTypeRegistration:
          types = Enumerable.Empty<TypeInfo>();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      types = types
        .Where(type => !type.IsGenericTypeDefinition)
        .Where(type => !type.IsAbstract)
        .Where(type => !type.IsInterface)
        .ToArray();

      foreach (var type in types)
      {
        container.RegisterType(type.AsType());
      }
    }

    public enum InterfaceHandlingTypes
    {
      /// <summary>
      /// Causes the registrar to <see cref="Registrar"/> a type as the implementation of all interfaces it implements (directly)
      /// </summary>
      RegisterAllImplementedInterfaces,

      /// <summary>
      /// Causes the <see cref="Registrar"/> to register a type only as implementation of interfaces marked with the <see cref="ContractAttribute"/> attribute
      /// </summary>
      RegisterContractsOnly,

      /// <summary>
      /// Does not register types as implementation of interfaces
      /// </summary>
      NoInterfaceRegistration
    }

    /// <summary>
    /// Gets or Sets a value indicating for which combinations to call <see cref="IDependencyContainer.RegisterImplementation{TContract,TImplementation}"/>
    /// </summary>
    public InterfaceHandlingTypes InterfaceHandling { get; set; } = InterfaceHandlingTypes.RegisterContractsOnly;


    public enum TypeHandlingTypes
    {
      /// <summary>
      /// Register all types found in the assemblies
      /// </summary>
      RegisterAllTypes,

      /// <summary>
      /// Register only types marked with the <see cref="ContractAttribute"/> attribute
      /// </summary>
      RegisterContractsOnly,

      /// <summary>
      /// Does not register types directly. Does not affect registration of types as implementation of interfaces. 
      /// </summary>
      NoTypeRegistration
    }

    public TypeHandlingTypes TypeHandling { get; set; } = TypeHandlingTypes.RegisterContractsOnly;

    public enum AbstractHandlingTypes
    {
      /// <summary>
      /// Registers all types found in the assemblies that inherit from an abstract class
      /// </summary>
      RegisterAllImplementations,

      /// <summary>
      /// Registers all types found in the assemblies that inherit from an abstract class which is marked with the <see cref="ContractAttribute"/> attribute
      /// </summary>
      RegisterContractsOnly,

      /// <summary>
      /// Does not register implementations of abstract classes
      /// </summary>
      NoRegistrationOfAbstractImplementations
    }

    public AbstractHandlingTypes AbstractImplementationHandling { get; set; } = AbstractHandlingTypes.RegisterContractsOnly;

    public IDependencyContainer CreateContainer()
    {
      var result = new DependencyContainer();
      RegisterToContainer(result);

      return result;
    }

    public void AddContract<TContract>()
    {
      _contracts.Add(typeof(TContract).GetTypeInfo(), null);
    }
  }
}