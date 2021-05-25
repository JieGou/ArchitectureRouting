using System ;
using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  [Flags]
  public enum DocumentUpdateListenType
  {
    Any = ~0,
    Parameter = 0x1,
    Geometry = 0x2,
    Addition = 0x10,
    Deletion = 0x20,
  }

  /// <summary>
  /// A listener class of document changing (Wrapper class of <see cref="Autodesk.Revit.DB.IUpdater"/>).
  /// </summary>
  public interface IDocumentUpdateListener
  {
    /// <summary>
    /// Returns the name of the <see cref="Autodesk.Revit.DB.IUpdater"/>.
    /// </summary>
    string Name { get ; }

    /// <summary>
    /// Returns the description text of the <see cref="Autodesk.Revit.DB.IUpdater"/>.
    /// </summary>
    string Description { get ; }

    /// <summary>
    /// Returns the change priority of the <see cref="Autodesk.Revit.DB.IUpdater"/>.
    /// </summary>
    ChangePriority ChangePriority { get ; }

    /// <summary>
    /// Listening types, combined with bit or.
    /// </summary>
    DocumentUpdateListenType ListenType { get ; }

    /// <summary>
    /// A filter to determine which element is listened.
    /// </summary>
    /// <returns></returns>
    ElementFilter GetElementFilter() ;

    /// <summary>
    /// Enumerates parameters to be listened (Used only <see cref="DocumentUpdateListenType.Parameter"/> is contained in <see cref="ListenType"/> property).
    /// </summary>
    /// <returns>Parameter list (Use <see cref="ParameterProxy.From( Autodesk.Revit.DB.Parameter )"/> or <see cref="ParameterProxy.From( Autodesk.Revit.DB.ElementId )"/>).</returns>
    IEnumerable<ParameterProxy> GetListeningParameters() ;

    /// <summary>
    /// Action on document is changing.
    /// </summary>
    /// <param name="data">Updating information.</param>
    void Execute( UpdaterData data ) ;
  }
}