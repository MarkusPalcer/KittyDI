using System;
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
    public void AddAssemblyOf<T>()
    {
      Add(typeof(T).Assembly);
    }

    public void RegisterToContainer(IDependencyContainer container)
    {
      RegisterTypes(container);

      RegisterImplementations(container);
    }

    private void RegisterImplementations(IDependencyContainer container)
    {
      var typesWithInterfaces = this
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => !type.IsGenericTypeDefinition)
        .Where(type => !type.IsInterface)
        .Where(type => !type.IsAbstract)
        .SelectMany(type => type.GetInterfaces().Select(@interface => Tuple.Create(@interface, type)));

      switch (InterfaceHandling)
      {
        case InterfaceHandlingTypes.RegisterAllImplementedInterfaces:
          // Leave typesWithInterfaces as it is
          break;
        case InterfaceHandlingTypes.RegisterContractsOnly:
          typesWithInterfaces =
            typesWithInterfaces.Where(tuple => tuple.Item1.GetCustomAttribute<ContractAttribute>() != null);
          break;
        case InterfaceHandlingTypes.NoInterfaceRegistration:
          typesWithInterfaces = Enumerable.Empty<Tuple<Type, Type>>();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      foreach (var tuple in typesWithInterfaces)
      {
        container.RegisterImplementation(tuple.Item1, tuple.Item2);
      }
    }

    private void RegisterTypes(IDependencyContainer container)
    {
      IEnumerable<Type> types;
      switch (TypeHandling)
      {
        case TypeHandlingTypes.RegisterAllTypes:
          types = this.SelectMany(x => x.GetTypes());
          break;
        case TypeHandlingTypes.RegisterContractsOnly:
          types = this.SelectMany(x => x.GetTypes())
                      .Where(x => x.GetCustomAttribute<ContractAttribute>(false) != null);
          break;
        case TypeHandlingTypes.NoTypeRegistration:
          types = Enumerable.Empty<Type>();
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
        container.RegisterType(type);
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

    public IDependencyContainer CreateContainer()
    {
      var result = new DependencyContainer();
      RegisterToContainer(result);

      return result;
    }
  }
}