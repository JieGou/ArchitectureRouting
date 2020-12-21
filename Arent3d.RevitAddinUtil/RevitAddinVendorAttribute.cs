using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Revit
{
  /// <summary>
  /// Assemblies with RevitAddinVendorAttribute are built into an *.addin file by make_addin command.
  /// </summary>
  [AttributeUsage( AttributeTargets.Assembly )]
  public class RevitAddinVendorAttribute : Attribute
  {
    /// <summary>
    /// <para>Revit addin Vendor ID for the class. it is an &lt;VendorId&gt; value of each &lt;Addin&gt; elements in *.addin file.</para>
    /// <para>Beforehand, register Vendor ID by Registered Developer Symbol (RDS).</para>
    /// </summary>
    public string VendorId { get; }

    /// <summary>
    /// Assemblies with RevitAddinVendorAttribute are built into an *.addin file by make_addin command.
    /// </summary>
    /// <param name="vendorId">
    ///   <para>Revit addin Vendor ID for the class. it is an &lt;VendorId&gt; value of each &lt;Addin&gt; elements in *.addin file.</para>
    ///   <para>Beforehand, register Vendor ID by Registered Developer Symbol (RDS).</para>
    /// </param>
    public RevitAddinVendorAttribute( string vendorId )
    {
      VendorId = vendorId;
    }
  }
}
