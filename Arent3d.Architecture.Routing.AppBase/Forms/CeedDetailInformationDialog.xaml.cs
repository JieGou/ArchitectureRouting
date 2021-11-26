using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public partial class CeedDetailInformationDialog : Window
    {
        private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModel;
        private readonly List<HiroiMasterModel> _hiroiMasterModel;
        private readonly List<CeedModel> _listCeedModel;
        private string _setCode;
        private string _productCodeFormat = "{0:D6}";

        public CeedDetailInformationDialog(Document document, string pickedText)
        {
            InitializeComponent();
            _setCode = pickedText;
            _listCeedModel = document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>();
            _hiroiSetMasterNormalModel = document.GetCsvStorable().HiroiSetMasterNormalModelData;
            _hiroiMasterModel = document.GetCsvStorable().HiroiMasterModelData;
            txtSetCode.Text = _setCode;
        }

        private void BuildQueryData(string materialCode, string quantity, ref ObservableCollection<QueryData> queryData)
        {
            if (!string.IsNullOrWhiteSpace(materialCode))
            {
                materialCode = String.Format(_productCodeFormat, Convert.ToInt32(materialCode));
                var hiroiMasterItem = _hiroiMasterModel.FirstOrDefault(x => x.Buzaicd == materialCode);
                var name = hiroiMasterItem != null ? hiroiMasterItem.Hinmei.Trim() : string.Empty;
                var standard = hiroiMasterItem != null ? hiroiMasterItem.Kikaku.Trim() : string.Empty;
                queryData.Add(new QueryData(materialCode, name, standard, quantity));
            }
        }

        private void LoadData()
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            if (!string.IsNullOrWhiteSpace(_setCode))
            {
                CeedModel? ceedModel = _listCeedModel.FirstOrDefault((model => model.CeeDSetCode.ToUpper().Equals(_setCode.ToUpper())));
                if (ceedModel != null)
                {
                    var hiroiSetMasterNormalList = _hiroiSetMasterNormalModel.Where(x => x.ParentPartModelNumber.Contains(ceedModel.CeeDModelNumber));
                    foreach (var item in hiroiSetMasterNormalList)
                    {
                        BuildQueryData(item.MaterialCode1, item.Quantity1, ref queryData);
                        BuildQueryData(item.MaterialCode2, item.Quantity2, ref queryData);
                        BuildQueryData(item.MaterialCode3, item.Quantity3, ref queryData);
                        BuildQueryData(item.MaterialCode4, item.Quantity4, ref queryData);
                        BuildQueryData(item.MaterialCode5, item.Quantity5, ref queryData);
                        BuildQueryData(item.MaterialCode6, item.Quantity6, ref queryData);
                        BuildQueryData(item.MaterialCode7, item.Quantity7, ref queryData);
                        BuildQueryData(item.MaterialCode8, item.Quantity8, ref queryData);
                    }
                }
            }

            CeeDDetailInformationModel ceeDDetailInformationModels = new CeeDDetailInformationModel(queryData, "");
            CeeDDetailInformationViewModel viewModel = new CeeDDetailInformationViewModel(ceeDDetailInformationModels);
            DataContext = viewModel;
        }

        private void TxtSetCode_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _setCode = txtSetCode.Text.Trim();
            LoadData();
        }

        private void BtnOK_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnReset_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
