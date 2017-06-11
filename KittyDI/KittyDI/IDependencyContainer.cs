namespace KittyDI
{
  public interface IDependencyContainer
  {
    T Resolve<T>();
  }
}