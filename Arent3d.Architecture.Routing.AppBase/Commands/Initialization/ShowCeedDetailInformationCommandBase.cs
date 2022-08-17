using System ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowCeedDetailInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      
      string ceedSetCode, deviceSymbol, modelNumber ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var textNoteFilter = new TextNotePickFilter() ;
      
      try {
        var element = uiDoc.Selection.PickObject( ObjectType.Element, textNoteFilter ) ;
        var textNote = document.GetAllElements<TextNote>().ToList().FirstOrDefault( x => x.Id == element.ElementId ) ;
        if ( null == textNote )
          return Result.Cancelled ;

        if ( textNote.GroupId == ElementId.InvalidElementId )
          return Result.Cancelled ;
        
        var groupId = document.GetAllElements<Group>().FirstOrDefault( g => g.Id == textNote.GroupId )?.AttachedParentId ;
        if ( null == groupId || groupId == ElementId.InvalidElementId )
          return Result.Cancelled ;
        
        var connector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).FirstOrDefault( e => e.GroupId == groupId || e.GroupId == textNote.GroupId ) ;
        if ( null == connector )
          return Result.Cancelled ;
        
        connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) ;
        if ( string.IsNullOrEmpty( ceedCode ) ) 
          return Result.Cancelled ;
        
        var ceedCodeModel = ceedCode!.Split( ':' ).ToList() ;
        ceedSetCode = ceedCodeModel.FirstOrDefault() ?? string.Empty ;
        deviceSymbol = ceedCodeModel.Count > 1 ? ceedCodeModel.ElementAt( 1 ) : string.Empty ;
        modelNumber = ceedCodeModel.Count > 2 ? ceedCodeModel.ElementAt( 2 ) : string.Empty ;
      }
      catch {
        return Result.Cancelled ;
      }

      if ( string.IsNullOrEmpty( ceedSetCode ) ) 
        return Result.Cancelled ;

      var dataContext = new CeedDetailInformationViewModel( document, ceedSetCode, deviceSymbol, modelNumber ) ;
      var ceedDetailInformationView = new CeedDetailInformationView { DataContext = dataContext};
      ceedDetailInformationView.ShowDialog() ;
      
      return dataContext.DialogResult ? Result.Succeeded : Result.Cancelled ;
    }

    private class TextNotePickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e.GetBuiltInCategory() == BuiltInCategory.OST_TextNotes ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}