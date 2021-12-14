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
        var element = selection.PickObject( ObjectType.Element, ConduitSelectionFilter.Instance, "Select cable." ) ;
        var conduit = doc.GetElement( element.ElementId ) ;
        var conduitHasSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.ConduitId == conduit.Id.IntegerValue.ToString() ) ;

        var (symbols, angle) = CreateValueForCombobox( doc, detailSymbolStorable.DetailSymbolModelData, conduit ) ;
        var detailSymbolSettingDialog = new DetailSymbolSettingDialog( symbols, angle ) ;
        detailSymbolSettingDialog.ShowDialog() ;
        if ( ! ( detailSymbolSettingDialog.DialogResult ?? false ) ) return Result.Cancelled ;

        var checkSameSymbol = CheckDetailSymbolOfConduitDifferentCode( doc, conduit, detailSymbolStorable.DetailSymbolModelData, detailSymbolSettingDialog.DetailSymbol ) ;
        XYZ firstPoint = element.GlobalPoint ;
        var (textNote, lineIds) = CreateDetailSymbol( doc, detailSymbolSettingDialog, firstPoint, detailSymbolSettingDialog.Angle, checkSameSymbol ) ;

        if ( conduitHasSymbol != null )
          UpdateDetailSymbol( doc, detailSymbolStorable, conduitHasSymbol, textNote, detailSymbolSettingDialog.DetailSymbol, lineIds, checkSameSymbol ) ;
        else
          SaveDetailSymbol( doc, detailSymbolStorable, conduit, textNote, detailSymbolSettingDialog, lineIds, checkSameSymbol ) ;

        return Result.Succeeded ;
      } ) ;
    }

    private ( TextNote, string) CreateDetailSymbol( Document doc, DetailSymbolSettingDialog detailSymbolSettingDialog, XYZ firstPoint, string angle, bool checkSameSymbol )
    {
      const double dPlus = 0.2 ;
      var isLeft = true ;
      var size = detailSymbolSettingDialog.HeightCharacter ;
      // create color using Color.FromArgb with RGB inputs
      var color = System.Drawing.Color.FromArgb(255,0,0);
      // convert color into an integer
      var colorInt = System.Drawing.ColorTranslator.ToWin32(color);
      var txtColor = checkSameSymbol ? 0 : colorInt ;
      List<string> lineIds = new List<string>() ;
      var startLineP1 = new XYZ( firstPoint.X + dPlus, firstPoint.Y + dPlus, firstPoint.Z ) ;
      var endLineP1 = new XYZ( firstPoint.X - dPlus, firstPoint.Y - dPlus, firstPoint.Z ) ;
      Curve startCurve = Line.CreateBound( startLineP1, endLineP1 ) ;
      var startLine = doc.Create.NewDetailCurve( doc.ActiveView, startCurve ) ;
      var subCategory = GetLineStyle( doc ) ;
      startLine.LineStyle = subCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      lineIds.Add( startLine.Id.IntegerValue.ToString() ) ;

      List<XYZ> points = new List<XYZ>() ;
      switch ( angle ) {
        case "0" :
          points.Add( new XYZ( firstPoint.X - dPlus * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X + dPlus, firstPoint.Y, firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y + dPlus * 10, firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X - dPlus * ( 3 + size ), firstPoint.Y + dPlus * 10, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y - dPlus, firstPoint.Z ) ;
          isLeft = true ;
          break ;
        case "180" :
          points.Add( new XYZ( firstPoint.X + dPlus * ( 7 + size ), firstPoint.Y, firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X - dPlus, firstPoint.Y, firstPoint.Z ) ;
          isLeft = false ;
          break ;
        case "-90" :
          points.Add( new XYZ( firstPoint.X, firstPoint.Y - dPlus * ( 8 + size ), firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X + dPlus * ( 5 + size ), firstPoint.Y - dPlus * ( 8 + size ), firstPoint.Z ) ) ;
          firstPoint = new XYZ( firstPoint.X, firstPoint.Y + dPlus, firstPoint.Z ) ;
          isLeft = false ;
          break ;
      }

      foreach ( var nextP in points ) {
        if ( firstPoint.DistanceTo( nextP ) > 0.001 ) {
          var curve = Line.CreateBound( firstPoint, nextP ) ;
          var detailCurve = doc.Create.NewDetailCurve( doc.ActiveView, curve ) ;
          lineIds.Add( detailCurve.Id.IntegerValue.ToString() ) ;
        }

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

      var txtPosition = new XYZ( firstPoint.X + ( isLeft ? dPlus : -dPlus * 4 ), firstPoint.Y + dPlus * ( 1 + size * 2 ), firstPoint.Z ) ;
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, detailSymbolSettingDialog.DetailSymbol, opts ) ;
      CreateNewTextNoteType( doc, textNote, size, detailSymbolSettingDialog.SymbolFont, detailSymbolSettingDialog.SymbolStyle, detailSymbolSettingDialog.Offset, detailSymbolSettingDialog.BackGround, detailSymbolSettingDialog.PercentWidth, txtColor ) ;
      return ( textNote, string.Join( ",", lineIds ) ) ;
    }

    private void UpdateDetailSymbol( Document doc, DetailSymbolStorable detailSymbolStorable, DetailSymbolModel detailSymbolModel, TextNote symbol, string detailSymbol, string lineIds, bool checkSameSymbol )
    {
      try {
        var oldSymbol = detailSymbolModel.DetailSymbol ;
        var oldParentSymbol = detailSymbolModel.ParentSymbol ;
        var parentSymbol = checkSameSymbol ? 0 : 1 ;
        // delete old symbol
        var symbolId = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.Id.IntegerValue.ToString() == detailSymbolModel.DetailSymbolId ).Select( t => t.Id ).FirstOrDefault() ;
        if ( symbolId != null ) doc.Delete( symbolId ) ;
        foreach ( var lineId in detailSymbolModel.LineIds.Split( ',' ) ) {
          var id = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.Id.IntegerValue.ToString() == lineId ).Select( e => e.Id ).FirstOrDefault() ;
          if ( id != null ) doc.Delete( id ) ;
        }

        // update symbol of cables same from-to connector
        foreach ( var symbolModel in detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbol == oldSymbol && d.FromConnectorId == detailSymbolModel.FromConnectorId && d.ToConnectorId == detailSymbolModel.ToConnectorId ) ) {
          symbolModel.DetailSymbolId = symbol.Id.IntegerValue.ToString() ;
          symbolModel.DetailSymbol = detailSymbol ;
          symbolModel.LineIds = lineIds ;
          symbolModel.ParentSymbol = parentSymbol ;
        }

        if ( ! string.IsNullOrEmpty( detailSymbolModel.Code ) ) {
          // update symbol of cables same code
          UpdateSymbolOfConduitSameCode( doc, detailSymbolStorable.DetailSymbolModelData, detailSymbol, detailSymbolModel.Code, symbol.TextNoteType, parentSymbol ) ;

          // update symbol's text color of cables different code and same symbol
          if ( oldSymbol != detailSymbol && oldParentSymbol == 0 ) {
            UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolStorable.DetailSymbolModelData, oldSymbol, detailSymbolModel.Code ) ;
          }
        }

        detailSymbolStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
      }
    }

    private void UpdateSymbolOfConduitSameCode( Document doc, List<DetailSymbolModel> detailSymbolModels, string detailSymbol, string code, TextNoteType textNoteType, int parentSymbol )
    {
      List<string> detailSymbolIds = new List<string>() ;
      foreach ( var symbolModel in detailSymbolModels.Where( d => d.Code == code ) ) {
        symbolModel.DetailSymbol = detailSymbol ;
        symbolModel.ParentSymbol = parentSymbol ;
        if ( ! detailSymbolIds.Contains( symbolModel.DetailSymbolId ) )
          detailSymbolIds.Add( symbolModel.DetailSymbolId ) ;
      }

      if ( ! detailSymbolIds.Any() ) return ;
      foreach ( var id in detailSymbolIds ) {
        var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.Id.IntegerValue.ToString() == id ) ;
        if ( textElement == null ) continue ;
        var textNote = ( textElement as TextNote ) ! ;
        textNote.Text = detailSymbol ;
        textNote.TextNoteType = textNoteType ;
      }
    }
    
    private void UpdateSymbolOfConduitSameSymbolAndDifferentCode( Document doc, List<DetailSymbolModel> detailSymbolModels, string detailSymbol, string code )
    {
      var firstChildSymbol = detailSymbolModels.FirstOrDefault( d => d.DetailSymbol == detailSymbol && d.Code != code ) ;
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
          detailSymbolModel.ParentSymbol = 0 ;
        }
      }
    }

    private void SaveDetailSymbol( Document doc, DetailSymbolStorable detailSymbolStorable, Element conduit, TextNote detailSymbol, DetailSymbolSettingDialog detailSymbolSettingDialog, string lineIds, bool checkSameSymbol )
    {
      try {
        List<DetailSymbolModel> detailSymbolModels = new List<DetailSymbolModel>() ;
        List<string> conduitIdsHasSymbol = detailSymbolStorable.DetailSymbolModelData.Select( d => d.ConduitId ).ToList() ;
        List<Element> allConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
        List<Element> allConduit = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.Id != conduit.Id ).ToList() ;
        DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( doc, allConnector, conduit, detailSymbol.Id.IntegerValue.ToString(), detailSymbolSettingDialog.DetailSymbol, lineIds, checkSameSymbol ) ;
        detailSymbolModels.Add( detailSymbolModel ) ;
        AddDetailSymbolForConduitSameFromToConnectors( doc, allConduit, allConnector, detailSymbolModels, detailSymbol.Id.IntegerValue.ToString(), detailSymbolSettingDialog.DetailSymbol, detailSymbolModel.FromConnectorId, detailSymbolModel.ToConnectorId, lineIds, checkSameSymbol ) ;
        if ( ! string.IsNullOrEmpty( detailSymbolModel.Code ) ) {
          var parentSymbol = checkSameSymbol ? 0 : 1 ;
          var oldSymbol = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( d => d.Code == detailSymbolModel.Code ) ;
          AddDetailSymbolForConduitSameCode( doc, allConduit, allConnector, detailSymbolModels, detailSymbolSettingDialog, conduitIdsHasSymbol, detailSymbolModel.Code, checkSameSymbol ) ;
          UpdateSymbolOfConduitSameCode( doc, detailSymbolStorable.DetailSymbolModelData, detailSymbolSettingDialog.DetailSymbol, detailSymbolModel.Code, detailSymbol.TextNoteType, parentSymbol ) ;
          if ( oldSymbol != null && oldSymbol.DetailSymbol != detailSymbolSettingDialog.DetailSymbol && oldSymbol.ParentSymbol == 0 ) 
            UpdateSymbolOfConduitSameSymbolAndDifferentCode( doc, detailSymbolStorable.DetailSymbolModelData, oldSymbol.DetailSymbol, detailSymbolModel.Code ) ;
        }
        detailSymbolStorable.DetailSymbolModelData.AddRange( detailSymbolModels ) ;
        detailSymbolStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
      }
    }

    private DetailSymbolModel CreateDetailSymbolModel( Document doc, List<Element> allConnectors, Element conduit, string detailSymbolId, string detailSymbol, string lineIds, bool checkSameSymbol )
    {
      var code = string.Empty ;
      var fromElementId = string.Empty ;
      var toElementId = string.Empty ;

      var fromEndPoint = conduit.GetNearestEndPoints( true ).ToList() ;
      if ( fromEndPoint.Any() ) {
        var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
        fromElementId = fromEndPointKey!.GetElementId() ;
        if ( ! string.IsNullOrEmpty( fromElementId ) ) {
          var fromConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
          if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
            fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorId, out string? fromConnectorId ) ;
            if ( ! string.IsNullOrEmpty( fromConnectorId ) ) {
              fromElementId = fromConnectorId! ;
            }
          }
        }
      }

      var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
      if ( toEndPoint.Any() ) {
        var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
        toElementId = toEndPointKey!.GetElementId() ;
        if ( ! string.IsNullOrEmpty( toElementId ) ) {
          var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
          if ( toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) {
            toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? toConnectorId ) ;
            toElementId = toConnectorId! ;
            if ( ! string.IsNullOrEmpty( toElementId ) )
              toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
          }

          if ( toConnector != null )
            code = GetCeeDSetCodeOfElement( doc, toConnector ) ;
        }
      }

      var parentSymbol = checkSameSymbol ? 0 : 1 ;
      DetailSymbolModel detailSymbolModel = new DetailSymbolModel( detailSymbolId, detailSymbol, conduit.Id.IntegerValue.ToString(), fromElementId, toElementId, code, lineIds, parentSymbol ) ;
      return detailSymbolModel ;
    }

    private string GetCeeDSetCodeOfElement( Document doc, Element element )
    {
      var ceeDSetCode = string.Empty ;
      if ( element.GroupId == ElementId.InvalidElementId ) return ceeDSetCode ;
      var groupId = doc.GetAllElements<Group>().FirstOrDefault( g => g.AttachedParentId == element.GroupId )?.Id ;
      if ( groupId != null )
        ceeDSetCode = doc.GetAllElements<TextNote>().FirstOrDefault( t => t.GroupId == groupId )?.Text.Trim( '\r' ) ;

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

    private void AddDetailSymbolForConduitSameFromToConnectors( Document doc, List<Element> allConduit, List<Element> allConnector, List<DetailSymbolModel> detailSymbolModels, string detailSymbolId, string detailSymbol, string fromConnectorId, string toConnectorId, string lineIds, bool checkSameSymbol )
    {
      foreach ( var conduit in allConduit ) {
        DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( doc, allConnector, conduit, detailSymbolId, detailSymbol, lineIds, checkSameSymbol ) ;
        if ( detailSymbolModel.FromConnectorId == fromConnectorId && detailSymbolModel.ToConnectorId == toConnectorId )
          detailSymbolModels.Add( detailSymbolModel ) ;
      }
    }

    private void AddDetailSymbolForConduitSameCode( Document doc, List<Element> allConduit, List<Element> allConnector, List<DetailSymbolModel> detailSymbolModels, DetailSymbolSettingDialog detailSymbolSettingDialog, List<string> conduitIdsHasSymbol, string code, bool checkSameSymbol )
    {
      Dictionary<string, List<Element>> conduitSameToConnectors = new Dictionary<string, List<Element>>() ;
      conduitIdsHasSymbol.AddRange( detailSymbolModels.Select( d => d.ConduitId ).ToList() ) ;
      var conduitsHaveNotSymbol = allConduit.Where( c => ! conduitIdsHasSymbol.Contains( c.Id.IntegerValue.ToString() ) ).ToList() ;
      foreach ( var conduit in conduitsHaveNotSymbol ) {
        DetailSymbolModel detailSymbolModel = CreateDetailSymbolModel( doc, allConnector, conduit, string.Empty, detailSymbolSettingDialog.DetailSymbol, string.Empty, checkSameSymbol ) ;
        if ( detailSymbolModel.Code != code ) continue ;
        detailSymbolModels.Add( detailSymbolModel ) ;
        var key = detailSymbolModel.FromConnectorId + "," + detailSymbolModel.ToConnectorId ;
        if ( conduitSameToConnectors.ContainsKey( key ) ) {
          conduitSameToConnectors[ key ].Add( conduit ) ;
        }
        else {
          conduitSameToConnectors.Add( key, new List<Element>() { conduit } ) ;
        }
      }

      if ( ! conduitSameToConnectors.Any() ) return ;
      {
        foreach ( var (key, elements) in conduitSameToConnectors ) {
          var maxLength = double.MinValue ;
          XYZ firstPoint = XYZ.Zero ;
          var isDirectionX = true ;
          var conduits = elements.Where( e => e is Conduit ).ToList() ;
          foreach ( var conduit in conduits ) {
            var location = ( conduit.Location as LocationCurve ) ! ;
            var line = ( location.Curve as Line ) ! ;
            if ( line.Direction.X is not (1.0 or -1.0) && line.Direction.Y is not (1.0 or -1.0) ) continue ;
            var length = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( doc, "Length" ) ).AsDouble() / 2 ;
            if ( ! ( length > maxLength ) ) continue ;
            maxLength = length ;
            isDirectionX = line.Direction.X is 1.0 or -1.0 ? true : false ;
            var (x, y, z) = line.Origin ;
            firstPoint = isDirectionX ? new XYZ( line.Direction.X is 1.0 ? x + length : x - length, y, z ) : new XYZ( x, line.Direction.Y is 1.0 ? y + length : y - length, z ) ;
          }

          var angle = isDirectionX ? "90" : "0" ;
          var (detailSymbol, lineIds) = CreateDetailSymbol( doc, detailSymbolSettingDialog, firstPoint, angle, checkSameSymbol ) ;
          foreach ( var element in elements ) {
            var detailSymbolModel = detailSymbolModels.FirstOrDefault( d => d.ConduitId == element.Id.IntegerValue.ToString() ) ;
            if ( detailSymbolModel == null ) continue ;
            detailSymbolModel.DetailSymbolId = detailSymbol.Id.IntegerValue.ToString() ;
            detailSymbolModel.LineIds = lineIds ;
          }
        }
      }
    }

    private (List<string>, List<int>) CreateValueForCombobox( Document doc, List<DetailSymbolModel> detailSymbolModels, Element conduit )
    {
      List<string> symbols = new List<string>() ;
      for ( var letter = 'A' ; letter <= 'Z' ; letter++ ) {
        symbols.Add( letter.ToString() ) ;
      }

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

      return ( symbols, angle ) ;
    }

    private bool CheckDetailSymbolOfConduitDifferentCode( Document doc, Element conduit, List<DetailSymbolModel> detailSymbolModels, string detailSymbol )
    {
      List<Element> allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
      if ( ! toEndPoint.Any() ) return true ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      var toElementId = toEndPointKey!.GetElementId() ;

      if ( string.IsNullOrEmpty( toElementId ) ) return true ;
      var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
      if ( toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) {
        toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toConnectorId ) ;
      }

      if ( toConnector == null ) return true ;
      var code = GetCeeDSetCodeOfElement( doc, toConnector ) ;
      if ( string.IsNullOrEmpty( code ) ) return true ;
      var detailSymbolModel = detailSymbolModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Code ) && d.Code != code && d.DetailSymbol == detailSymbol ) ;
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