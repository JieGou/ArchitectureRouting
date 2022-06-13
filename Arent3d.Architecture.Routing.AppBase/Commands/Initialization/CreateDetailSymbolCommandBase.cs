using System.Collections.Generic ;
using System.Drawing ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
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
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;

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

        var (symbols, angle, defaultSymbol) = CreateValueForCombobox( detailSymbolStorable.DetailSymbolModelData, conduit ) ;
        var detailSymbolSettingDialog = new DetailSymbolSettingDialog( symbols, angle, defaultSymbol ) ;
        detailSymbolSettingDialog.ShowDialog() ;
        if ( ! ( detailSymbolSettingDialog.DialogResult ?? false ) ) return Result.Cancelled ;

        var isParentSymbol = CheckDetailSymbolOfConduitDifferentCode( doc, conduit, detailSymbolStorable.DetailSymbolModelData, detailSymbolSettingDialog.DetailSymbol ) ;
        XYZ firstPoint = element.GlobalPoint ;
        var (textNote, lineIds) = CreateDetailSymbol( doc, detailSymbolSettingDialog, firstPoint, detailSymbolSettingDialog.Angle, isParentSymbol ) ;

        SaveDetailSymbol( doc, detailSymbolStorable, conduit, textNote, detailSymbolSettingDialog, lineIds, isParentSymbol ) ;

        return Result.Succeeded ;
      } ) ;
    }

    private ( TextNote, string) CreateDetailSymbol( Document doc, DetailSymbolSettingDialog detailSymbolSettingDialog, XYZ firstPoint, string angle, bool isParentSymbol )
    {
      var scale = ImportDwgMappingModel.GetDefaultSymbolMagnification( doc ) ;
      var baseLengthOfLine = 1d.MillimetersToRevitUnits() * scale ;
      
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
      lineIds.Add( startLine.UniqueId ) ;

      var bg = detailSymbolSettingDialog.BackGround ;
      List<XYZ> points = new List<XYZ>() ;
      switch ( angle ) {
        case "0" :
          points.Add( new XYZ( firstPoint.X - baseLengthOfLine * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X + (bg == 0 ? baseLengthOfLine : 0), firstPoint.Y, firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y + baseLengthOfLine * 10, firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X - baseLengthOfLine * ( 3 + size ), firstPoint.Y + baseLengthOfLine * 10, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y - (bg == 0 ? baseLengthOfLine : 0), firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "180" :
          points.Add( new XYZ( firstPoint.X + baseLengthOfLine * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X - (bg == 0 ? baseLengthOfLine : 0), firstPoint.Y, firstPoint.Z ) ;
          isLeft = false ;
          break ;
        case "-90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y - baseLengthOfLine * ( 8 + size ), firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X + baseLengthOfLine * ( 5 + size ), firstPoint.Y - baseLengthOfLine * ( 8 + size ), firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y + (bg == 0 ? baseLengthOfLine : 0), firstPoint.Z ) ;
          isLeft = false ;
          break ;
      }

      foreach ( var nextP in points ) {
        var curve = Line.CreateBound( firstPoint, nextP ) ;
        var detailCurve = doc.Create.NewDetailCurve( doc.ActiveView, curve ) ;
        lineIds.Add( detailCurve.UniqueId ) ;
        firstPoint = nextP ;
      }

      var defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
      doc.GetElement( defaultTextTypeId ).get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( 1d.MillimetersToRevitUnits() ) ;
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

      var txtPosition = new XYZ( firstPoint.X + ( isLeft ? baseLengthOfLine : -baseLengthOfLine * 2 ), firstPoint.Y + baseLengthOfLine * 6, firstPoint.Z ) ;
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, detailSymbolSettingDialog.DetailSymbol, opts ) ;
      CreateNewTextNoteType( doc, textNote, size, detailSymbolSettingDialog.SymbolFont, detailSymbolSettingDialog.SymbolStyle, detailSymbolSettingDialog.BackGround, detailSymbolSettingDialog.PercentWidth, txtColor ) ;
      return ( textNote, string.Join( ",", lineIds ) ) ;
    }

    public List<string> GetRouteNameSamePosition( Document doc, string representativeRouteName, Element pickConduit )
    {
      List<string> routeNames = new List<string>() ;
      if ( pickConduit is Conduit ) {
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
      }
      else {
        var routeNamesSamePosition = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).Select( c => c.GetRouteName() ).Distinct().ToList() ;
        var conduitFittingsOfRoutes = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => routeNamesSamePosition.Contains( c.GetRouteName() ) ).ToList() ;
        var pickConduitFitting = doc.GetElementById<FamilyInstance>( pickConduit.Id )! ;
        var location = ( pickConduitFitting.Location as LocationPoint )! ;
        var origin = location.Point ;
        var direction = pickConduitFitting.FacingOrientation ;
        foreach ( var conduitFitting in conduitFittingsOfRoutes ) {
          var anotherConduitFitting = doc.GetElementById<FamilyInstance>( conduitFitting.Id )! ;
          var anotherLocation = ( anotherConduitFitting.Location as LocationPoint )! ;
          var anotherOrigin = anotherLocation.Point ;
          var anotherDirection = anotherConduitFitting.FacingOrientation ;
          if ( anotherOrigin.DistanceTo( origin ) == 0 && anotherDirection.DistanceTo( direction ) == 0 && ! routeNames.Contains( conduitFitting.GetRouteName()! ) )
            routeNames.Add( conduitFitting.GetRouteName()! ) ;
        }
      }

      return routeNames ;
    }

    private void DeleteDetailSymbol( Document doc, string detailSymbolId, string lineIds )
    {
      var symbolId = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.UniqueId == detailSymbolId ).Select( t => t.Id ).FirstOrDefault() ;
      if ( symbolId != null ) doc.Delete( symbolId ) ;
      foreach ( var lineId in lineIds.Split( ',' ) ) {
        var id = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.UniqueId == lineId ).Select( e => e.Id ).FirstOrDefault() ;
        if ( id != null ) doc.Delete( id ) ;
      }
    }

    private void UpdateSymbolOfConduitSameSymbolAndDifferentCode( Document doc, List<DetailSymbolModel> detailSymbolModels, string detailSymbol, string ceedCode, List<string> conduitSamePosition )
    {
      var firstChildSymbol = conduitSamePosition.Any() ? detailSymbolModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != ceedCode && ! conduitSamePosition.Contains( d.ConduitId ) ) : detailSymbolModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != ceedCode ) ;
      if ( firstChildSymbol == null ) return ;
      {
        var detailSymbolIds = detailSymbolModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).Select( d => d.DetailSymbolId ).Distinct().ToList() ;
        foreach ( var id in detailSymbolIds ) {
          var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.UniqueId == id ) ;
          if ( textElement == null ) continue ;
          var textNote = ( textElement as TextNote ) ! ;
          CreateNewTextNoteType( doc, textNote, 0 ) ;
        }

        foreach ( var detailSymbolModel in detailSymbolModels.Where( d => d.DetailSymbol == firstChildSymbol.DetailSymbol && d.Code == firstChildSymbol.Code ).ToList() ) {
          detailSymbolModel.IsParentSymbol = true ;
        }
      }
    }

    private string? GetRepresentativeRouteName( Document document, Element conduit, string routeName )
    {
      var representativeRouteName = string.Empty ;
      if ( conduit is Conduit ) {
        representativeRouteName = conduit.GetRepresentativeRouteName() ;
      }
      else {
        var conduitOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).FirstOrDefault( c => c.GetRouteName() == routeName && c.GetRepresentativeRouteName() != routeName ) ;
        if ( conduitOfRoute != null )
          representativeRouteName = conduitOfRoute.GetRepresentativeRouteName() ;
      }

      return representativeRouteName ;
    }

    private void SaveDetailSymbol( Document doc, DetailSymbolStorable detailSymbolStorable, Element conduit, TextNote detailSymbol, DetailSymbolSettingDialog detailSymbolSettingDialog, string lineIds, bool isParentSymbol )
    {
      try {
        const string defaultPlumbingType = "E" ;
        List<DetailSymbolModel> detailSymbolModels = new List<DetailSymbolModel>() ;
        List<DetailSymbolModel> detailSymbolModelsIsDeleted = new List<DetailSymbolModel>() ;
        List<Element> allConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
        List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.Id != conduit.Id ).ToList() ;
        var routeName = conduit.GetRouteName() ;
        var representativeRouteName = GetRepresentativeRouteName( doc, conduit, routeName! ) ;
        var (ceedCode, deviceSymbol) = GetCeedCodeAndDeviceSymbolOfRouteToConnector( doc, allConnector, routeName! ) ;

        var routeNameSamePosition = GetRouteNameSamePosition( doc, representativeRouteName!, conduit ) ;
        var oldDetailSymbolModel = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.ConduitId == conduit.UniqueId && d.CountCableSamePosition == routeNameSamePosition.Count ) ;
        var plumbingType = oldDetailSymbolModel == null ? defaultPlumbingType : oldDetailSymbolModel.PlumbingType ;
        if ( oldDetailSymbolModel != null )
          if ( routeName == representativeRouteName )
            detailSymbolModelsIsDeleted = detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbolId == oldDetailSymbolModel.DetailSymbolId && d.RouteName == oldDetailSymbolModel.RouteName ).ToList() ;
          else
            detailSymbolModelsIsDeleted = detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbolId == oldDetailSymbolModel.DetailSymbolId && routeNameSamePosition.Contains( oldDetailSymbolModel.RouteName ) && d.CountCableSamePosition == routeNameSamePosition.Count ).ToList() ;

        DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( conduit, detailSymbol.UniqueId, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, ceedCode, routeNameSamePosition.Count, deviceSymbol, plumbingType ) ;
        detailSymbolModels.Add( detailSymbolModel ) ;
        AddDetailSymbolForConduitSameRoute( doc, allConduit, allConnector, detailSymbolModels, detailSymbol.UniqueId, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, detailSymbolModel.Code, routeNameSamePosition.Count, deviceSymbol, plumbingType ) ;

        // update symbol of conduit same symbol and different code 
        if ( ! string.IsNullOrEmpty( detailSymbolModel.Code ) ) {
          var oldSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.Code == detailSymbolModel.Code && d.CountCableSamePosition == routeNameSamePosition.Count ) ;
          if ( oldSymbol != null && oldSymbol.DetailSymbol != detailSymbolSettingDialog.DetailSymbol && oldSymbol.IsParentSymbol ) {
            List<string> conduitSamePosition = GetAllConduitIdsOfRouteSamePosition( doc, conduit ) ;
            UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolStorable.DetailSymbolModelData, oldSymbol.DetailSymbol, detailSymbolModel.Code, conduitSamePosition ) ;
          }
        }

        // add symbol for conduit same position
        if ( ! string.IsNullOrEmpty( representativeRouteName ) && ! string.IsNullOrEmpty( routeName ) && representativeRouteName != routeName ) {
          AddDetailSymbolForConduitsSamePosition( doc, allConduit, allConnector, detailSymbolModels, detailSymbolSettingDialog, routeName!, detailSymbol.UniqueId, lineIds, isParentSymbol, routeNameSamePosition, plumbingType ) ;
        }

        detailSymbolStorable.DetailSymbolModelData.AddRange( detailSymbolModels ) ;

        // remove old detail symbol
        if ( detailSymbolModelsIsDeleted.Any() ) {
          var detailSymbols = detailSymbolModelsIsDeleted.GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.First().LineIds ) ;
          if ( detailSymbols.Any() ) {
            foreach ( var (symbolId, strLineIds) in detailSymbols ) {
              DeleteDetailSymbol( doc, symbolId, strLineIds ) ;
            }
          }

          foreach ( var detailSymbolModelIsDeleted in detailSymbolModelsIsDeleted ) {
            detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModelIsDeleted ) ;
          }
        }
        
        //Update detail table when update detail symbol
        if ( oldDetailSymbolModel != null )
          UpdateDetailTableModel( doc, oldDetailSymbolModel.DetailSymbolId, detailSymbol.UniqueId, detailSymbolSettingDialog.DetailSymbol ) ;

        detailSymbolStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
      }
    }

    private void UpdateDetailTableModel( Document doc, string oldDetailSymbolId, string newDetailSymbolId, string detailSymbol )
    {
      try {
        DetailTableStorable detailTableStorable = doc.GetDetailTableStorable() ;
        var detailTableModels = detailTableStorable.DetailTableModelData.Where( d => d.DetailSymbolId == oldDetailSymbolId ).ToList() ;
        if ( ! detailTableModels.Any() ) return ;
        foreach ( var detailTableModel in detailTableModels ) {
          detailTableModel.DetailSymbolId = newDetailSymbolId ;
          detailTableModel.DetailSymbol = detailSymbol ;
        }

        detailTableStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private ( string, string ) GetCeedCodeAndDeviceSymbolOfRouteToConnector( Document doc, List<Element> allConnectors, string routeName )
    {
      string ceedCode = string.Empty ;
      string deviceSymbol = string.Empty ;
      var conduitsOfRoute = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementUniqueId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        ( ceedCode, deviceSymbol ) = GetCeedCodeAndDeviceSymbolOfElement( toConnector ) ;
      }

      return ( ceedCode, deviceSymbol ) ;
    }

    private DetailSymbolModel CreateDetailSymbolModel( Element conduit, string detailSymbolId, string detailSymbol, string lineIds, bool isParentSymbol, string routeName, string ceedCode, int countCableSamePosition, string deviceSymbol, string plumbingType )
    {
      DetailSymbolModel detailSymbolModel = new DetailSymbolModel( detailSymbolId, detailSymbol, conduit.UniqueId, routeName, ceedCode, lineIds, isParentSymbol, countCableSamePosition, deviceSymbol, plumbingType ) ;
      return detailSymbolModel ;
    }

    private ( string, string ) GetCeedCodeAndDeviceSymbolOfElement( Element element )
    {
      element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
      if ( string.IsNullOrEmpty( ceedSetCodeModel ) ) return ( string.Empty, string.Empty ) ;
      var ceedSetCode = ceedSetCodeModel!.Split( ':' ).ToList() ;
      var ceedCode = ceedSetCode.FirstOrDefault() ;
      var deviceSymbol = ceedSetCode.ElementAt( 1 ) ;
      return ( ceedCode ?? string.Empty, deviceSymbol ?? string.Empty ) ;
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

    private void AddDetailSymbolForConduitSameRoute( Document doc, List<Element> allConduit, List<Element> allConnector, List<DetailSymbolModel> detailSymbolModels, string detailSymbolId, string detailSymbol, string lineIds, bool isParentSymbol, string routeName, string ceedCode, int countCableSamePosition, string deviceSymbol, string plumbingType )
    {
      var conduitOfRoute = allConduit.Where( c => c.GetRouteName() == routeName ).ToList() ;
      if ( string.IsNullOrEmpty( ceedCode ) )
        ( ceedCode, deviceSymbol ) = GetCeedCodeAndDeviceSymbolOfRouteToConnector( doc, allConnector, routeName ) ;
      foreach ( var conduit in conduitOfRoute ) {
        DetailSymbolModel detailSymbolModel = new DetailSymbolModel( detailSymbolId, detailSymbol, conduit.UniqueId, routeName, ceedCode, lineIds, isParentSymbol, countCableSamePosition, deviceSymbol, plumbingType ) ;
        detailSymbolModels.Add( detailSymbolModel ) ;
      }
    }

    private void AddDetailSymbolForConduitsSamePosition( Document doc, List<Element> allConduit, List<Element> allConnector, List<DetailSymbolModel> detailSymbolModels, DetailSymbolSettingDialog detailSymbolSettingDialog, string conduitRouteName, string detailSymbolId, string lineIds, bool isParentSymbol, List<string> routeNamesSamePosition, string plumbingType )
    {
      var routeNames = allConduit.Where( c => routeNamesSamePosition.Contains( c.GetRouteName()! ) && c.GetRouteName() != conduitRouteName ).Select( c => c.GetRouteName() ).Distinct().ToList() ;
      foreach ( var routeName in routeNames ) {
        var conduitsOfRouteName = allConduit.Where( c => c.GetRouteName() == routeName ).ToList() ;
        if ( ! conduitRouteName.Any() ) continue ;
        var (ceedCode, deviceSymbol) = GetCeedCodeAndDeviceSymbolOfRouteToConnector( doc, allConnector, routeName! ) ;
        foreach ( var conduit in conduitsOfRouteName ) {
          DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( conduit, detailSymbolId, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol, routeName!, ceedCode, routeNamesSamePosition.Count, deviceSymbol, plumbingType ) ;
          detailSymbolModels.Add( detailSymbolModel ) ;
        }
      }
    }

    private void RemoveDetailSymbolUnused( Document doc, DetailSymbolStorable detailSymbolStorable )
    {
      var detailSymbolUnused = new List<DetailSymbolModel>() ;
      if ( ! detailSymbolStorable.DetailSymbolModelData.Any() ) return ;
      foreach ( var detailSymbolModel in detailSymbolStorable.DetailSymbolModelData ) {
        var conduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).FirstOrDefault( c => c.UniqueId == detailSymbolModel.ConduitId ) ;
        var detailSymbol = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.UniqueId == detailSymbolModel.DetailSymbolId ) ;
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

    private (List<string>, List<int>, string) CreateValueForCombobox( List<DetailSymbolModel> detailSymbolModels, Element conduit )
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
      var routeName = conduit.GetRouteName() ?? string.Empty ;
      var representativeRouteName = GetRepresentativeRouteName( doc, conduit, routeName ) ?? string.Empty ;
      List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var routeNames = GetRouteNameSamePosition( doc, representativeRouteName, conduit ) ;
      List<string> conduitIdsSamePosition = representativeRouteName != routeName ? allConduit.Where( c => routeNames.Contains( c.GetRouteName()! ) ).Select( c => c.UniqueId ).ToList() : new List<string>() ;
      return conduitIdsSamePosition ;
    }

    private bool CheckDetailSymbolOfConduitDifferentCode( Document doc, Element conduit, List<DetailSymbolModel> detailSymbolModels, string detailSymbol )
    {
      List<string> conduitSamePosition = GetAllConduitIdsOfRouteSamePosition( doc, conduit ) ;
      List<Element> allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var routeName = conduit.GetRouteName() ;
      var (ceedCode, _) = GetCeedCodeAndDeviceSymbolOfRouteToConnector( doc, allConnectors, routeName! ) ;
      if ( string.IsNullOrEmpty( ceedCode ) ) return true ;
      var detailSymbolModel = conduitSamePosition.Any() ? detailSymbolModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Code ) && d.Code != ceedCode && d.DetailSymbol == detailSymbol && d.IsParentSymbol && ! conduitSamePosition.Contains( d.ConduitId ) ) : detailSymbolModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Code ) && d.Code != ceedCode && d.DetailSymbol == detailSymbol && d.IsParentSymbol ) ;
      return detailSymbolModel == null ;
    }

    private void CreateNewTextNoteType( Document doc, TextNote textNote, double size, string symbolFont, string symbolStyle, int background, int widthScale, int color )
    {
      //Create new text type
      var bold = 0 ;
      var italic = 0 ;
      var underline = 0 ;
      string strStyleName = "DetailSymbol-TNT-" + symbolFont + "-" + color + "-" + size + "-" + background + "-" + widthScale + "%" ;
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
        textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( 1d.MillimetersToRevitUnits() ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_WIDTH_SCALE ).Set( (double) widthScale / 100 ) ;
        textNoteType?.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
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