using Autodesk.Revit.UI;
using System;
using System.ComponentModel;

namespace Arent3d.Revit
{
  /// <summary>
  /// <para>Classes with <see cref="RevitAddinAttribute"/> are built into an *.addin file by make_addin command.</para>
  /// <para>When <see cref="RevitAddinAttribute"/> is attached to <see cref="IExternalCommand"/>, several other attributes are used:</para>
  /// <list type="bullet">
  ///   <item><term><see cref="DisplayNameAttribute"/></term><description><code>Text</code> tag. When no <see cref="DisplayNameAttribute"/> is attached, class name is used as <code>Text</code>.</description></item>
  ///   <item><term><see cref="DescriptionAttribute"/></term><description><code>LongDescription</code> tag.</description></item>
  /// </list>
  /// <para>When <see cref="RevitAddinAttribute"/> is attached to <see cref="IExternalApplication"/>, several other attributes are used:</para>
  /// <list type="bullet">
  ///   <item><term><see cref="DisplayNameAttribute"/></term><description><code>Name</code> tag. When no <see cref="DisplayNameAttribute"/> is attached, class name is used as <code>Name</code>.</description></item>
  /// </list>
  /// </summary>
  [AttributeUsage( AttributeTargets.Class )]
  public class RevitAddinAttribute : Attribute
  {
    public Guid Guid { get; }

    /// <summary>
    /// Classes with <see cref="RevitAddinAttribute"/> are built into an *.addin file by make_addin command.
    /// </summary>
    /// <param name="guid"><see cref="System.Guid"/> of an addin. It must not be same as other addin's GUID.</param>
    public RevitAddinAttribute( string guid )
    {
      Guid = Guid.Parse( guid );
    }
  }
}
