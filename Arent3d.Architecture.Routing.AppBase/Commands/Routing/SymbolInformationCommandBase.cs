using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class SymbolInformationCommandBase : IExternalCommand
  {
    private const string SymbolInformationTextNoteTypeName10 = "1.0mm_SymbolInformationText" ;
    private const string SymbolInformationTextNoteTypeName12 = "1.2mm_SymbolInformationText" ;
    private const string SymbolInformationTextNoteTypeName15 = "1.5mm_SymbolInformationText" ;
    private const string SymbolInformationTextNoteTypeName18 = "1.8mm_SymbolInformationText" ;
    private const string SymbolInformationTextNoteTypeName20 = "2.0mm_SymbolInformationText" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var symbolInformationStorable = document.GetSymbolInformationStorable() ;
        var symbolInformationList = symbolInformationStorable.AllSymbolInformationModelData ;
        var level = uiDocument.ActiveView.GenLevel ;
        var heightOfSymbol = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        SymbolInformationModel? model = null ;
        FamilyInstance? symbolInformationInstance = null ;
        var xyz = XYZ.Zero ;


        var selectedItemIsSymbolInformation = false ;
        TextNote? textNote = null ;
        Group? oldParentGroup = null ;
        if ( uiDocument.Selection.GetElementIds().Count > 0 ) {
          var groupId = uiDocument.Selection.GetElementIds().First() ;
          if ( document.GetElement( groupId ) is Group parentGroup ) {
            oldParentGroup = parentGroup ;
            var elementId = GetElementIdOfSymbolInformationFromGroup( document, symbolInformationList, parentGroup, ref textNote ) ;
            if ( elementId != null ) {
              var symbolInformation = symbolInformationList.FirstOrDefault( x => x.Id == elementId.ToString() ) ;
              //pickedObject is SymbolInformationModel
              if ( null != symbolInformation ) {
                model = symbolInformation ;
                var symbolInformationSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ) ?? throw new InvalidOperationException() ;
                if ( model.SymbolKind == SymbolKindEnum.丸.ToString() ) {
                  symbolInformationSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolCircle ) ?? throw new InvalidOperationException() ;
                }

                symbolInformationInstance = document.GetAllFamilyInstances( symbolInformationSymbols ).FirstOrDefault( x => x.Id.ToString() == symbolInformation.Id ) ;
                xyz = symbolInformationInstance!.Location is LocationPoint pPoint ? pPoint.Point : XYZ.Zero ;
                selectedItemIsSymbolInformation = true ;
              }
              //pickedObject ISN'T SymbolInformationModel
              else {
                var element = document.GetElement( elementId ) ;
                if ( null != element.Location ) {
                  xyz = element.Location is LocationPoint pPoint ? pPoint.Point : XYZ.Zero ;
                }

                symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfSymbol ) ) ;
                model = new SymbolInformationModel { Id = symbolInformationInstance.Id.ToString() } ;
                symbolInformationList.Add( model ) ;
                selectedItemIsSymbolInformation = true ;
              }
            }
          }
        }

        if ( selectedItemIsSymbolInformation == false ) {
          using Transaction transaction = new(uiDocument.Document, "Electrical.App.Commands.Routing.SymbolInformationCommand") ;
          transaction.Start() ;
          try {
            xyz = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
            symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfSymbol ) ) ;
            model = new SymbolInformationModel { Id = symbolInformationInstance.Id.ToString(), Floor = level.Name } ;
            symbolInformationList.Add( model ) ;
            transaction.Commit() ;
          }
          catch ( OperationCanceledException ) {
            transaction.RollBack() ;
            return Result.Cancelled ;
          }
        }

        var viewModel = new SymbolInformationViewModel( document, model, commandData ) ;
        var dialog = new SymbolInformationDialog( viewModel ) ;
        var ceedDetailStorable = document.GetCeedDetailStorable() ;

        if ( dialog.ShowDialog() == true && model != null ) {
          using Transaction transaction = new(document, "Electrical.App.Commands.Routing.SymbolInformationCommand") ;
          transaction.Start() ;
          //Create group symbol information 
          if ( oldParentGroup != null ) {
            oldParentGroup.UngroupMembers() ;
          }

          //*****Save symbol setting*********** 
          if ( GetSymbolKindName( symbolInformationInstance!.Symbol.Name ) != viewModel.SymbolInformation.SymbolKind ) {
            var oldId = symbolInformationInstance.Id ;
            var oldSymbolInformation = symbolInformationList.FirstOrDefault( x => x.Id == oldId.ToString() ) ;
            document.Delete( oldId ) ;
            symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfSymbol ), GetElectricalRoutingFamilyType( viewModel.SelectedSymbolKind ) ) ;

            oldSymbolInformation!.Id = symbolInformationInstance!.Id.ToString() ;
            //Update parentId in viewModel.CeedDetailList
            foreach ( var ceedDetail in viewModel.CeedDetailList ) {
              ceedDetail.ParentId = symbolInformationInstance.Id.ToString() ;
            }
          }

          var symbolHeightParameter = symbolInformationInstance?.LookupParameter( "Symbol Height" ) ;
          symbolHeightParameter?.Set( model.Height.MillimetersToRevitUnits() ) ;
          symbolInformationStorable.Save() ;

          //****Save ceedDetails******
          //Delete old data
          ceedDetailStorable.AllCeedDetailModelData.RemoveAll( x => x.ParentId == model.Id ) ;
          //Add new data
          ceedDetailStorable.AllCeedDetailModelData.AddRange( viewModel.CeedDetailList ) ;
          ceedDetailStorable.Save() ;

          CreateGroupSymbolInformation( document, symbolInformationInstance!.Id, model, new XYZ( xyz.X, xyz.Y, heightOfSymbol ), oldParentGroup ) ;
          OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
          ogs.SetProjectionLineColor( SymbolColor.DictSymbolColor[ model.Color ] ) ;
          ogs.SetProjectionLineWeight( 5 ) ;
          document.ActiveView.SetElementOverrides( symbolInformationInstance!.Id, ogs ) ;

          transaction.Commit() ;
        }
        else if ( selectedItemIsSymbolInformation == false ) {
          using Transaction transaction = new(document, "Electrical.App.Commands.Routing.SymbolInformationCommand") ;
          transaction.Start() ;
          document.Delete( symbolInformationInstance?.Id ) ;
          transaction.Commit() ;
        }


        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Cancelled ;
      }
    }

    private static string GetSymbolKindName( string symbolName )
    {
      return symbolName switch
      {
        "Symbol Circle" => SymbolKindEnum.丸.GetFieldName(),
        _ => SymbolKindEnum.星.GetFieldName()
      } ;
    } 

    private static ElectricalRoutingFamilyType GetElectricalRoutingFamilyType( SymbolKindEnum symbolKind )
    {
      return symbolKind switch
      {
        SymbolKindEnum.丸 => ElectricalRoutingFamilyType.SymbolCircle,
        _ => ElectricalRoutingFamilyType.SymbolStar
      } ;
    }


    private static ElementId? GetElementIdOfSymbolInformationFromGroup( Document document, List<SymbolInformationModel> symbolInformations, Group group, ref TextNote? textNote )
    {
      var memberIds = group.GetMemberIds() ;
      foreach ( var memberId in memberIds ) {
        if ( symbolInformations.FirstOrDefault( x => x.Id == memberId.ToString() ) == null ) continue ;
        {
          var txtGroup = document.GetAllElements<Group>().FirstOrDefault( x => x.AttachedParentId == group.Id ) ;
          if ( txtGroup == null ) return memberId ;
          var txtId = txtGroup.GetMemberIds().FirstOrDefault() ;
          var txtNode = document.GetElement( txtId ) ;
          if ( txtNode != null ) {
            textNote = (TextNote) txtNode ;
          }

          return memberId ;
        }
      }

      return null ;
    }

    private static FamilyInstance GenerateSymbolInformation( UIDocument uiDocument, Level level, XYZ xyz, ElectricalRoutingFamilyType symbolKind = ElectricalRoutingFamilyType.SymbolStar )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( symbolKind ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
    }

    private static void CreateGroupSymbolInformation( Document document, ElementId symbolInformationInstanceId, SymbolInformationModel model, XYZ xyz, Group? oldParentGroup )
    {
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      groupIds.Add( symbolInformationInstanceId ) ;

      if ( model.IsShowText && ! string.IsNullOrEmpty( model.Description ) ) {
        var noteWidth = ( model.Height ).MillimetersToRevitUnits() ;
        var symbolKind = (SymbolKindEnum) Enum.Parse( typeof( SymbolKindEnum ), model.SymbolKind! ) ;
        var delta = 1.0 ;
        if ( symbolKind == SymbolKindEnum.丸 )
          delta *= 1.5 ;
        
        var anchor = (SymbolCoordinateEnum) Enum.Parse( typeof( SymbolCoordinateEnum ), model.SymbolCoordinate! ) ;

        XYZ txtPosition = anchor switch
        {
          SymbolCoordinateEnum.上 => new XYZ( xyz.X - noteWidth * 50, xyz.Y + noteWidth * delta * 100, xyz.Z ), //Up
          SymbolCoordinateEnum.左 => new XYZ( xyz.X - noteWidth * 200 * delta, xyz.Y, xyz.Z ), //Left
          SymbolCoordinateEnum.右 => new XYZ( xyz.X + noteWidth * 100 * delta, xyz.Y, xyz.Z ), //Right
          SymbolCoordinateEnum.下 => new XYZ( xyz.X - noteWidth * 50, xyz.Y - noteWidth * delta * 100, xyz.Z ), //Bottom
          _ => new XYZ( xyz.X - noteWidth * 50, xyz.Y, xyz.Z ) //Center
        } ;

        var defaultTextTypeId = document.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;

        // make sure note width works for the text type
        var minWidth = TextElement.GetMinimumAllowedWidth( document, defaultTextTypeId ) ;
        var maxWidth = TextElement.GetMaximumAllowedWidth( document, defaultTextTypeId ) ;
        noteWidth = noteWidth < minWidth ? minWidth : ( noteWidth > maxWidth ? maxWidth : noteWidth ) ;

        TextNoteOptions opts = new(defaultTextTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left, VerticalAlignment = VerticalTextAlignment.Middle, KeepRotatedTextReadable = true } ;

        var textNote = TextNote.Create( document, document.ActiveView.Id, txtPosition, noteWidth, model.Description, opts ) ;
        var textNodeTypeName = model.CharacterHeight switch
        {
          1 => SymbolInformationTextNoteTypeName10,
          2 => SymbolInformationTextNoteTypeName12,
          3 => SymbolInformationTextNoteTypeName15,
          4 => SymbolInformationTextNoteTypeName18,
          _ => SymbolInformationTextNoteTypeName20
        } ;
        var textNoteType = new FilteredElementCollector( document ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( textNodeTypeName, tt.Name ) ) ;
        if ( textNoteType == null ) {
          var textNodeSize = model.CharacterHeight switch
          {
            1 => 1.0,
            2 => 1.2,
            3 => 1.5,
            4 => 1.8,
            _ => 2.0
          } ;
          var elementType = textNote.TextNoteType.Duplicate( textNodeTypeName ) ;
          textNoteType = elementType as TextNoteType ;
          textNoteType?.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
          textNoteType?.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;
          textNoteType?.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( textNodeSize.MillimetersToRevitUnits() ) ;
        }

        if ( textNoteType != null ) textNote.ChangeTypeId( textNoteType.Id ) ;

        textNote.SetOverriddenColor( SymbolColor.DictSymbolColor[ model.Color ] ) ;
        groupIds.Add( textNote.Id ) ;
      }

      document.Create.NewGroup( groupIds ) ;
    }
  }
}