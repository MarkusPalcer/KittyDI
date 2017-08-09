using KittyDI.Attribute;

namespace TestKitten
{
  [Singleton(Create = SingletonAttribute.CreationRule.CreateWhenFirstResolved)]
  public class LazilyInstantiatedSingleton
  {
    public static int InstanceCounter = 0;

    public LazilyInstantiatedSingleton()
    {
      InstanceCounter++;
    }
  }
}