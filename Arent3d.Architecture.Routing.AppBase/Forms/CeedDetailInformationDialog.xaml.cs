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

        public CeedDetailInformationDialog(Document document, string pickedText)
        {
            InitializeComponent();
            _setCode = pickedText;
            _listCeedModel = document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>();
            _hiroiSetMasterNormalModel = document.GetCsvStorable().HiroiSetMasterNormalModelData;
            _hiroiMasterModel = document.GetCsvStorable().HiroiMasterModelData;
            txtSetCode.Text = pickedText;
        }

        private ObservableCollection<QueryData> LoadQueryData(CeedModel ceedModel)
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            var hiroiSetMasterNormalList = _hiroiSetMasterNormalModel.Where(x => x.ParentPartModelNumber.Contains(ceedModel.CeeDModelNumber));
            foreach (var item in hiroiSetMasterNormalList)
            {
                if (!string.IsNullOrWhiteSpace(item.MaterialCode1))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode1);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode1, name, standard, item.Quantity1));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode2))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode2);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode2, name, standard, item.Quantity2));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode3))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode3);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode3, name, standard, item.Quantity3));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode4))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode4);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode4, name, standard, item.Quantity4));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode5))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode5);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode5, name, standard, item.Quantity5));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode6))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode6);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode6, name, standard, item.Quantity6));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode7))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode7);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode7, name, standard, item.Quantity7));
                }

                if (!string.IsNullOrWhiteSpace(item.MaterialCode8))
                {
                    var (name, standard) = GetNameAndStandardFromHiroiMaster(item.MaterialCode8);
                    queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode8, name, standard, item.Quantity8));
                }
            }

            return queryData;
        }

        private (string, string) GetNameAndStandardFromHiroiMaster(string str)
        {
            var item = _hiroiMasterModel.FirstOrDefault(x => x.Buzaicd == String.Format("{0:D6}", Convert.ToInt32(str)));

            return item != null ? (item.Hinmei.Trim(), item.Kikaku.Trim()) : (string.Empty, string.Empty);
        }

        private void LoadData()
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            if (!string.IsNullOrWhiteSpace(_setCode))
            {
                CeedModel? ceedModel = _listCeedModel.FirstOrDefault((model => model.CeeDSetCode.ToUpper().Equals(_setCode.ToUpper())));
                if (ceedModel != null)
                {
                    queryData = LoadQueryData(ceedModel);
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
