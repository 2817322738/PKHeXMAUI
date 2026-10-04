#if ANDROID
using Android.Widget;
#endif
using Microsoft.Maui.Platform;
using PKHeX.Core;
using System.Collections;
using System.Globalization;

namespace PKHeXMAUI;

/// <summary>
/// Custom Combo Box control that allows setting a generic ItemSource.
/// </summary>
public partial class comboBox : Microsoft.Maui.Controls.ContentView
{
    /// <summary>
    /// Bindable property for DisplayMemberPath.
    /// </summary>
    public static BindableProperty DisplayMemberPathProperty =
        BindableProperty.Create(
            nameof(DisplayMemberPath),
            typeof(string),
            typeof(comboBox),
            ".",
            propertyChanged: OnItemsCollectionChanged);

    /// <summary>
    /// Bindable property for ItemSource.
    /// </summary>
    public static BindableProperty ItemSourceProperty =
        BindableProperty.Create(
            nameof(ItemSource),
            typeof(IEnumerable),
            typeof(comboBox),
            new List<object>(),
            propertyChanged: OnItemsCollectionChanged);

    /// <summary>
    /// Bindable property for Title.
    /// </summary>
    public static BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(comboBox));

    /// <summary>
    /// Bindable property for Placeholder.
    /// </summary>
    public static BindableProperty PlaceholderProperty =
        BindableProperty.Create(
            nameof(Placeholder),
            typeof(string),
            typeof(comboBox));

    /// <summary>
    /// Bindable property for SelectedItem.
    /// </summary>
    public static BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(comboBox),
            null,
            BindingMode.TwoWay,
            propertyChanged: SetSelectedItem);

    /// <summary>
    /// Bindable property for SelectedIndex.
    /// </summary>
    public static BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(comboBox),
            -1,
            propertyChanged: SetSelectedIndex);

    public event EventHandler? SelectedIndexChanged;
    public event EventHandler? TextChanged;

    /// <summary>
    /// Gets or sets the Binding Path for displaying the object.
    /// </summary>
    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the Title.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// String representation of the items in ItemSource.
    /// </summary>
    public IList<string> Items = [];

    /// <summary>
    /// Gets or sets the ItemSource.
    /// </summary>
    public IList ItemSource
    {
        get => (IList)GetValue(ItemSourceProperty);
        set => SetValue(ItemSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the Placeholder.
    /// </summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected index.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set
        {
            picker.SelectedItem = value;
            SetValue(SelectedItemProperty, value);
        }
    }

    public CollectionView picker;

    public comboBox()
    {
        InitializeComponent();

        picker = new CollectionView
        {
            BackgroundColor = Colors.White
        };

        picker.SelectionChanged += IndexChanged;
        picker.HeightRequest = 50;
        picker.SelectionMode = SelectionMode.Single;

        picker.SetBinding(
            Microsoft.Maui.Controls.CollectionView.ItemsSourceProperty,
            new Binding("ItemSource", source: ThisView));

        picker.ItemTemplate = new DataTemplate(() =>
        {
            Grid cell = [];
            Label label = new();

            label.SetBinding(
                Label.TextProperty,
                new Binding(DisplayMemberPath));

            label.SetBinding(
                Label.BackgroundColorProperty,
                new Binding(
                    "Valid",
                    converter: new BoolToColorConverter()));

            label.TextColor = Colors.Black;

            cell.Add(label);
            return cell;
        });

#if ANDROID
        entry.Unfocused += (s, e) => popupWindow.Dismiss();
#endif

        picker.ZIndex = 1;
    }

    static void OnItemsCollectionChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((comboBox)bindable)
            .OnItemsCollectionChanged(bindable, EventArgs.Empty);
    }

    public void OnItemsCollectionChanged(object sender, EventArgs e)
    {
        if (ItemSource is null)
            return;

        Items.Clear();

        foreach (var item in ItemSource)
        {
            Items.Add(GetDisplayMember(item));
        }
    }

    private string GetDisplayMember(object item)
    {
        if (DisplayMemberPath == ".")
        {
            return item?.ToString() ?? "";
        }

        var property = item
            .GetType()
            .GetProperty(DisplayMemberPath);

        var result = property?.GetValue(item)?.ToString();

        return result ?? "";
    }

    /// <summary>
    /// Filters the items based on the text entered by the user.
    ///
    /// Supports:
    /// - Chinese names
    /// - English names when present
    /// - Partial name search
    /// - ComboItem numeric Value search
    /// </summary>
    private void entry_textChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (ItemSource is null)
            return;

#if ANDROID
        if (popupWindow?.IsShowing == false)
            ShowDropdown();
#endif

        string searchText = entry.Text?.Trim() ?? "";

        // Empty input: show everything.
        if (string.IsNullOrEmpty(searchText))
        {
            picker.ItemsSource =
                ItemSource.Cast<object>().ToList();

            TextChanged?.Invoke(this, e);
            return;
        }

        // Special handling for 8-character hexadecimal input.
        if (searchText.Length == 8)
        {
            int hex = (int)Util.GetHexValue(searchText);

            if (hex != 0)
            {
                var result =
                    BlockEditor8.SortedBlockKeys
                        .ToList()
                        .Find(z => z.Value == hex);

                if (result is not null)
                {
                    picker.ItemsSource = new List<ComboItem>
                    {
                        result
                    };

                    TextChanged?.Invoke(this, e);
                    return;
                }
            }
        }

        // Numeric search.
        //
        // Example:
        // 6   -> Pokémon #6
        // 006 -> Pokémon #6
        // 25  -> Pokémon #25
        //
        if (int.TryParse(searchText, out int number))
        {
            var numericResults = ItemSource
                .Cast<object>()
                .Where(item =>
                    item is ComboItem combo &&
                    combo.Value == number)
                .ToList();

            if (numericResults.Count > 0)
            {
                picker.ItemsSource = numericResults;

                TextChanged?.Invoke(this, e);
                return;
            }
        }

        // Text search.
        //
        // Contains() instead of StartsWith():
        // 输入“火龙”也可以找到“喷火龙”。
        var filtered = ItemSource
            .Cast<object>()
            .Where(item =>
            {
                string text = GetDisplayMember(item);

                return text.Contains(
                    searchText,
                    StringComparison.CurrentCultureIgnoreCase);
            })
            .ToList();

        picker.ItemsSource = filtered;

        TextChanged?.Invoke(this, e);
    }

    private string SelectedItemText = "";

    /// <summary>
    /// Changes the selected item.
    /// </summary>
    private void IndexChanged(
        object? sender,
        EventArgs? e)
    {
        if (picker.SelectedItem is null)
            return;

        SelectedItemText =
            GetDisplayMember(picker.SelectedItem);

        entry.Text = SelectedItemText;

        SelectedItem = picker.SelectedItem;

        SelectedIndex =
            ItemSource
                .Cast<object>()
                .ToList()
                .IndexOf(SelectedItem);

        SelectedIndexChanged?.Invoke(this, e!);

#if ANDROID
        popupWindow?.Dismiss();
#endif
    }

    public void ForceSelection(
        object? sender,
        EventArgs? e)
    {
        picker.SelectedItem = SelectedItem;
    }

    static void SetSelectedItem(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((comboBox)bindable).SetSelectedItem(newValue);
    }

    public void SetSelectedItem(object? value)
    {
        picker.SelectedItem = value;

        if (value is not null)
        {
            entry.Text = GetDisplayMember(value);
        }
    }

    static void SetSelectedIndex(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if ((int)newValue < 0)
            return;

        var lv = (comboBox)bindable;

        var items = lv.ItemSource
            .Cast<object>()
            .ToList();

        int index = (int)newValue;

        if (index >= 0 && index < items.Count)
        {
            lv.SelectedItem = items[index];
        }
    }

    /// <summary>
    /// Hides the dropdown list.
    /// </summary>
    public void HideList()
    {
#if ANDROID
        popupWindow?.Dismiss();
#endif
    }

    /// <summary>
    /// Shows the dropdown list.
    /// </summary>
    public void ShowList()
    {
        ShowDropdown();
    }

    private void ShowList(
        object sender,
        FocusEventArgs e)
    {
        ShowDropdown();
    }

    private void ClearText(
        object sender,
        EventArgs e)
    {
        entry.Text = string.Empty;

        if (!entry.IsFocused)
            entry.Focus();
    }

    /// <summary>
    /// Automatically selects an item when the user presses Enter.
    ///
    /// Supports both numeric Value and displayed text.
    /// </summary>
    private void AutoCompleteText(
        object sender,
        EventArgs e)
    {
        string searchText =
            entry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(searchText))
            return;

        object? item = null;

        // First try numeric search.
        if (int.TryParse(searchText, out int number))
        {
            item = ItemSource
                .Cast<object>()
                .FirstOrDefault(x =>
                    x is ComboItem combo &&
                    combo.Value == number);
        }

        // Then try text search.
        item ??= ItemSource
            .Cast<object>()
            .FirstOrDefault(x =>
                GetDisplayMember(x).Contains(
                    searchText,
                    StringComparison.CurrentCultureIgnoreCase));

        if (item is null)
            return;

        SelectedItem = item;
        picker.SelectedItem = item;

        SelectedItemText = GetDisplayMember(item);

        entry.Text = SelectedItemText;
    }

#if ANDROID
    private PopupWindow popupWindow = new();
#endif

    /// <summary>
    /// Shows the dropdown list.
    /// Currently for Android only.
    /// </summary>
    private void ShowDropdown()
    {
#if ANDROID
        if (this.Handler?.MauiContext == null)
            return;

        popupWindow?.Dismiss();

        var contentView =
            picker.ToPlatform(this.Handler.MauiContext);

        popupWindow = new PopupWindow(
            contentView,
            (int)(this.Width * 2),
            300)
        {
            OutsideTouchable = true
        };

        var parentView =
            this.entry.ToPlatform(this.Handler.MauiContext);

        popupWindow.ShowAsDropDown(parentView);
#endif
    }
}

internal class BoolToColorConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is null)
            return Colors.White;

        return (bool)value
            ? Colors.Green
            : Colors.White;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
