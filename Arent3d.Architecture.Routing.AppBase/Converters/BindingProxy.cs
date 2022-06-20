﻿using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Converters
{
  public class BindingProxy : Freezable
  {
    #region Overrides of Freezable

    protected override Freezable CreateInstanceCore()
    {
      return new BindingProxy() ;
    }

    #endregion

    public object Data
    {
      get => GetValue( DataProperty ) ;
      set => SetValue( DataProperty, value ) ;
    }

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register( "Data", typeof( object ), typeof( BindingProxy ) ) ;
  }
}