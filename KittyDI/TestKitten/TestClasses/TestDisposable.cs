using System;

namespace TestKitten.TestClasses
{
  public class TestDisposable : IDisposable, ITestInterface
  {
    public void Dispose()
    {
      IsDisposed = true;
    }

    public bool IsDisposed { get; set; }
  }
}