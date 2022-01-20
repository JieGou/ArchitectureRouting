using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowElectricSymbolsCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var pickedObjects = uiDoc.Selection
        .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
        .Where( p => p is Conduit ) ;
      var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
        var level = uiDoc.ActiveView.GenLevel ;
        var heightOfConnector =
          document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

        ElementId defaultTextTypeId = document.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
        var noteWidth = 0.4 ;
        TextNoteOptions opts = new(defaultTextTypeId) ;
        var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
        return document.Transaction(
          "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault( "Set conduit information" ),
          _ =>
          {
            TextNote.Create( document, document.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable(), opts ) ;
            return Result.Succeeded ;
          } ) ;
       
    }
    
    private string GenerateTextTable()
    {
      string line =new string( '＿', 32 );
      string result = string.Empty ;
      result += $"{line}\r│abcd" ;
      for ( int i = 0 ; i < 10 ; i++ ) {
        result += $"\r{line}" ;
      }

      result += $"\r{line}" ;
      return result ;
    }

    private string CheckEmptyString( string str, int lenght )
    {
      return ! string.IsNullOrEmpty( str ) ? $"({str})" : new string( '　', lenght ) ;
    }

    private string AddFullString( string str, int length )
    {
      if ( str.Length < length ) {
        str += new string( '　', length - str.Length ) ;
      }

      return str ;
    }
  }
}