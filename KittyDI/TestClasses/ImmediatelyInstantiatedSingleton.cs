using KittyDI.Attribute;

namespace TestKitten
{
  [Singleton(Create = SingletonAttribute.CreationRule.CreateWhenRegistered)]
  public class ImmediatelyInstantiatedSingleton
  {
    public static int InstanceCounter = 0;

    public ImmediatelyInstantiatedSingleton()
    {
      InstanceCounter++;
    }
  }
}