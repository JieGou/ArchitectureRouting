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
        private static char[] _whiteSpaces = new char[] {' ', '　'};
        private static char[] _numbers = new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '０', '１', '２', '３', '４', '５', '６', '７', '８', '９'};

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
                        var (name, standard) = GetNameAndStandard(item.Name1);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode1, name, standard, item.Quantity1));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode2))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name2);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode2, name, standard, item.Quantity2));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode3))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name3);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode3, name, standard, item.Quantity3));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode4))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name4);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode4, name, standard, item.Quantity4));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode5))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name5);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode5, name, standard, item.Quantity5));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode6))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name6);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode6, name, standard, item.Quantity6));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode7))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name7);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode7, name, standard, item.Quantity7));
                    }

                    if (!string.IsNullOrWhiteSpace(item.MaterialCode8))
                    {
                        var (name, standard) = GetNameAndStandard(item.Name8);
                        queryData.Add(new QueryData(ceedModel.CeeDSetCode, ceedModel.CeeDModelNumber, item.ParentPartModelNumber, item.MaterialCode8, name, standard, item.Quantity8));
                    }
                }
            }

            return queryData;
        }

        private static (string, string) GetNameAndStandard(string str)
        {
            str = str.Trim();
            string name, standard = "";
            int index;

            if ((index = str.LastIndexOfAny(_whiteSpaces)) <= 0)
            {
                index = str.IndexOfAny(_numbers);
            }

            if (index <= 0)
            {
                name = str;
            }
            else
            {
                name = str.Substring(0, index);
                standard = str.Substring(index, str.Length - index);
            }

            return (name.Trim(), standard.Trim());
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
