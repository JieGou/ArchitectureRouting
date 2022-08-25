using System ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
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
        
      connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
      if ( string.IsNullOrEmpty( ceedSetCodeModel ) ) 
        return Result.Cancelled ;
        
      var ceedSetCode = ceedSetCodeModel!.Split( ':' ).ToList() ;
      var pickedText = ceedSetCode.FirstOrDefault() ?? string.Empty ;

      if ( string.IsNullOrEmpty( pickedText ) ) 
        return Result.Cancelled ;

      var dataContext = new CeedDetailInformationViewModel( uiDoc.Document, pickedText ) ;
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