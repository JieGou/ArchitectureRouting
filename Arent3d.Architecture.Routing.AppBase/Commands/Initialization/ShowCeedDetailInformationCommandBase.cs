using System.Linq ;
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
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var tagPickFilter = new TagPickFilter() ;

      Reference? reference = null;
      try {
        reference = uiDoc.Selection.PickObject( ObjectType.Element, tagPickFilter ) ;
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
      
      if ( null == reference || uiDoc.Document.GetElement( reference ) is not IndependentTag independentTag )
        return Result.Cancelled ;

      var connector = independentTag.GetTaggedLocalElements().FirstOrDefault( x => BuiltInCategorySets.OtherElectricalElements.Any( y => (int) y == x.Category.Id.IntegerValue ) ) ;
      if ( null == connector )
        return Result.Cancelled ;
        
      connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) ;
      if ( string.IsNullOrEmpty( ceedCode ) ) 
        return Result.Cancelled ;
        
      var ceedCodeModel = ceedCode!.Split( ':' ).ToList() ;
      var ceedSetCode = ceedCodeModel.FirstOrDefault() ?? string.Empty ;
      var deviceSymbol = ceedCodeModel.Count > 1 ? ceedCodeModel.ElementAt( 1 ) : string.Empty ;
      var modelNumber = ceedCodeModel.Count > 2 ? ceedCodeModel.ElementAt( 2 ) : string.Empty ;
      
      var dataContext = new CeedDetailInformationViewModel( uiDoc.Document, ceedSetCode, deviceSymbol, modelNumber ) ;
      var ceedDetailInformationView = new CeedDetailInformationView { DataContext = dataContext};
      ceedDetailInformationView.ShowDialog() ;
      
      return dataContext.DialogResult ? Result.Succeeded : Result.Cancelled ;
    }

    private class TagPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element element )
      {
        if ( element is not IndependentTag independentTag )
          return false ;

        var elementType = element.Document.GetElement( independentTag.GetTypeId() ) ;
        if ( elementType is not FamilySymbol familySymbol )
          return false ;

        return familySymbol.FamilyName == ElectricalRoutingFamilyType.SymbolContentTag.GetFamilyName() ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}