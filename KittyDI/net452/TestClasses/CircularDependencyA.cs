namespace TestClasses
{
  public class CircularDependencyA
  {
    public CircularDependencyA(CircularDependencyB c) { }
  }
}