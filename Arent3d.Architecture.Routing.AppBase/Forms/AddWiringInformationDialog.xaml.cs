﻿using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class AddWiringInformationDialog 
  {
    public AddWiringInformationDialog(AddWiringInformationViewModel viewModel )
    { 
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public abstract class DesignAddWiringInformationViewModel : AddWiringInformationViewModel
  {
    protected DesignAddWiringInformationViewModel( Document document, DetailTableModel detailTableModel, ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes ) : base( document, detailTableModel, conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes )
    {
    }
  }
}