using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Excel = Microsoft.Office.Interop.Excel ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "998cd31f-ada6-4fbf-ae78-971fc071c030" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedStorable : StorableBase
  {
    public const string StorableName = "CeeD Model" ;
    private const string CeeDModelField = "CeeDModel" ;
    private const string CeeDFileName = "CeeDModelList" ;

    public Dictionary<string, CeedModel> CeedModelData { get ; private set ; }

    public CeedStorable( DataStorage owner ) : base( owner, false )
    {
      CeedModelData = GetAllCeeDModelNumber( CeeDFileName ) ;
    }

    public CeedStorable( Document document ) : base( document, false )
    {
      CeedModelData = GetAllCeeDModelNumber( CeeDFileName ) ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      CeedModelData = reader.GetArray<CeedModel>( CeeDModelField ).ToDictionary( x => x.CeeDModelNumber ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CeeDModelField, CeedModelData.Values.ToList() ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedModel>( CeeDModelField ) ;
    }

    public override string Name => StorableName ;

    public static Dictionary<string, CeedModel> GetAllCeeDModelNumber( string ceedFileName )
    {
      Dictionary<string, CeedModel> ceedModelData = new Dictionary<string, CeedModel>() ;
      string generalDisplayDeviceSymbols = string.Empty ;
      string floorPlanSymbol = string.Empty ;
      var path = AssetManager.GetCeeDModelPath( ceedFileName ) ;

      Excel.ApplicationClass app = new Excel.ApplicationClass() ;
      // Create the workbook object by opening the excel file.
      Excel.Workbook workBook = app.Workbooks.Open( path, 0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0 ) ;
      // Get the active worksheet using sheet name or active sheet
      Excel.Worksheet workSheet = (Excel.Worksheet) workBook.Sheets[ 2 ] ;
      const int row = 200 ;
      int firstIndexGroup = 8 ;
      int lastIndexGroup = 8 ;
      try {
        for ( int i = 8 ; i <= row ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;

          var modelNumber = ( (Excel.Range) workSheet.Cells[ i, 5 ] ).Value2 ;
          if ( modelNumber != null ) {
            firstIndexGroup = i ;
            var nextModelNumber = ( (Excel.Range) workSheet.Cells[ i, 5 ] ).Value2 ;
            do {
              if ( modelNumber != null ) {
                modelNumbers.Add( modelNumber.ToString() ) ;
              }

              i++ ;
              modelNumber = nextModelNumber ;
              nextModelNumber = ( (Excel.Range) workSheet.Cells[ i, 5 ] ).Value2 ;
            } while ( ! ( modelNumber == null && nextModelNumber != null ) ) ;

            lastIndexGroup = i ;
            for ( int j = firstIndexGroup ; j < lastIndexGroup ; j++ ) {
              var generalDisplayDeviceSymbol = ( (Excel.Range) workSheet.Cells[ j, 3 ] ).Value2 ;
              if ( generalDisplayDeviceSymbol != null ) generalDisplayDeviceSymbols = generalDisplayDeviceSymbol.ToString() ;
              var symbol = ( (Excel.Range) workSheet.Cells[ j, 6 ] ).Value2 ;
              if ( symbol != null ) floorPlanSymbol = symbol.ToString() ;

              var ceeDModelNumber = ( (Excel.Range) workSheet.Cells[ j, 2 ] ).Value2 ;
              if ( ceeDModelNumber != null ) ceeDModelNumbers.Add( ceeDModelNumber.ToString() ) ;
              var ceeDSetCode = ( (Excel.Range) workSheet.Cells[ j, 1 ] ).Value2 ;
              if ( ceeDSetCode != null ) ceeDSetCodes.Add( ceeDSetCode.ToString() ) ;
            }

            if ( ! ceeDModelNumbers.Any() ) {
              CeedModel ceedModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols, string.Join( "\n", modelNumbers ), floorPlanSymbol ) ;
              ceedModelData.Add( "Empty" + firstIndexGroup, ceedModel ) ;
            }
            else {
              for ( int k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
                var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
                CeedModel ceedModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, string.Join( "\n", modelNumbers ), floorPlanSymbol ) ;
                ceedModelData.Add( ceeDModelNumbers[ k ], ceedModel ) ;
              }
            }
          }

          i-- ;
        }
      }
      catch ( Exception ) {
        app.Quit() ;
      }

      return ceedModelData ;
    }
  }
}