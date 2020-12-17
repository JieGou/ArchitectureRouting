using System;

namespace Arent3d.Revit
{
  /// <summary>
  /// Revit addin type.
  /// </summary>
  public enum AddinType
  {
    /// <summary>
    /// Command type addin.
    /// </summary>
    Command,

    /// <summary>
    /// Application type addin.
    /// </summary>
    Application,
  }

  /// <summary>
  /// Classes with RevitAddinAttribute are built into an *.addin file by make_addin command.
  /// </summary>
  [AttributeUsage( AttributeTargets.Class )]
  public class RevitAddinAttribute : Attribute
  {
    /// <summary>
    /// Revit addin type for the class. it is `Type' attribute of &lt;Addin&gt; element in *.addin file.
    /// </summary>
    public AddinType Type { get; }

    /// <summary>
    /// Revit addin type for the class. it is an &lt;Text&gt; value of &lt;Addin&gt; element in *.addin file.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Classes with RevitAddinAttribute are built into an *.addin file by make_addin command.
    /// </summary>
    /// <param name="type">Revit addin type for the class. it is `Type' attribute of &lt;Addin&gt; element in *.addin file.</param>
    /// <param name="title">Revit addin type for the class. it is an &lt;Text&gt; value of &lt;Addin&gt; element in *.addin file.</param>
    public RevitAddinAttribute( AddinType type, string title )
    {
      Type = type;
      Title = title;
    }
  }
}
