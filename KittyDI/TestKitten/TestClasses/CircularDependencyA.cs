namespace TestKitten.TestClasses
{
  public class CircularDependencyA
  {
    public CircularDependencyA(CircularDependencyB c) { }
  }
}