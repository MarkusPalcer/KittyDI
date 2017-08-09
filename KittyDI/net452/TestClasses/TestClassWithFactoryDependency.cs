using System;

namespace TestClasses
{
  public class TestClassWithFactoryDependency
  {
    public Func<TestImplementation> TestFactory { get; }

    public TestClassWithFactoryDependency(Func<TestImplementation> testFactory)
    {
      TestFactory = testFactory;
    }
  }
}