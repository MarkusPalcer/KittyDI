using System;

namespace KittyDI.Attribute
{
  [AttributeUsage(AttributeTargets.Class)]
  public class SingletonAttribute : System.Attribute
  {
    public enum CreationRule
    {
      /// <summary>
      /// Does not create the singleton instance until it is used for the first time
      /// </summary>
      CreateWhenFirstResolved,

      /// <summary>
      /// Creates the singleton instance as soon as it is known to the dependency container
      /// </summary>
      CreateWhenRegistered,

      /// <summary>
      /// Creates the singleton instance when the methond <see cref="DependencyContainer.InitializeServices"/> is called
      /// </summary>
      CreateDurinServiceInitialization
    }

    public CreationRule Create { get; set; } = CreationRule.CreateWhenFirstResolved;
  }
}