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
        private readonly List<HiroiSetMasterModel> _hiroiSetMasterEcoModel;
        private readonly ObservableCollection<QueryData> _queryDataMaster;
        private readonly List<CeedModel> _listCeedModel;
        private string _setCode;

        public CeedDetailInformationDialog(Document document, string pickedText)
        {
            InitializeComponent();
            _setCode = pickedText;
            _listCeedModel = document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>();
            _hiroiSetMasterEcoModel = document.GetCsvStorable().HiroiSetMasterEcoModelData;
            _queryDataMaster = LoadQueryData();
            txtSetCode.Text = pickedText;
        }

        private ObservableCollection<QueryData> LoadQueryData()
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            foreach (var ceedModel in _listCeedModel)
            {
                var listHiroiSet = _hiroiSetMasterEcoModel.Where(x => x.ParentPartModelNumber.Contains(ceedModel.CeeDModelNumber));
                foreach (var item in listHiroiSet)
                {
                    if (!string.IsNullOrWhiteSpace(item.MaterialCode1))
                    {
                        (string, string) split = SplitString(item.Name1);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode1, split.Item1, split.Item2, item.Quantity1));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode2))
                    {
                        (string, string) split = SplitString(item.Name2);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode2, split.Item1, split.Item2, item.Quantity2));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode3))
                    {
                        (string, string) split = SplitString(item.Name3);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode3, split.Item1, split.Item2, item.Quantity3));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode4))
                    {
                        (string, string) split = SplitString(item.Name4);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode4, split.Item1, split.Item2, item.Quantity4));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode5))
                    {
                        (string, string) split = SplitString(item.Name5);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode5, split.Item1, split.Item2, item.Quantity5));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode6))
                    {
                        (string, string) split = SplitString(item.Name6);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode6, split.Item1, split.Item2, item.Quantity6));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode7))
                    {
                        (string, string) split = SplitString(item.Name7);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode7, split.Item1, split.Item2, item.Quantity7));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode8))
                    {
                        (string, string) split = SplitString(item.Name8);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode8, split.Item1, split.Item2, item.Quantity8));
                    }
                }
            }

            return queryData;
        }

        private (string, string) SplitString(string str)
        {
            str = str.Replace("　", " ");
            string standard = "";
            string[] strArray = str.Trim().Split(' ');
            if (strArray.Length > 1)
            {
                standard = strArray[1].Trim();
            }

            return (strArray[0].Trim(), standard);
        }

        private void LoadData()
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            if (!string.IsNullOrWhiteSpace(_setCode))
            {
                queryData = new ObservableCollection<QueryData>(_queryDataMaster.Where(x => x.CeeDSetCode.IndexOf(_setCode, StringComparison.OrdinalIgnoreCase) > -1));
            }

            CeeDDetailInformationModel ceeDDetailInformationModels = new CeeDDetailInformationModel(queryData, "");
            CeeDDetailInformationViewModel viewModel = new CeeDDetailInformationViewModel(ceeDDetailInformationModels);
            DataContext = viewModel;
        }

        private void TxtSetCode_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _setCode = txtSetCode.Text;
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
