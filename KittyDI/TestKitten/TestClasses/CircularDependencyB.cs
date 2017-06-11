namespace TestKitten.TestClasses
{
  public class CircularDependencyB
  {
    public CircularDependencyB(CircularDependencyA c) { }
  }
}