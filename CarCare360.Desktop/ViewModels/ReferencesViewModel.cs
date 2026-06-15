using CarCare360.Desktop.Helpers;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel раздела «Справочники».
/// Хранит экземпляры sub-VM для каждой вкладки и команду переключения.
/// </summary>
public sealed class ReferencesViewModel : BaseViewModel
{
    private int _selectedTabIndex;

    public ReferencesViewModel()
    {
        Services        = new ServicesRefViewModel();
        Bays            = new BaysRefViewModel();
        Specializations = new SpecializationsRefViewModel();
        Brands          = new BrandsRefViewModel();

        // CommandParameter приходит как строка из XAML ("0"…"3")
        SetTabCommand = new RelayCommand<string>(s =>
        {
            if (int.TryParse(s, out var idx))
                SelectedTabIndex = idx;
        });
    }

    public ServicesRefViewModel        Services        { get; }
    public BaysRefViewModel            Bays            { get; }
    public SpecializationsRefViewModel Specializations { get; }
    public BrandsRefViewModel          Brands          { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public ICommand SetTabCommand { get; }
}
