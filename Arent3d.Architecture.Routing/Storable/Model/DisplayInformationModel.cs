using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    public class CeeDDetailInformationModel: INotifyPropertyChanged
    {
        public string SymbolImage { get; set; }
        public ObservableCollection<QueryData> QueryData{ get; set; }

        public CeeDDetailInformationModel(ObservableCollection<QueryData> queryData, string symbolImage, string setCodeFilter)
        {
            QueryData = queryData;
            SymbolImage = symbolImage;
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChange([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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