using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ChangePullBoxDimensionCommandBase : IExternalCommand
  {
    private const string PullBoxNotFound = "No satisfied pull box dimension" ;
    private const string ChangePullBoxDimensionSuccesfully = "Change pull box dimension succesfully" ;
    private const string ChangePullBoxDimensionFailed = "Change pull box dimension failed" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      bool? changeResult = false ;
      try {
        var csvStorable = document.GetCsvStorable() ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;
        var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;

        var pullBoxElements = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
          .Where( e => e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() && e is FamilyInstance )
          .Where( e => !CheckDefaultLabelPullBox( document, pullBoxInfoStorable, e ) ).ToList() ;

        foreach ( var pullBoxElement in pullBoxElements ) {
          var pullBoxModel = PullBoxRouteManager.GetPullBoxWithAutoCalculatedDimension( document, pullBoxElement,
            csvStorable, detailSymbolStorable, conduitsModelData, hiroiMasterModels ) ;

          if ( pullBoxModel == null ) {
            MessageBox.Show( PullBoxNotFound ) ;
            return Result.Failed ;
          }

          var (depthPullBox, widthPullBox, heightPullBox) = PullBoxRouteManager.ParseKikaku( pullBoxModel.Kikaku ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Depth )?.Set( depthPullBox ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Width )?.Set( widthPullBox ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Height )?.Set( heightPullBox ) ;

          using var transaction = new Transaction( document ) ;
          transaction.Start( "Change pull box dimension" ) ;
          changeResult = pullBoxElement.ParametersMap.get_Item( PickUpViewModel.MaterialCodeParameter )
            ?.Set( pullBoxModel.Buzaicd ) ;
          detailSymbolStorable.DetailSymbolModelData.RemoveAll( d => d.DetailSymbolId == pullBoxElement.UniqueId) ;
          transaction.Commit() ;

          var textLabel = PullBoxRouteManager.GetPullBoxTextBox( depthPullBox, heightPullBox, PullBoxRouteManager.DefaultPullBoxLabel ) ;
          ChangeLabelOfPullBox( document, pullBoxInfoStorable, pullBoxElement, textLabel ) ;

          if ( ! ( changeResult ?? false ) )
            break ;
        }

        if ( changeResult ?? false ) {
          MessageBox.Show( ChangePullBoxDimensionSuccesfully ) ;
          return Result.Succeeded ;
        }

        MessageBox.Show( ChangePullBoxDimensionFailed ) ;
        return Result.Failed ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    public static void ChangeLabelOfPullBox( Document document, PullBoxInfoStorable pullBoxInfoStorable, Element pullBoxElement, string textLabel )
    {
      var pullBoxInfoModel = pullBoxInfoStorable.PullBoxInfoModelData.FirstOrDefault( p => p.PullBoxUniqueId == pullBoxElement.UniqueId ) ;
      var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel?.TextNoteUniqueId == t.UniqueId ) ;
      if ( textNote != null ) {
        using var transaction2 = new Transaction( document ) ;
        transaction2.Start( "Change pull box label" ) ;
        textNote.Text = textLabel ;
        transaction2.Commit() ;
      }
    }

    private bool CheckDefaultLabelPullBox( Document document, PullBoxInfoStorable pullBoxInfoStorable, Element pullBoxElement )
    {
      var pullBoxInfoModel = pullBoxInfoStorable.PullBoxInfoModelData.FirstOrDefault( p => p.PullBoxUniqueId == pullBoxElement.UniqueId ) ;
      if ( pullBoxInfoModel == null ) return false ;
      var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel.TextNoteUniqueId == t.UniqueId ) ;
      var textLabel = textNote?.Text ?? string.Empty ;
      return textLabel.Trim() == PullBoxRouteManager.DefaultPullBoxLabel ;
    }

    protected abstract AddInType GetAddInType() ;

    public class PullBoxDimensions
    {
      public const string Depth = "Depth" ;
      public const string Width = "Width" ;
      public const string Height = "Height" ;
    }
  }
}