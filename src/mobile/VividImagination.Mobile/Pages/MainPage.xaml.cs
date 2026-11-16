using VividImagination.Mobile.Models;
using VividImagination.Mobile.PageModels;

namespace VividImagination.Mobile.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}