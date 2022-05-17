using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Color = Autodesk.Revit.DB.Color ;
using FormattedText = Autodesk.Revit.DB.FormattedText ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class SymbolInformationCommandBase : IExternalCommand
  {
    /// <summary>
    /// Get selected objects, check if the first is SymbolInformation or not.
    /// 1. If that one is SymbolInformation => show dialog 
    /// 2. If that one isn't SymbolInformation => show message
    /// If there isn't selected => create new base on click location => show dialog 
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var symbolInformationStorable = document.GetSymbolInformationStorable() ;
        var symbolInformations = symbolInformationStorable.AllSymbolInformationModelData ;
        var level = uiDocument.ActiveView.GenLevel ;
        var heightOfSymbol = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        SymbolInformationModel? model = null ;
        FamilyInstance? symbolInformationInstance = null ;
        var xyz = XYZ.Zero ;

        return document.Transaction( "Electrical.App.Commands.Routing.SymbolInformationCommand", _ =>
        {
          var selectedItemIsSymbolInformation = false ;
          TextNote? textNote = null ;
          Group? oldParentGroup = null ;
          if ( uiDocument.Selection.GetElementIds().Count > 0 ) {
            var groupId = uiDocument.Selection.GetElementIds().First() ;
            if ( document.GetElement( groupId ) is Group parentGroup ) {
              oldParentGroup = parentGroup ;
              var elementId = GetElementIdOfSymbolInformationFromGroup( document, symbolInformations, parentGroup, ref textNote ) ;
              if ( elementId != null ) {
                var symbolInformation = symbolInformations.FirstOrDefault( x => x.Id == elementId.ToString() ) ;
                //pickedObject is SymbolInformationModel
                if ( null != symbolInformation ) {
                  model = symbolInformation ;
                  var symbolInformationSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ) ?? throw new InvalidOperationException() ;
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
                  symbolInformations.Add( model ) ;
                  selectedItemIsSymbolInformation = true ;
                }
              }
            }
          }

          if ( selectedItemIsSymbolInformation == false ) {
            try {
              xyz = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
              symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfSymbol ) ) ;
              model = new SymbolInformationModel { Id = symbolInformationInstance.Id.ToString() } ;
              symbolInformations.Add( model ) ;
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
              return Result.Cancelled ;
            }
          }

          var viewModel = new SymbolInformationViewModel( document, model ) ;
          var dialog = new SymbolInformationDialog( viewModel ) ;
          var ceedDetailStorable = document.GetCeedDetailStorable() ;

          if ( dialog.ShowDialog() == true && model != null ) {
            //*****Save symbol setting***********
            var symbolHeightParameter = symbolInformationInstance?.LookupParameter( "Symbol Height" ) ;
            symbolHeightParameter?.Set( model.Height.MillimetersToRevitUnits() ) ;
            symbolInformationStorable.Save() ;

            //****Save ceedDetails******
            //Delete old data
            ceedDetailStorable.AllCeedDetailModelData.RemoveAll( x => x.ParentId == model.Id ) ;
            //Add new data
            ceedDetailStorable.AllCeedDetailModelData.AddRange( viewModel.CeedDetailList ) ;
            ceedDetailStorable.Save() ;

            //Create group symbol information 
            if ( oldParentGroup != null ) {
              oldParentGroup.UngroupMembers() ;
              if ( textNote != null )
                document.Delete( textNote.Id ) ;
            }


            CreateGroupSymbolInformation( document, symbolInformationInstance!.Id, model, new XYZ( xyz.X, xyz.Y, heightOfSymbol ), oldParentGroup ) ;
            OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
            ogs.SetProjectionLineColor( SymbolColor.DictSymbolColor[ model.Color ] ) ;
            ogs.SetProjectionLineWeight( 5 ) ;
            document.ActiveView.SetElementOverrides( symbolInformationInstance!.Id, ogs ) ;
          }
          else if ( selectedItemIsSymbolInformation == false ) {
            document.Delete( symbolInformationInstance?.Id ) ;
          }

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Cancelled ;
      }
    }

    /// <summary>
    /// Get ElementId of SymbolInformation from selected group 
    /// </summary>
    /// <param name="document"></param>
    /// <param name="symbolInformations"></param>
    /// <param name="group"></param>
    /// <param name="textNote"></param>
    /// <returns></returns>
    private ElementId? GetElementIdOfSymbolInformationFromGroup( Document document, List<SymbolInformationModel> symbolInformations, Group group, ref TextNote? textNote )
    {
      var memberIds = group.GetMemberIds() ;
      foreach ( var memberId in memberIds ) {
        if ( symbolInformations.FirstOrDefault( x => x.Id == memberId.ToString() ) != null ) {
          var txtGroup = document.GetAllElements<Group>().FirstOrDefault( x => x.AttachedParentId == group.Id ) ;
          if ( txtGroup != null ) {
            var txtId = txtGroup.GetMemberIds().FirstOrDefault() ;
            var txtNode = document.GetElement( txtId ) ;
            if ( txtNode != null ) {
              textNote = (TextNote) txtNode ;
            }
          }

          return memberId ;
        }
      }

      return null ;
    }

    /// <summary>
    /// Create FamilySymbol type ElectricalRoutingFamilyType.SymbolStar
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="level"></param>
    /// <param name="xyz"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private FamilyInstance GenerateSymbolInformation( UIDocument uiDocument, Level level, XYZ xyz )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
    }

    /// <summary>
    /// Create group of symbol and text annotation
    /// </summary>
    /// <param name="document"></param>
    /// <param name="symbolInformationInstanceId"></param>
    /// <param name="model"></param>
    /// <param name="xyz"></param>
    /// <param name="oldParentGroup"></param>
    private void CreateGroupSymbolInformation( Document document, ElementId symbolInformationInstanceId, SymbolInformationModel model, XYZ xyz, Group? oldParentGroup )
    {
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      groupIds.Add( symbolInformationInstanceId ) ;

      if ( model.IsShowText && ! string.IsNullOrEmpty( model.Description ) ) {
        var noteWidth = .05 ;
        var anchor = (SymbolCoordinateEnum) Enum.Parse( typeof( SymbolCoordinateEnum ), model.SymbolCoordinate! ) ;
        XYZ txtPosition ;
        switch ( anchor ) {
          case SymbolCoordinateEnum.Top :
            txtPosition = new XYZ( xyz.X - 1, xyz.Y + 1, xyz.Z ) ;
            break ;
          case SymbolCoordinateEnum.Left :
            txtPosition = new XYZ( xyz.X - model.Height, xyz.Y + 0.1, xyz.Z ) ;
            break ;
          case SymbolCoordinateEnum.Right :
            txtPosition = new XYZ( xyz.X + model.Height / 3, xyz.Y + 0.1, xyz.Z ) ;
            break ;
          default :
            txtPosition = new XYZ( xyz.X - 1, xyz.Y - 1, xyz.Z ) ;
            break ;
        }


        ElementId defaultTextTypeId = document.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;

        // make sure note width works for the text type
        var minWidth = TextElement.GetMinimumAllowedWidth( document, defaultTextTypeId ) ;
        var maxWidth = TextElement.GetMaximumAllowedWidth( document, defaultTextTypeId ) ;
        noteWidth = noteWidth < minWidth ? minWidth : ( noteWidth > maxWidth ? maxWidth : noteWidth ) ;

        TextNoteOptions opts = new(defaultTextTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left, VerticalAlignment = VerticalTextAlignment.Middle, KeepRotatedTextReadable = true } ;

        var textNote = TextNote.Create( document, document.ActiveView.Id, txtPosition, noteWidth, model.Description, opts ) ;
        textNote.SetOverriddenColor( SymbolColor.DictSymbolColor[ model.Color ] ) ;
        groupIds.Add( textNote.Id ) ;
      }

      document.Create.NewGroup( groupIds ) ;
    }
  }
}