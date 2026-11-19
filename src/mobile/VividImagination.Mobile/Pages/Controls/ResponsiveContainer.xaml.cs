using Microsoft.Maui.Controls.Shapes;

namespace VividImagination.Mobile.Pages.Controls;

[ContentProperty(nameof(BodyContent))]
public partial class ResponsiveContainer : ContentView
{
    public static readonly BindableProperty BodyContentProperty =
        BindableProperty.Create(
            nameof(BodyContent),
            typeof(View),
            typeof(ResponsiveContainer),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is ResponsiveContainer container && container.PageBodyPresenter != null)
                {
                    container.PageBodyPresenter.Content = newValue as View;
                }
            });

    public View? BodyContent
    {
        get => (View?)GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(
            nameof(ActiveTab),
            typeof(string),
            typeof(ResponsiveContainer),
            "Feed",
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is ResponsiveContainer rc && rc.BottomNavControl != null)
                {
                    rc.BottomNavControl.ActiveTab = (string)newValue;
                }
            });

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public static readonly BindableProperty ShowBottomNavProperty =
        BindableProperty.Create(
            nameof(ShowBottomNav),
            typeof(bool),
            typeof(ResponsiveContainer),
            true,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is ResponsiveContainer rc && rc.BottomNavControl != null)
                {
                    rc.BottomNavControl.IsVisible = (bool)newValue;
                }
            });

    public bool ShowBottomNav
    {
        get => (bool)GetValue(ShowBottomNavProperty);
        set => SetValue(ShowBottomNavProperty, value);
    }

    public ResponsiveContainer()
    {
        InitializeComponent();

        ApplyLayoutMetrics();

        if (PageBodyPresenter != null && BodyContent != null)
        {
            PageBodyPresenter.Content = BodyContent;
        }

        if (BottomNavControl != null)
        {
            BottomNavControl.ActiveTab = ActiveTab;
            BottomNavControl.IsVisible = ShowBottomNav;
        }

        SizeChanged += OnContainerSizeChanged;
    }

    private void ApplyLayoutMetrics()
    {
        if (ColumnFrame == null) return;

        bool isMobile = DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS;

        if (isMobile || Width < 500)
        {
            ColumnFrame.WidthRequest = -1;
            ColumnFrame.MaximumWidthRequest = double.PositiveInfinity;
            ColumnFrame.HorizontalOptions = LayoutOptions.Fill;
            ColumnFrame.Margin = new Thickness(0);
            ColumnFrame.StrokeThickness = 0;
            ColumnFrame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0) };
        }
        else
        {
            ColumnFrame.WidthRequest = 460;
            ColumnFrame.MaximumWidthRequest = 500;
            ColumnFrame.HorizontalOptions = LayoutOptions.Center;
            ColumnFrame.Margin = new Thickness(0, 16);
            ColumnFrame.StrokeThickness = 1;
            ColumnFrame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) };
        }
    }

    private void OnContainerSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0) return;
        ApplyLayoutMetrics();
    }
}
