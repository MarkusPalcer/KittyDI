using KittyDI.Attribute;

namespace TestKitten
{
  [Singleton(Create = SingletonAttribute.CreationRule.CreateDurinServiceInitialization)]
  public class ExplicitInstantiatedSingleton
  {
    public static int InstanceCounter = 0;

    public ExplicitInstantiatedSingleton()
    {
      InstanceCounter++;
    }
  }
}