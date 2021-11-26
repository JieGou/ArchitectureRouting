using System.Collections.ObjectModel;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    public class CeeDDetailInformationModel
    {
        public string SymbolImage { get; set; }
        public ObservableCollection<QueryData> QueryData{ get; set; }

        public CeeDDetailInformationModel(ObservableCollection<QueryData> queryData, string symbolImage)
        {
            QueryData = queryData;
            SymbolImage = symbolImage;
        }
    }

    public class QueryData
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Standard { get; set; }
        public string Quantity { get; set; }

        public QueryData(string productCode, string productName, string standard, string quantity)
        {
            ProductCode = productCode;
            ProductName = productName;
            Standard = standard;
            Quantity = quantity;
        }
    }
}