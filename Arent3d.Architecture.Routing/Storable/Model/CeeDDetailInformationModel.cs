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
        public string CeeDSetCode { get; set; }
        public string CeeDModelNumber { get; set; }
        public string ParentPartModelNumber { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Standard { get; set; }
        public string Quantity { get; set; }

        public QueryData(string ceeDSetCode, string ceeDModelNumber, string parentPartModelNumber,string productCode, string productName, string standard, string quantity)
        {
            CeeDSetCode = ceeDSetCode;
            CeeDModelNumber = ceeDModelNumber;
            ParentPartModelNumber = parentPartModelNumber;
            ProductCode = productCode;
            ProductName = productName;
            Standard = standard;
            Quantity = quantity;
        }
    }
}