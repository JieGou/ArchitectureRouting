using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Group = Autodesk.Revit.DB.Group ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ChangePullBoxDimensionCommandBase : IExternalCommand
  {
    private const string HinmeiPullBox = "プルボックス" ;
    private const string DefaultPullBoxLabel = "PB" ;
    private const string TaniPullBox = "個" ;
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
        
        // var pullBoxPickerInfo = PullBoxPicker.PickPullBox( uiDocument, "Pick pull box", GetAddInType() ) ;
        var pullBoxElements = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
          .Where( e => e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() && e is FamilyInstance ).ToList() ;
        foreach ( var pullBoxElement in pullBoxElements ) {
          var conduitsFromPullBox = PullBoxRouteManager.GetFromConnectorOfPullBox( document, pullBoxElement, true ) ;
          var conduitsToPullBox = PullBoxRouteManager.GetFromConnectorOfPullBox( document, pullBoxElement ) ;
          var directionFrom = PullBoxRouteManager.GetDirectionOfConduit( pullBoxElement, conduitsFromPullBox ) ;
          var directionTo = PullBoxRouteManager.GetDirectionOfConduit( pullBoxElement, conduitsToPullBox ) ;
          
          var isStraightDirection = directionFrom != null && directionTo != null && PullBoxRouteManager.IsStraightDirection( directionFrom!, directionTo! ) ;
          
          conduitsFromPullBox = conduitsFromPullBox.Where( c => c is Conduit ).ToList() ;
          var groupConduits =
            conduitsFromPullBox.GroupBy( c => c.GetRepresentativeRouteName() ).Select( c => c.First() ) ;
          foreach ( var conduit in groupConduits )
            AddWiringInformationCommandBase.CreateDetailSymbolModel( document, conduit, csvStorable, detailSymbolStorable, pullBoxElement.UniqueId ) ;
          
          var elementIds = conduitsFromPullBox.Select( c => c.UniqueId ).ToList() ;
          var (detailTableModels, _, _) = CreateDetailTableCommandBase.CreateDetailTableAddWiringInfo( document,
            csvStorable, detailSymbolStorable, conduitsFromPullBox, elementIds, false ) ;
          
          var newDetailTableModels = DetailTableViewModel.SummarizePlumbing( detailTableModels, conduitsModelData,
            detailSymbolStorable, new List<DetailTableModel>(), false, new Dictionary<string, string>() ) ;
          
          var plumbingSizes = newDetailTableModels.Where( p => int.TryParse( p.PlumbingSize, out _ ) )
            .Select( p => Convert.ToInt32( p.PlumbingSize ) ).ToArray() ;
          var (depth, width, height) = PullBoxRouteManager.CalculatePullBoxDimension( plumbingSizes, isStraightDirection ) ;
          
          var minPullBoxModelDepth = hiroiMasterModels
            .Where( p => p.Tani == TaniPullBox && p.Hinmei.Contains( HinmeiPullBox ) ).Where( p =>
            {
              var (d, w, h) = ParseKikaku( p.Kikaku ) ;
              return d >= depth && w >= width && h >= height ;
            } ).Min( x =>
            {
              var (d, w, _) = ParseKikaku( x.Kikaku ) ;
              return d ;
            } ) ;
          
          var pullBoxModel = hiroiMasterModels.FirstOrDefault( p =>
          {
            var (d, w, h) = ParseKikaku( p.Kikaku ) ;
            return h == height && d == minPullBoxModelDepth ;
          } ) ;
          
          if ( pullBoxModel == null ) {
            MessageBox.Show( PullBoxNotFound ) ;
            return Result.Failed ;
          }

          var (depthPullBox, widthPullBox, heightPullBox) = ParseKikaku( pullBoxModel.Kikaku ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Depth )?.Set( depthPullBox ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Width )?.Set( widthPullBox ) ;
          // pullBoxElement.ParametersMap.get_Item( PullBoxDimensions.Height )?.Set( heightPullBox ) ;
          using var transaction = new Transaction( document ) ;
          transaction.Start( "Change pull box dimension" ) ;
          changeResult = pullBoxElement.ParametersMap.get_Item( PickUpViewModel.MaterialCodeParameter )?.Set( pullBoxModel.Buzaicd ) ;
          detailSymbolStorable.DetailSymbolModelData.RemoveAll( _ => true) ;
          transaction.Commit() ;
          using var transaction2 = new Transaction( document ) ;
          transaction2.Start( "Change pull box label" ) ;
          Dictionary<ElementId, List<ElementId>> pullBoxGroup = new Dictionary<ElementId, List<ElementId>>() ;
          ElectricalCommandUtil.UnGroupConnector( document, pullBoxElement, ref pullBoxGroup ) ;
          if ( pullBoxGroup.Count > 0 ) {
            var listTextNoteIds = pullBoxGroup[ pullBoxElement.Id ] ;
            TextNote? textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => listTextNoteIds.Contains( t.Id ) ) ;
            if ( textNote != null ) { 
              textNote.Text = PullBoxRouteManager.GetPullBoxTextBox( depthPullBox, widthPullBox, DefaultPullBoxLabel ) ;
            }
          }
          transaction2.Commit() ;
          if(pullBoxGroup.Count > 0)
            ElectricalCommandUtil.GroupConnector( document, pullBoxGroup ) ;

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

    protected abstract AddInType GetAddInType() ;

    private (int depth, int width, int height) ParseKikaku( string kikaku )
    {
      var kikakuRegex = new Regex( "(?!\\d)*(?<kikaku>((\\d+(x)){2}(\\d+)))(?!\\d)*" ) ;
      var m = kikakuRegex.Match( kikaku ) ;
      if ( m.Success ) {
        var strKikaku = m.Groups[ "kikaku" ].Value.Split( 'x' ) ;
        if ( strKikaku.Length == 3 ) {
          var depth = Convert.ToInt32( strKikaku[ 0 ] ) ;
          var width = Convert.ToInt32( strKikaku[ 1 ] ) ;
          var height = Convert.ToInt32( strKikaku[ 2 ] ) ;
          return ( depth, width, height ) ;
        }
      }

      return ( 0, 0, 0 ) ;
    }

    public class PullBoxDimensions
    {
      public const string Depth = "Depth" ;
      public const string Width = "Width" ;
      public const string Height = "Height" ;
    }
  }
}