using System.Linq ;
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
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.CreateSymbolContentTagCommand".GetAppStringByKeyOrDefault( "Create Symbol Content Tag Command" ) ;

    protected override ExecutionResult Execute( SymbolContentTagCommandParameter param, Document document, TransactionWrapper transaction )
    {
      var deviceSymbolTagType = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolContentTag ).FirstOrDefault() ;
      if ( deviceSymbolTagType == null ) return ExecutionResult.Succeeded ;
      IndependentTag.Create( document, deviceSymbolTagType.Id, document.ActiveView.Id, new Reference( param.Element ), false, TagOrientation.Horizontal, new XYZ( param.Point.X, param.Point.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * document.ActiveView.Scale, param.Point.Z ) ) ;
      return ExecutionResult.Succeeded ;
    }
  }
}