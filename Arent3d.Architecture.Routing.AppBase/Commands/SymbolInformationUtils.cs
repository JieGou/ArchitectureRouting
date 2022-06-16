using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public static class SymbolInformationUtils
  {
    private static double StringToDouble( string value )
    {
      double.TryParse( value, out var result ) ;
      return result ;
    }

    private static CeedDetailModel? GetElectricalWireInfo( IEnumerable<CeedDetailModel> ceedDetailList, CeedDetailModel conduit )
    {
      return ceedDetailList.FirstOrDefault( x => ! string.IsNullOrEmpty( x.CeedCode ) && x.CeedCode == conduit.CeedCode && x.AllowInputQuantity && ! x.IsConduit ) ;
    }

    private static CeedDetailModel? GetConduitInfo( IEnumerable<CeedDetailModel> ceedDetailList, CeedDetailModel electricalWireInfo )
    {
      return ceedDetailList.FirstOrDefault( x => ! string.IsNullOrEmpty( x.CeedCode ) && x.CeedCode == electricalWireInfo.CeedCode && x.IsConduit ) ;
    }

    public static void UpdateQuantity( ObservableCollectionEx<CeedDetailModel> ceedDetailList, CeedDetailModel itemChanged, CeedDetailModel conduit )
    {
      var electricalWireInfo = GetElectricalWireInfo( ceedDetailList, itemChanged ) ;
      if ( null == electricalWireInfo ) return ;

      if ( conduit.Classification == ClassificationType.露出.GetFieldName() ) {
        conduit.QuantityCalculate = StringToDouble( electricalWireInfo.Quantity ) ;
        electricalWireInfo.QuantityCalculate = 0 ;
      }

      if ( conduit.Classification != ClassificationType.隠蔽.GetFieldName() ) return ;
      electricalWireInfo.QuantityCalculate = StringToDouble( conduit.Quantity ) ;
      conduit.QuantityCalculate = 0 ;
    }

    public static void ChangeQuantityInfo( IEnumerable<CeedDetailModel> ceedDetailList, CeedDetailModel itemChanged )
    {
      var itemCvv = itemChanged.Classification == ClassificationType.隠蔽.GetFieldName() ? GetElectricalWireInfo( ceedDetailList, itemChanged ) : GetConduitInfo( ceedDetailList, itemChanged ) ;
      if ( null == itemCvv ) return ;

      if ( itemChanged.Classification == ClassificationType.隠蔽.GetFieldName() ) {
        itemCvv.QuantityCalculate = StringToDouble( itemChanged.Quantity ) ;
      }

      if ( itemCvv.Classification == ClassificationType.露出.GetFieldName() && itemChanged.Classification != ClassificationType.露出.GetFieldName() ) {
        itemCvv.QuantityCalculate = StringToDouble( itemChanged.Quantity ) ;
      }
    }
  }
}