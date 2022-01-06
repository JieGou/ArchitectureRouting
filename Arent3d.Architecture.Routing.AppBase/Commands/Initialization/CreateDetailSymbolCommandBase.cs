using System.Collections.Generic ;
using System.Drawing ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Application = Autodesk.Revit.ApplicationServices.Application ;
using Color = Autodesk.Revit.DB.Color ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class CreateDetailSymbolCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var activeView = doc.ActiveView ;
      if ( activeView is View3D ) {
        const string mess = "Please select cable on view 2D." ;
        MessageBox.Show( mess, "Message" ) ;
        return Result.Cancelled ;
      }

      var uiDoc = commandData.Application.ActiveUIDocument ;
      var selection = uiDoc.Selection ;
      UIApplication uiApp = commandData.Application ;
      Application app = uiApp.Application ;
      var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;

      return doc.Transaction( "TransactionName.Commands.Routing.AddSymbol".GetAppStringByKeyOrDefault( "Create Detail Symbol" ), _ =>
      {
        RemoveDetailSymbolUnused( doc, detailSymbolStorable ) ;
        var element = selection.PickObject( ObjectType.Element, ConduitSelectionFilter.Instance, "Select cable." ) ;
        var conduit = doc.GetElement( element.ElementId ) ;
        var conduitHasSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.ConduitId == conduit.Id.IntegerValue.ToString() ) ;

        var (symbols, angle, defaultSymbol) = CreateValueForCombobox( doc, detailSymbolStorable.DetailSymbolModelData, conduit ) ;
        var detailSymbolSettingDialog = new DetailSymbolSettingDialog( symbols, angle, defaultSymbol ) ;
        detailSymbolSettingDialog.ShowDialog() ;
        if ( ! ( detailSymbolSettingDialog.DialogResult ?? false ) ) return Result.Cancelled ;

        var isParentSymbol = CheckDetailSymbolOfConduitDifferentCode( doc, conduit, detailSymbolStorable.DetailSymbolModelData, detailSymbolSettingDialog.DetailSymbol ) ;
        XYZ firstPoint = element.GlobalPoint ;
        var (textNote, lineIds) = CreateDetailSymbol( doc, detailSymbolSettingDialog, firstPoint, detailSymbolSettingDialog.Angle, isParentSymbol ) ;

        if ( conduitHasSymbol != null ) {
          var representativeRouteName = conduit.GetRepresentativeRouteName() ?? string.Empty ;
          var routeName = conduit.GetRouteName() ?? string.Empty ;
          UpdateDetailSymbol( doc, detailSymbolStorable, conduit, conduitHasSymbol, textNote, detailSymbolSettingDialog, lineIds, isParentSymbol, representativeRouteName, routeName ) ;
        }
        else
          SaveDetailSymbol( doc, detailSymbolStorable, conduit, textNote, detailSymbolSettingDialog, lineIds, isParentSymbol ) ;

        return Result.Succeeded ;
      } ) ;
    }

    private ( TextNote, string) CreateDetailSymbol( Document doc, DetailSymbolSettingDialog detailSymbolSettingDialog, XYZ firstPoint, string angle, bool isParentSymbol )
    {
      const double baseLengthOfLine = 0.2 ;
      var isLeft = true ;
      var size = detailSymbolSettingDialog.HeightCharacter ;
      // create color using Color.FromArgb with RGB inputs
      var color = System.Drawing.Color.FromArgb( 255, 0, 0 ) ;
      // convert color into an integer
      var colorInt = System.Drawing.ColorTranslator.ToWin32( color ) ;
      var txtColor = isParentSymbol ? 0 : colorInt ;
      List<string> lineIds = new List<string>() ;
      var startLineP1 = new XYZ( firstPoint.X + baseLengthOfLine, firstPoint.Y + baseLengthOfLine, firstPoint.Z ) ;
      var endLineP1 = new XYZ( firstPoint.X - baseLengthOfLine, firstPoint.Y - baseLengthOfLine, firstPoint.Z ) ;
      Curve startCurve = Line.CreateBound( startLineP1, endLineP1 ) ;
      var startLine = doc.Create.NewDetailCurve( doc.ActiveView, startCurve ) ;
      var subCategory = GetLineStyle( doc ) ;
      startLine.LineStyle = subCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      lineIds.Add( startLine.Id.IntegerValue.ToString() ) ;

      List<XYZ> points = new List<XYZ>() ;
      switch ( angle ) {
        case "0" :
          points.Add( new XYZ( firstPoint.X - baseLengthOfLine * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X + baseLengthOfLine, firstPoint.Y, firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y + baseLengthOfLine * 10, firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X - baseLengthOfLine * ( 3 + size ), firstPoint.Y + baseLengthOfLine * 10, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y - baseLengthOfLine, firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "180" :
          points.Add( new XYZ( firstPoint.X + baseLengthOfLine * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X - baseLengthOfLine, firstPoint.Y, firstPoint.Z ) ;
          isLeft = false ;
          break ;
        case "-90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y - baseLengthOfLine * ( 8 + size ), firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X + baseLengthOfLine * ( 5 + size ), firstPoint.Y - baseLengthOfLine * ( 8 + size ), firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y + baseLengthOfLine, firstPoint.Z ) ;
          isLeft = false ;
          break ;
      }

      foreach ( var nextP in points ) {
        var curve = Line.CreateBound( firstPoint, nextP ) ;
        var detailCurve = doc.Create.NewDetailCurve( doc.ActiveView, curve ) ;
        lineIds.Add( detailCurve.Id.IntegerValue.ToString() ) ;
        firstPoint = nextP ;
      }

      ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
      var noteWidth = ( size / 32.0 ) * ( 1.0 / 12.0 ) * detailSymbolSettingDialog.PercentWidth / 100 ;

      // make sure note width works for the text type
      var minWidth = TextElement.GetMinimumAllowedWidth( doc, defaultTextTypeId ) ;
      var maxWidth = TextElement.GetMaximumAllowedWidth( doc, defaultTextTypeId ) ;
      if ( noteWidth < minWidth ) {
        noteWidth = minWidth ;
      }
      else if ( noteWidth > maxWidth ) {
        noteWidth = maxWidth ;
      }

      TextNoteOptions opts = new( defaultTextTypeId ) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

      var txtPosition = new XYZ( firstPoint.X + ( isLeft ? baseLengthOfLine : -baseLengthOfLine * 4 ), firstPoint.Y + baseLengthOfLine * ( 1 + size * 2 ), firstPoint.Z ) ;
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, detailSymbolSettingDialog.DetailSymbol, opts ) ;
      CreateNewTextNoteType( doc, textNote, size, detailSymbolSettingDialog.SymbolFont, detailSymbolSettingDialog.SymbolStyle, detailSymbolSettingDialog.Offset, detailSymbolSettingDialog.BackGround, detailSymbolSettingDialog.PercentWidth, txtColor ) ;
      return ( textNote, string.Join( ",", lineIds ) ) ;
    }

    private void UpdateDetailSymbol( Document doc, DetailSymbolStorable detailSymbolStorable, Element conduit, DetailSymbolModel detailSymbolModel, TextNote symbol, DetailSymbolSettingDialog detailSymbolSettingDialog, string lineIds, bool isParentSymbol, string representativeRouteName, string routeName )
    {
      try {
        var detailSymbol = detailSymbolSettingDialog.DetailSymbol ;
        var oldDetailSymbolId = detailSymbolModel.DetailSymbol ;
        var oldSymbol = detailSymbolModel.DetailSymbol ;
        var oldIsParentSymbol = detailSymbolModel.IsParentSymbol ;
        // delete old symbol
        var isSamePosition = representativeRouteName != routeName ;
        if ( isSamePosition ) {
          var routeNameSamePosition = GetRouteNameSamePosition( doc, representativeRouteName, conduit ) ;
          var detailSymbolModelsSameId = detailSymbolStorable.DetailSymbolModelData.Where( n => ! routeNameSamePosition.Contains( n.RouteName ) && n.DetailSymbolId == detailSymbolModel.DetailSymbolId ).ToList() ;
          if ( ! detailSymbolModelsSameId.Any() )
            DeleteDetailSymbol( doc, detailSymbolModel.DetailSymbolId, detailSymbolModel.LineIds ) ;
        }
        else {
          var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbolId == detailSymbolModel.DetailSymbolId && d.RouteName != routeName ).ToList() ;
          if ( ! detailSymbolModels.Any() )
            DeleteDetailSymbol( doc, detailSymbolModel.DetailSymbolId, detailSymbolModel.LineIds ) ;
        }

        // update symbol of cables same route
        List<string> conduitIdsSameRoute = new List<string>() ;
        foreach ( var symbolModel in detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbol == oldSymbol && d.RouteName == routeName ) ) {
          symbolModel.DetailSymbolId = symbol.Id.IntegerValue.ToString() ;
          symbolModel.DetailSymbol = detailSymbol ;
          symbolModel.LineIds = lineIds ;
          symbolModel.IsParentSymbol = isParentSymbol ;
          conduitIdsSameRoute.Add( symbolModel.ConduitId ) ;
        }

        // update symbol's text color of cables different code and same symbol
        if ( ! string.IsNullOrEmpty( detailSymbolModel.Code ) && oldSymbol != detailSymbol && oldIsParentSymbol ) {
          List<string> conduitSamePosition = GetAllConduitIdsOfRouteSamePosition( doc, conduit ) ;
          var parentSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.IsParentSymbol && d.DetailSymbol == oldSymbol ) ;
          if ( parentSymbol == null ) UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolStorable.DetailSymbolModelData, oldSymbol, detailSymbolModel.Code, conduitSamePosition ) ;
        }

        // add symbol for cables same position
        if ( isSamePosition ) {
          List<DetailSymbolModel> detailSymbolModels = new List<DetailSymbolModel>() ;
          List<string> conduitIdsHasSymbol = detailSymbolStorable.DetailSymbolModelData.Select( d => d.ConduitId ).ToList() ;
          List<Element> allConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
          List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.Id.IntegerValue.ToString() != detailSymbolModel.ConduitId ).ToList() ;
          UpdateSymbolOfConduitSamePosition( doc, detailSymbolStorable.DetailSymbolModelData, allConduit, conduit, conduitIdsHasSymbol, symbol.Id.IntegerValue.ToString(), detailSymbol, lineIds, isParentSymbol, representativeRouteName, oldDetailSymbolId, conduitIdsSameRoute ) ;
          AddDetailSymbolForConduitsSamePosition( doc, allConduit, allConnector, conduit, detailSymbolModels, detailSymbolSettingDialog, conduitIdsHasSymbol, representativeRouteName, routeName, symbol.Id.IntegerValue.ToString(), lineIds, isParentSymbol ) ;
          detailSymbolStorable.DetailSymbolModelData.AddRange( detailSymbolModels ) ;
        }

        detailSymbolStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
      }
    }

    private List<string> GetRouteNameSamePosition( Document doc, string representativeRouteName, Element pickConduit )
    {
      List<string> routeNames = new List<string>() ;
      if ( pickConduit is not Conduit ) return routeNames ;
      var conduits = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).ToList() ;
      var location = ( pickConduit.Location as LocationCurve ) ! ;
      var line = ( location.Curve as Line ) ! ;
      var origin = line.Origin ;
      var direction = line.Direction ;
      foreach ( var conduit in conduits ) {
        var anotherLocation = ( conduit.Location as LocationCurve ) ! ;
        var anotherLine = ( anotherLocation.Curve as Line ) ! ;
        var anotherOrigin = anotherLine.Origin ;
        var anotherDirection = anotherLine.Direction ;
        if ( anotherOrigin.DistanceTo( origin ) == 0 && anotherDirection.DistanceTo( direction ) == 0 && ! routeNames.Contains( conduit.GetRouteName()! ) )
          routeNames.Add( conduit.GetRouteName()! ) ;
      }

      return routeNames ;
    }

    private void DeleteDetailSymbol( Document doc, string detailSymbolId, string lineIds )
    {
      var symbolId = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.Id.IntegerValue.ToString() == detailSymbolId ).Select( t => t.Id ).FirstOrDefault() ;
      if ( symbolId != null ) doc.Delete( symbolId ) ;
      foreach ( var lineId in lineIds.Split( ',' ) ) {
        var id = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.Id.IntegerValue.ToString() == lineId ).Select( e => e.Id ).FirstOrDefault() ;
        if ( id != null ) doc.Delete( id ) ;
      }
    }

    private void UpdateSymbolOfConduitSamePosition( Document doc, List<DetailSymbolModel> detailSymbolModels, List<Element> allConduit, Element conduit, List<string> conduitIdsHasSymbol, string detailSymbolId, string detailSymbol, string lineIds, bool isParentSymbol, string conduitRepresentativeRouteName, string oldDetailSymbolId, List<string> conduitIdsSameRoute )
    {
      List<string> conduitSamePositionIds = new List<string>() ;
      var routeNames = GetRouteNameSamePosition( doc, conduitRepresentativeRouteName, conduit ) ;
      foreach ( var routeName in routeNames ) {
        var conduitIds = allConduit.Where( c => c.GetRouteName() == routeName && conduitIdsHasSymbol.Contains( c.Id.IntegerValue.ToString() ) ).Select( c => c.Id.IntegerValue.ToString() ).ToList() ;
        if ( conduitIds.Any() ) conduitSamePositionIds.AddRange( conduitIds ) ;
      }

      if ( ! conduitSamePositionIds.Any() ) return ;
      {
        var detailSymbols = detailSymbolModels.Where( d => conduitSamePositionIds.Contains( d.ConduitId ) && d.DetailSymbolId != oldDetailSymbolId && d.DetailSymbolId != detailSymbolId ).GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.First().LineIds ) ;
        if ( ! detailSymbols.Any() ) return ;
        foreach ( var (symbolId, strLineIds) in detailSymbols ) {
          var detailSymbolDifferenceRouteName = detailSymbolModels.FirstOrDefault( d => d.DetailSymbolId == symbolId && ! routeNames.Contains( d.RouteName ) ) ;
          if ( detailSymbolDifferenceRouteName != null ) continue ;
          var id = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.Id.IntegerValue.ToString() == symbolId ).Select( t => t.Id ).FirstOrDefault() ;
          if ( id != null ) doc.Delete( id ) ;
          foreach ( var lineId in strLineIds.Split( ',' ) ) {
            id = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.Id.IntegerValue.ToString() == lineId ).Select( e => e.Id ).FirstOrDefault() ;
            if ( id != null ) doc.Delete( id ) ;
          }
        }

        var detailSymbolAndCodes = detailSymbolModels.Where( d => conduitSamePositionIds.Contains( d.ConduitId ) && d.DetailSymbolId != oldDetailSymbolId && d.DetailSymbolId != detailSymbolId && d.IsParentSymbol && d.DetailSymbol != detailSymbol ).GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.First() ) ;
        conduitIdsSameRoute.AddRange( conduitSamePositionIds ) ;
        foreach ( var (_, detailSymbolModel) in detailSymbolAndCodes ) {
          UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolModels, detailSymbolModel.DetailSymbol, detailSymbolModel.Code, conduitIdsSameRoute ) ;
        }

        foreach ( var symbolModel in detailSymbolModels.Where( d => conduitSamePositionIds.Contains( d.ConduitId ) ) ) {
          symbolModel.DetailSymbolId = detailSymbolId ;
          symbolModel.DetailSymbol = detailSymbol ;
          symbolModel.LineIds = lineIds ;
          symbolModel.IsParentSymbol = isParentSymbol ;
        }
      }
    }

    private void UpdateSymbolOfConduitSameSymbolAndDifferentCode( Document doc, List<DetailSymbolModel> detailSymbolModels, string detailSymbol, string code, List<string> conduitSamePosition )
    {
      var firstChildSymbol = conduitSamePosition.Any() ? detailSymbolModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != code && ! conduitSamePosition.Contains( d.ConduitId ) ) : detailSymbolModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != code ) ;
      if ( firstChildSymbol == null ) return ;
      {
        var detailSymbolIds = detailSymbolModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).Select( d => d.DetailSymbolId ).Distinct().ToList() ;
        foreach ( var id in detailSymbolIds ) {
          var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.Id.IntegerValue.ToString() == id ) ;
          if ( textElement == null ) continue ;
          var textNote = ( textElement as TextNote ) ! ;
          CreateNewTextNoteType( doc, textNote, 0 ) ;
        }

        foreach ( var detailSymbolModel in detailSymbolModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).ToList() ) {
          detailSymbolModel.IsParentSymbol = true ;
        }
      }
    }

    private void SaveDetailSymbol( Document doc, DetailSymbolStorable detailSymbolStorable, Element conduit, TextNote detailSymbol, DetailSymbolSettingDialog detailSymbolSettingDialog, string lineIds, bool isParentSymbol )
    {
      try {
        List<DetailSymbolModel> detailSymbolModels = new List<DetailSymbolModel>() ;
        List<string> conduitIdsHasSymbol = detailSymbolStorable.DetailSymbolModelData.Select( d => d.ConduitId ).ToList() ;
        List<Element> allConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
        List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.Id != conduit.Id ).ToList() ;
        var routeName = conduit.GetRouteName() ;
        var representativeRouteName = conduit.GetRepresentativeRouteName() ;
        var code = GetCodeOfRouteToConnector( doc, allConnector, routeName! ) ;
        DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( conduit, detailSymbol.Id.IntegerValue.ToString(), detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, code ) ;
        detailSymbolModels.Add( detailSymbolModel ) ;
        AddDetailSymbolForConduitSameRoute( doc, allConduit, allConnector, detailSymbolModels, detailSymbol.Id.IntegerValue.ToString(), detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, detailSymbolModel.Code ) ;
        // update symbol of conduit same symbol and different code 
        if ( ! string.IsNullOrEmpty( detailSymbolModel.Code ) ) {
          var oldSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.Code == detailSymbolModel.Code ) ;
          if ( oldSymbol != null && oldSymbol.DetailSymbol != detailSymbolSettingDialog.DetailSymbol && oldSymbol.IsParentSymbol ) {
            List<string> conduitSamePosition = GetAllConduitIdsOfRouteSamePosition( doc, conduit ) ;
            UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolStorable.DetailSymbolModelData, oldSymbol.DetailSymbol, detailSymbolModel.Code, conduitSamePosition ) ;
          }
        }

        // add symbol for conduit same position
        if ( ! string.IsNullOrEmpty( representativeRouteName ) && ! string.IsNullOrEmpty( routeName ) && representativeRouteName != routeName ) {
          List<string> conduitIdsSameRoute = detailSymbolModels.Select( d => d.ConduitId ).ToList() ;
          UpdateSymbolOfConduitSamePosition( doc, detailSymbolStorable.DetailSymbolModelData, allConduit, conduit, conduitIdsHasSymbol, detailSymbol.Id.IntegerValue.ToString(), detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, representativeRouteName!, detailSymbol.Id.IntegerValue.ToString(), conduitIdsSameRoute ) ;
          AddDetailSymbolForConduitsSamePosition( doc, allConduit, allConnector, conduit, detailSymbolModels, detailSymbolSettingDialog, conduitIdsHasSymbol, representativeRouteName!, routeName!, detailSymbol.Id.IntegerValue.ToString(), lineIds, isParentSymbol ) ;
        }

        detailSymbolStorable.DetailSymbolModelData.AddRange( detailSymbolModels ) ;
        detailSymbolStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
      }
    }

    private string GetCodeOfRouteToConnector( Document doc, List<Element> allConnectors, string routeName )
    {
      string code = string.Empty ;
      var conduitsOfRoute = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var toElementId = toEndPointKey!.GetElementId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if ( toConnector == null || toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) continue ;
        code = GetCeeDSetCodeOfElement( toConnector ) ;
      }

      return code ;
    }

    private DetailSymbolModel CreateDetailSymbolModel( Element conduit, string detailSymbolId, string detailSymbol, string lineIds, bool isParentSymbol, string routeName, string code )
    {
      DetailSymbolModel detailSymbolModel = new DetailSymbolModel( detailSymbolId, detailSymbol, conduit.Id.IntegerValue.ToString(), routeName, code, lineIds, isParentSymbol ) ;
      return detailSymbolModel ;
    }

    private string GetCeeDSetCodeOfElement( Element element )
    {
      element.TryGetProperty( ConnectorFamilyParameter.CeeDCode, out string? ceeDSetCode ) ;
      return ceeDSetCode ?? string.Empty ;
    }

    private Category GetLineStyle( Document doc )
    {
      var categories = doc.Settings.Categories ;
      var subCategoryName = "MySubCategory" ;
      Category category = doc.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        var newColor = new Color( 0, 250, 0 ) ;
        subCategory.LineColor = newColor ;
      }
      else
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;

      return subCategory ;
    }

    private void AddDetailSymbolForConduitSameRoute( Document doc, List<Element> allConduit, List<Element> allConnector, List<DetailSymbolModel> detailSymbolModels, string detailSymbolId, string detailSymbol, string lineIds, bool isParentSymbol, string routeName, string code )
    {
      var conduitOfRoute = allConduit.Where( c => c.GetRouteName() == routeName ).ToList() ;
      if ( string.IsNullOrEmpty( code ) )
        code = GetCodeOfRouteToConnector( doc, allConnector, routeName ) ;
      foreach ( var conduit in conduitOfRoute ) {
        DetailSymbolModel detailSymbolModel = new DetailSymbolModel( detailSymbolId, detailSymbol, conduit.Id.IntegerValue.ToString(), routeName, code, lineIds, isParentSymbol ) ;
        detailSymbolModels.Add( detailSymbolModel ) ;
      }
    }

    private void AddDetailSymbolForConduitsSamePosition( Document doc, List<Element> allConduit, List<Element> allConnector, Element pickConduit, List<DetailSymbolModel> detailSymbolModels, DetailSymbolSettingDialog detailSymbolSettingDialog, List<string> conduitIdsHasSymbol, string conduitRepresentativeRouteName, string conduitRouteName, string detailSymbolId, string lineIds, bool isParentSymbol )
    {
      conduitIdsHasSymbol.AddRange( detailSymbolModels.Select( d => d.ConduitId ).ToList() ) ;
      var routeNamesSamePosition = GetRouteNameSamePosition( doc, conduitRepresentativeRouteName, pickConduit ) ;
      var routeNames = allConduit.Where( c => ! conduitIdsHasSymbol.Contains( c.Id.IntegerValue.ToString() ) && routeNamesSamePosition.Contains( c.GetRouteName()! ) && c.GetRouteName() != conduitRouteName ).Select( c => c.GetRouteName() ).Distinct().ToList() ;
      foreach ( var routeName in routeNames ) {
        var conduitsOfRouteName = allConduit.Where( c => c.GetRouteName() == routeName ).ToList() ;
        if ( ! conduitRouteName.Any() ) continue ;
        var code = GetCodeOfRouteToConnector( doc, allConnector, routeName! ) ;
        foreach ( var conduit in conduitsOfRouteName ) {
          DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( conduit, detailSymbolId, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, code ) ;
          detailSymbolModels.Add( detailSymbolModel ) ;
        }
      }
    }

    private void RemoveDetailSymbolUnused( Document doc, DetailSymbolStorable detailSymbolStorable )
    {
      var detailSymbolUnused = new List<DetailSymbolModel>() ;
      if ( ! detailSymbolStorable.DetailSymbolModelData.Any() ) return ;
      foreach ( var detailSymbolModel in detailSymbolStorable.DetailSymbolModelData ) {
        var conduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).FirstOrDefault( c => c.Id.IntegerValue.ToString() == detailSymbolModel.ConduitId ) ;
        var detailSymbol = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.Id.IntegerValue.ToString() == detailSymbolModel.DetailSymbolId ) ;
        if ( conduit == null || detailSymbol == null ) {
          detailSymbolUnused.Add( detailSymbolModel ) ;
        }
      }

      if ( ! detailSymbolUnused.Any() ) return ;
      foreach ( var detailSymbolModel in detailSymbolUnused ) {
        detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
      }

      detailSymbolStorable.Save() ;
    }

    private (List<string>, List<int>, string) CreateValueForCombobox( Document doc, List<DetailSymbolModel> detailSymbolModels, Element conduit )
    {
      List<string> symbols = new List<string>() ;
      for ( var letter = 'A' ; letter <= 'Z' ; letter++ ) {
        symbols.Add( letter.ToString() ) ;
      }

      var usedSymbols = detailSymbolModels.Select( d => d.DetailSymbol ).Distinct().ToList() ;
      var defaultSymbol = symbols.FirstOrDefault( s => ! usedSymbols.Contains( s ) ) ;

      List<int> angle = new List<int>() ;
      if ( conduit is Conduit ) {
        var location = ( conduit.Location as LocationCurve )! ;
        var line = ( location.Curve as Line )! ;
        if ( line.Direction.X is 1.0 or -1.0 )
          angle.AddRange( new List<int>() { 90, -90 } ) ;
        else if ( line.Direction.Y is 1.0 or -1.0 )
          angle.AddRange( new List<int>() { 0, 180 } ) ;
        else {
          angle.AddRange( new List<int>() { 0, 90, 180, -90 } ) ;
        }
      }
      else {
        var elbow = ( conduit as FamilyInstance )! ;
        if ( elbow.FacingOrientation.X is 1.0 or -1.0 )
          angle.AddRange( new List<int>() { 90, -90 } ) ;
        else if ( elbow.FacingOrientation.Y is 1.0 or -1.0 )
          angle.AddRange( new List<int>() { 0, 180 } ) ;
        else {
          angle.AddRange( new List<int>() { 0, 90, 180, -90 } ) ;
        }
      }

      return ( symbols, angle, defaultSymbol ) ;
    }

    private List<string> GetAllConduitIdsOfRouteSamePosition( Document doc, Element conduit )
    {
      var representativeRouteName = conduit.GetRepresentativeRouteName() ?? string.Empty ;
      var routeName = conduit.GetRouteName() ?? string.Empty ;
      List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var routeNames = GetRouteNameSamePosition( doc, representativeRouteName, conduit ) ;
      List<string> conduitIdsSamePosition = representativeRouteName != routeName ? allConduit.Where( c => routeNames.Contains( c.GetRouteName()! ) ).Select( c => c.Id.IntegerValue.ToString() ).ToList() : new List<string>() ;
      return conduitIdsSamePosition ;
    }

    private bool CheckDetailSymbolOfConduitDifferentCode( Document doc, Element conduit, List<DetailSymbolModel> detailSymbolModels, string detailSymbol )
    {
      List<string> conduitSamePosition = GetAllConduitIdsOfRouteSamePosition( doc, conduit ) ;
      List<Element> allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var routeName = conduit.GetRouteName() ;
      var code = GetCodeOfRouteToConnector( doc, allConnectors, routeName! ) ;
      if ( string.IsNullOrEmpty( code ) ) return true ;
      var detailSymbolModel = conduitSamePosition.Any() ? detailSymbolModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Code ) && d.Code != code && d.DetailSymbol == detailSymbol && d.IsParentSymbol && ! conduitSamePosition.Contains( d.ConduitId ) ) : detailSymbolModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Code ) && d.Code != code && d.DetailSymbol == detailSymbol && d.IsParentSymbol ) ;
      return detailSymbolModel == null ;
    }

    private void CreateNewTextNoteType( Document doc, TextNote textNote, double size, string symbolFont, string symbolStyle, int offset, int background, int widthScale, int color )
    {
      //Create new text type
      var bold = 0 ;
      var italic = 0 ;
      var underline = 0 ;
      string strStyleName = "TNT-" + symbolFont + "-" + color + "-" + size + "-" + background + "-" + offset + "-" + widthScale + "%" ;
      if ( symbolStyle == FontStyle.Bold.GetFieldName() ) {
        strStyleName += "-Bold" ;
        bold = 1 ;
      }
      else if ( symbolStyle == FontStyle.Italic.GetFieldName() ) {
        strStyleName += "-Italic" ;
        italic = 1 ;
      }
      else if ( symbolStyle == FontStyle.Underline.GetFieldName() ) {
        strStyleName += "-Underline" ;
        underline = 1 ;
      }

      var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( strStyleName, tt.Name ) ) ;
      if ( textNoteType == null ) {
        // Create new Note type
        Element ele = textNote.TextNoteType.Duplicate( strStyleName ) ;
        textNoteType = ( ele as TextNoteType ) ! ;

        textNoteType.get_Parameter( BuiltInParameter.LINE_COLOR ).Set( color ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_FONT ).Set( symbolFont ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( ( size / 32.0 ) * ( 1.0 / 12.0 ) ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( background ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_STYLE_BOLD ).Set( bold ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_STYLE_ITALIC ).Set( italic ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_STYLE_UNDERLINE ).Set( underline ) ;
        textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( ( offset / 32.0 ) * ( 1.0 / 12.0 ) ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_WIDTH_SCALE ).Set( (double) widthScale / 100 ) ;
      }

      // Change the text notes type to the new type
      textNote.ChangeTypeId( textNoteType!.Id ) ;
    }

    private void CreateNewTextNoteType( Document doc, TextNote textNote, int color )
    {
      //Create new text type
      string strStyleName = textNote.TextNoteType.Name + "-" + color ;

      var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( strStyleName, tt.Name ) ) ;
      if ( textNoteType == null ) {
        // Create new Note type
        Element ele = textNote.TextNoteType.Duplicate( strStyleName ) ;
        textNoteType = ( ele as TextNoteType ) ! ;
        textNoteType.get_Parameter( BuiltInParameter.LINE_COLOR ).Set( color ) ;
      }

      // Change the text notes type to the new type
      textNote.ChangeTypeId( textNoteType!.Id ) ;
    }
  }
}