using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class SymbolContentTagCommandParameter
  {
    public Element Element { get ; }
    public XYZ Point { get ; }

    public SymbolContentTagCommandParameter( Element element, XYZ point )
    {
      Element = element ;
      Point = point ;
    }
  }
  
  public class CreateSymbolContentTagCommandBase : RoutingExternalAppCommandBaseWithParam<SymbolContentTagCommandParameter>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.CreateSymbolContentTagCommand".GetAppStringByKeyOrDefault( "Create connector" ) ;

    protected override ExecutionResult Execute( SymbolContentTagCommandParameter param, Document document, TransactionWrapper transaction )
    {
      var symbolContentTag = param.Element.Category.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures ? ElectricalRoutingFamilyType.SymbolContentTag : ElectricalRoutingFamilyType.SymbolContentEquipmentTag ;
      var deviceSymbolTagType = document.GetFamilySymbols( symbolContentTag ).FirstOrDefault( x => x.LookupParameter( "Is Hide Quantity" ).AsInteger() == 1 ) ;
      if ( deviceSymbolTagType == null ) return ExecutionResult.Succeeded ;
      IndependentTag.Create( document, deviceSymbolTagType.Id, document.ActiveView.Id, new Reference( param.Element ), false, TagOrientation.Horizontal, new XYZ( param.Point.X, param.Point.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * document.ActiveView.Scale, param.Point.Z ) ) ;
      
      var connectorUpdater = new ConnectorUpdater( document.Application.ActiveAddInId ) ;
      if ( UpdaterRegistry.IsUpdaterRegistered( connectorUpdater.GetUpdaterId() ) ) return ExecutionResult.Succeeded ;
      UpdaterRegistry.RegisterUpdater( connectorUpdater, document ) ;
      var multiCategoryFilter = new ElementMulticategoryFilter( BuiltInCategorySets.OtherElectricalElements ) ;
      UpdaterRegistry.AddTrigger( connectorUpdater.GetUpdaterId(), document, multiCategoryFilter, Element.GetChangeTypeAny() ) ;

      return ExecutionResult.Succeeded ;
    }
  }
}