namespace KittyDI
{
  /// <summary>
  /// Different operating modes of the dependency container
  /// </summary>
  public enum DependencyContainerMode
  {
    /// <summary>
    /// On resolving an unknown type it will be registered automatically
    /// </summary>
    Regular,

    /// <summary>
    /// On resolving an unknown type an exception will be thrown
    /// </summary>
    Strict,

    /// <summary>
    /// On resolving an unknown type an exception will be thrown and no further types can be registered
    /// 
    /// Note that the mode can not be changed if it is set to <code>Locked</code> once
    /// </summary>
    Locked
  }
}