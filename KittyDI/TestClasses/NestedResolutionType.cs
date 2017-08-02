namespace TestClasses
{
  public class NestedResolutionType<T> : ITestInterface
  {
    public T Value { get; }

    public NestedResolutionType(T t)
    {
      Value = t;
    }
  }
}