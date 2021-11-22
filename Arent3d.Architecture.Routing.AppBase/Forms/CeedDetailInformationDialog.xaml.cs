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
                        string itemStandard1 = "";
                        item.Name1 = item.Name1.Replace("　", " ");
                        string[] split1 = item.Name1.Trim().Split(' ');
                        if (split1.Length > 1)
                        {
                            itemStandard1 = split1[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode1, split1[0], itemStandard1, item.Quantity1));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode2))
                    {
                        string itemStandard2 = "";
                        item.Name2 = item.Name2.Replace("　", " ");
                        string[] split2 = item.Name2.Trim().Split(' ');
                        if (split2.Length > 1)
                        {
                            itemStandard2 = split2[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode2, split2[0], itemStandard2, item.Quantity2));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode3))
                    {
                        string itemStandard3 = "";
                        item.Name3 = item.Name3.Replace("　", " ");
                        string[] split3 = item.Name3.Trim().Split(' ');
                        if (split3.Length > 1)
                        {
                            itemStandard3 = split3[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode3, split3[0], itemStandard3, item.Quantity3));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode4))
                    {
                        string itemStandard4 = "";
                        item.Name4 = item.Name4.Replace("　", " ");
                        string[] split4 = item.Name4.Trim().Split(' ');
                        if (split4.Length > 1)
                        {
                            itemStandard4 = split4[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode4, split4[0], itemStandard4, item.Quantity4));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode5))
                    {
                        string itemStandard5 = "";
                        item.Name5 = item.Name5.Replace("　", " ");
                        string[] split5 = item.Name5.Trim().Split(' ');
                        if (split5.Length > 1)
                        {
                            itemStandard5 = split5[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode5, split5[0], itemStandard5, item.Quantity5));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode6))
                    {
                        string itemStandard6 = "";
                        item.Name6 = item.Name6.Replace("　", " ");
                        string[] split6 = item.Name6.Trim().Split(' ');
                        if (split6.Length > 1)
                        {
                            itemStandard6 = split6[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode6, split6[0], itemStandard6, item.Quantity6));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode7))
                    {
                        string itemStandard7 = "";
                        item.Name7 = item.Name7.Replace("　", " ");
                        string[] split7 = item.Name7.Trim().Split(' ');
                        if (split7.Length > 1)
                        {
                            itemStandard7 = split7[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode7, split7[0], itemStandard7, item.Quantity7));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode8))
                    {
                        string itemStandard8 = "";
                        item.Name8 = item.Name8.Replace("　", " ");
                        string[] split8 = item.Name8.Trim().Split(' ');
                        if (split8.Length > 1)
                        {
                            itemStandard8 = split8[1].Trim();
                        }

                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode8, split8[0], itemStandard8, item.Quantity8));
                    }
                }
            }

            return queryData;
        }

        private void LoadData()
        {
            ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
            if (!string.IsNullOrWhiteSpace(_setCode))
            {
                queryData = new ObservableCollection<QueryData>(_queryDataMaster.Where(x => x.CeeDSetCode.IndexOf(_setCode, StringComparison.OrdinalIgnoreCase) > -1));
            }

            CeeDDetailInformationModel ceeDDetailInformationModels = new CeeDDetailInformationModel(queryData, "", _setCode);
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
