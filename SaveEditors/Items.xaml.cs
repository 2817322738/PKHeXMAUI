using static PKHeXMAUI.MainPage;
using PKHeX.Core;
using System.Windows.Input;
using PKHeX.Core.Injection;
using System.Globalization;

namespace PKHeXMAUI;

public partial class Items : TabbedPage
{
    private readonly string[] itemlist;
    public List<List<itemInfo>> SourceList = [];
    private readonly PlayerBag pouches;
    private readonly SaveFile Origin;
    private readonly SaveFile SAV;
    public static int currentcount = 995;

    public Items()
    {
        InitializeComponent();

        SAV = (Origin = sav).Clone();

        // 道具名称：使用 PKHeX Core 自带的简体中文
        itemlist =
        [
            .. GameInfo.GetStrings("zh-Hans")
                .GetItemStrings(SAV.Context, SAV.Version)
        ];

        for (int i = 0; i < itemlist.Length; i++)
        {
            if (string.IsNullOrEmpty(itemlist[i]))
                itemlist[i] = $"(道具 #{i:000})";
        }

        if (Remote.Connected)
        {
            var success = Remote.Injector.ReadBlockFromString(
                Remote,
                sav,
                "Items",
                out var data
            );

            if (success)
            {
                try
                {
                    switch (sav)
                    {
                        case SAV9ZA za:
                            data?.ToArray()[0].CopyTo(
                                za.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV9SV s:
                            data?.ToArray()[0].CopyTo(
                                s.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV8LA la:
                            data?.ToArray()[0].CopyTo(
                                la.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV8BS bs:
                            data?.ToArray()[0].CopyTo(
                                bs.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV8SWSH sw:
                            data?.ToArray()[0].CopyTo(
                                sw.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV7 s7:
                            data?.ToArray()[0].CopyTo(
                                s7.Items.Data.ToArray(),
                                0
                            );
                            break;

                        case SAV6 s6:
                            data?.ToArray()[0].CopyTo(
                                s6.Items.Data.ToArray(),
                                0
                            );
                            break;
                    }
                }
                catch (Exception)
                {
                    // 远程道具数据读取失败时，不让道具编辑器直接崩溃
                }
            }
            else
            {
                _ = DisplayAlertAsync(
                    "错误",
                    "未找到数据",
                    "确定"
                );
            }
        }

        pouches = SAV.Inventory;

        foreach (var pouch in pouches.Pouches)
        {
            var content = new ContentPage()
            {
                Title = pouch.Type.ToString()
            };

            Grid header = [];

            header.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = GridLength.Star
                }
            );

            header.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = GridLength.Star
                }
            );

            header.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = GridLength.Star
                }
            );

            header.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = GridLength.Star
                }
            );

            header.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = GridLength.Star
                }
            );

            Label headerItem = new()
            {
                Text = "道具"
            };

            header.Add(headerItem, 1);

            Label headerCount = new()
            {
                Text = "数量",
                HorizontalOptions = LayoutOptions.End
            };

            header.Add(headerCount, 2);

            Label headerFav = new()
            {
                Text = "收藏",
                HorizontalOptions = LayoutOptions.End
            };

            header.Add(headerFav, 3);

            Label headerNew = new()
            {
                Text = "新",
                HorizontalOptions = LayoutOptions.Center
            };

            header.Add(headerNew, 4);

            var ItemCollection = new CollectionView
            {
                WidthRequest = 400,
                HeightRequest = 500,

                ItemTemplate = new DataTemplate(() =>
                {
                    Grid grid = [];

                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition()
                        {
                            Width = GridLength.Star
                        }
                    );

                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition()
                        {
                            Width = new GridLength(
                                2,
                                GridUnitType.Star
                            )
                        }
                    );

                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition()
                        {
                            Width = GridLength.Star
                        }
                    );

                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition()
                        {
                            Width = GridLength.Star
                        }
                    );

                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition()
                        {
                            Width = GridLength.Star
                        }
                    );

                    comboBox itemname = new()
                    {
                        Placeholder = "（无）",
                        BackgroundColor = Colors.Transparent,
                    };

                    itemname.SelectedIndexChanged += ChangeItemSprite;

                    var pouchstrings = GetStringsForPouch(
                        pouch.GetAllItems()
                    );

                    itemname.ItemSource = pouchstrings;

                    itemname.SetBinding(
                        comboBox.SelectedItemProperty,
                        "name",
                        mode: BindingMode.TwoWay
                    );

                    itemname.Loaded += itemname.ForceSelection;

                    grid.Add(itemname, 1);

                    Image itemsp = new()
                    {
                        HorizontalOptions = LayoutOptions.Start,
                        HeightRequest = 25,
                        WidthRequest = 25
                    };

                    itemsp.SetBinding(
                        Image.SourceProperty,
                        "itemsprite"
                    );

                    grid.Add(itemsp);

                    Editor itemCount = new();

                    itemCount.SetBinding(
                        Editor.TextProperty,
                        "count",
                        mode: BindingMode.TwoWay
                    );

                    grid.Add(itemCount, 2);

                    CheckBox ItemFavCheck = new();

                    ItemFavCheck.SetBinding(
                        CheckBox.IsCheckedProperty,
                        "isfav",
                        mode: BindingMode.TwoWay
                    );

                    grid.Add(ItemFavCheck, 3);

                    CheckBox ItemNewCheck = new();

                    ItemNewCheck.SetBinding(
                        CheckBox.IsCheckedProperty,
                        "isnew",
                        BindingMode.TwoWay
                    );

                    grid.Add(ItemNewCheck, 4);

                    return grid;
                })
            };

            var infolist = new List<itemInfo>();

            foreach (var item in pouch.Items)
            {
                try
                {
                    infolist.Add(
                        new itemInfo(
                            item,
                            itemlist
                        )
                    );
                }
                catch (Exception)
                {
                    // 某个异常道具无法解析时，跳过该道具，
                    // 防止整个道具编辑器闪退。
                }
            }

            SourceList.Add(infolist);

            ItemCollection.ItemsSource = infolist;

            Button GiveAll = new()
            {
                Text = "全部给予"
            };

            GiveAll.Clicked += GiveAll_Clicked;

            ToolTipProperties.SetText(
                GiveAll,
                "将此袋中的所有道具数量设置为上方数量，即使你当前没有这些道具"
            );

            Button ModifyAll = new()
            {
                Text = "全部修改"
            };

            ModifyAll.Clicked += ModifyAll_Clicked;

            ToolTipProperties.SetText(
                ModifyAll,
                "将你已有的所有道具数量设置为上方数量"
            );

            Editor GiveCount = new()
            {
                Text = "995"
            };

            GiveCount.TextChanged += SetCount;

            Button ClearAll = new()
            {
                Text = "全部清空"
            };

            ToolTipProperties.SetText(
                ClearAll,
                "清空当前道具袋"
            );

            ClearAll.Clicked += ClearAll_Clicked;

            var itemrefresh = new RefreshView();

            var itemscroll = new ScrollView
            {
                Content = new StackLayout()
                {
                    Children =
                    {
                        header,
                        ItemCollection,
                        GiveCount,
                        GiveAll,
                        ModifyAll,
                        ClearAll
                    }
                }
            };

            itemrefresh.Content = itemscroll;

            ICommand refreshview = new Command(async () =>
            {
                var pindex = Array.IndexOf(
                    [.. ItemsMain.Children],
                    ItemsMain.CurrentPage
                ) - 1;

                if (pindex >= 0 &&
                    pindex < SourceList.Count)
                {
                    ItemCollection.ItemsSource =
                        SourceList[pindex];
                }

                itemrefresh.IsRefreshing = false;
            });

            itemrefresh.Command = refreshview;

            content.Content = itemrefresh;

            ItemsMain.Children.Add(content);
        }

        ItemsMain.CurrentPageChanged += SetCount;
    }

    private void ClearAll_Clicked(
        object? sender,
        EventArgs? e)
    {
        var pindex = Array.IndexOf(
            [.. ItemsMain.Children],
            ItemsMain.CurrentPage
        ) - 1;

        if (pindex < 0 ||
            pindex >= SourceList.Count ||
            pindex >= pouches.Pouches.Count)
            return;

        var list = SourceList[pindex];

        list.Clear();

        var pouchlist = pouches.Pouches[pindex];

        pouchlist.RemoveAll();

        foreach (var item in pouchlist.Items)
        {
            try
            {
                list.Add(
                    new itemInfo(
                        item,
                        itemlist
                    )
                );
            }
            catch (Exception)
            {
                // 忽略无法解析的道具
            }
        }

        SourceList[pindex] = list;
    }

    private void SetCount(
        object? sender,
        EventArgs? e)
    {
        if (sender is Editor ed)
        {
            var parsed = int.TryParse(
                ed.Text,
                out var count
            );

            if (parsed)
                currentcount = count;
        }
        else
        {
            currentcount = 995;
        }
    }

    private void GiveAll_Clicked(
        object? sender,
        EventArgs? e)
    {
        var pindex = Array.IndexOf(
            [.. ItemsMain.Children],
            ItemsMain.CurrentPage
        ) - 1;

        if (pindex < 0 ||
            pindex >= SourceList.Count ||
            pindex >= pouches.Pouches.Count)
            return;

        var list = SourceList[pindex];

        list.Clear();

        var pouchlist = pouches.Pouches[pindex];

        var allitems = pouchlist.GetAllItems();

        pouchlist.GiveAllItems(
            sav.Inventory,
            allitems,
            currentcount
        );

        foreach (var item in pouchlist.Items)
        {
            try
            {
                list.Add(
                    new itemInfo(
                        item,
                        itemlist
                    )
                );
            }
            catch (Exception)
            {
                // 忽略无法解析的道具
            }
        }

        SourceList[pindex] = list;
    }

    private void ModifyAll_Clicked(
        object? sender,
        EventArgs? e)
    {
        var pindex = Array.IndexOf(
            [.. ItemsMain.Children],
            ItemsMain.CurrentPage
        ) - 1;

        if (pindex < 0 ||
            pindex >= SourceList.Count ||
            pindex >= pouches.Pouches.Count)
            return;

        var list = SourceList[pindex];

        list.Clear();

        var pouchlist = pouches.Pouches[pindex];

        pouchlist.ModifyAllCount(
            currentcount
        );

        foreach (var item in pouchlist.Items)
        {
            try
            {
                list.Add(
                    new itemInfo(
                        item,
                        itemlist
                    )
                );
            }
            catch (Exception)
            {
                // 忽略无法解析的道具
            }
        }

        SourceList[pindex] = list;
    }

    private string[] GetStringsForPouch(
        ReadOnlySpan<ushort> items,
        bool sort = true)
    {
        string[] res = new string[items.Length + 1];

        for (int i = 0; i < res.Length - 1; i++)
        {
            var index = items[i];

            if (index < itemlist.Length)
            {
                res[i] = itemlist[index];
            }
            else
            {
                res[i] = $"(道具 #{index:000})";
            }
        }

        res[items.Length] =
            itemlist.Length > 0
                ? itemlist[0]
                : "（无）";

        if (sort)
            Array.Sort(res);

        return res;
    }

    private async void SaveItemsClicked(
        object sender,
        EventArgs e)
    {
        int i = 0;

        saveitems.Text = "保存中...";

        await Task.Delay(100);

        foreach (var pouch in pouches.Pouches)
        {
            if (i < SourceList.Count)
                await setbag(
                    pouch,
                    i
                );

            i++;
        }

        pouches.CopyTo(SAV);

        Origin.CopyChangesFrom(SAV);

        if (Remote.Connected)
        {
            if (Remote.Injector is LPBDSP)
            {
                try
                {
                    Remote.Injector.WriteBlockFromString(
                        Remote,
                        "Items",
                        ((SAV8BS)Origin)
                            .Items
                            .Data
                            .ToArray(),
                        ((SAV8BS)Origin).Items
                    );
                }
                catch (Exception)
                {
                    // 忽略远程写入错误
                }
            }
            else
            {
                try
                {
                    Remote.Injector.WriteBlocksFromSAV(
                        Remote,
                        "Items",
                        Origin
                    );
                }
                catch (Exception)
                {
                    // 忽略远程写入错误
                }
            }
        }

        await Navigation.PopModalAsync();
    }

    private async Task setbag(
        InventoryPouch pouch,
        int sourceindex)
    {
        if (sourceindex < 0 ||
            sourceindex >= SourceList.Count)
            return;

        int ctr = 0;

        var list = SourceList[sourceindex];

        foreach (var it in list)
        {
            var itemindex = Array.IndexOf(
                itemlist,
                it.name
            );

            var validct = int.TryParse(
                it.count,
                out var itemct
            );

            if (itemindex <= 0)
                continue;

            if (!validct)
                continue;

            try
            {
                var item = pouch.GetEmpty(
                    itemindex,
                    itemct
                );

                if (item is IItemFavorite f)
                    f.IsFavorite = it.isfav;

                if (item is IItemNewFlag n)
                    n.IsNew = it.isnew;

                if (item is IItemFreeSpace fs)
                    fs.IsFreeSpace = it.isfreespace;

                if (item is IItemFreeSpaceIndex fi)
                    fi.FreeSpaceIndex =
                        it.isfreespaceindex;

                if (ctr < pouch.Items.Length)
                {
                    pouch.Items[ctr] = item;
                    ctr++;
                }
            }
            catch (Exception)
            {
                // 某个道具无法写入时跳过
            }
        }

        for (
            int i = ctr;
            i < pouch.Items.Length;
            i++)
        {
            try
            {
                pouch.Items[i] =
                    pouch.GetEmpty();
            }
            catch (Exception)
            {
                // 忽略无法清空的槽位
            }
        }
    }

    private void CloseItems(
        object? sender,
        EventArgs? e)
    {
        Navigation.PopModalAsync();
    }

#nullable enable

    private void ChangeItemSprite(
        object? sender,
        EventArgs? e)
    {
        var pindex = Array.IndexOf(
            [.. ItemsMain.Children],
            ItemsMain.CurrentPage
        ) - 1;

        if (pindex < 0 ||
            pindex >= SourceList.Count)
            return;

        var CurrentSource =
            SourceList[pindex];

        itemInfo? CurrentItem =
            CurrentSource.Find(
                z =>
                    z.name ==
                    (string?)((comboBox?)sender)?.SelectedItem
            );

        if (CurrentItem is not null)
        {
            try
            {
                var lump =
                    HeldItemLumpUtil.GetIsLump(
                        CurrentItem.InvItem.Index,
                        sav.Context
                    );

                CurrentItem.itemsprite =
                    sav.Generation >= 9
                        ? lump is HeldItemLumpImage.TechnicalMachine
                            ? "aitem_tm.png"
                            : lump is HeldItemLumpImage.TechnicalRecord
                                ? "aitem_tr.png"
                                : $"aitem_{Array.IndexOf(
                                    itemlist,
                                    CurrentItem.name
                                )}.png"
                        : lump is HeldItemLumpImage.TechnicalMachine
                            ? "bitem_tm.png"
                            : lump is HeldItemLumpImage.TechnicalRecord
                                ? "bitem_tr.png"
                                : $"bitem_{Array.IndexOf(
                                    itemlist,
                                    CurrentItem.name
                                )}.png";

                if (
                    CurrentItem.InvItem.Index <= ushort.MaxValue &&
                    itemInfo.Pouch_Material_SV.Contains(
                        (ushort)CurrentItem.InvItem.Index
                    )
                )
                {
                    CurrentItem.itemsprite =
                        "aitem_material.png";
                }

                if (
                    CurrentItem.InvItem.Index >= 2522 &&
                    CurrentItem.InvItem.Index <= 2546
                )
                {
                    CurrentItem.itemsprite =
                        "aitem_snack.png";
                }

                if (
                    CurrentItem.InvItem.Index <= ushort.MaxValue &&
                    itemInfo.Pouch_Picnic.Contains(
                        (ushort)CurrentItem.InvItem.Index
                    )
                )
                {
                    CurrentItem.itemsprite =
                        "aitem_picnic.png";
                }

                SourceList[pindex] =
                    CurrentSource;
            }
            catch (Exception)
            {
                // 道具图片生成失败时，不让整个编辑器闪退
            }
        }
    }
}

public class itemInfo
{
    public string count { get; set; }
    public string name { get; set; }
    public bool isfav { get; set; }
    public bool isnew { get; set; }
    public string itemsprite { get; set; }
    public bool isfreespace { get; set; }
    public uint isfreespaceindex { get; set; }
    public InventoryItem InvItem { get; set; }

    public itemInfo(
        InventoryItem item,
        string[] itemlist)
    {
        count = item.Count.ToString();

        if (
            item.Index >= 0 &&
            item.Index < itemlist.Length
        )
        {
            name = itemlist[item.Index];
        }
        else
        {
            name = $"(道具 #{item.Index:000})";
        }

        if (item is IItemFavorite f)
            isfav = f.IsFavorite;

        if (item is IItemNewFlag n)
            isnew = n.IsNew;

        if (item is IItemFreeSpace fs)
            isfreespace = fs.IsFreeSpace;

        if (item is IItemFreeSpaceIndex fi)
            isfreespaceindex =
                fi.FreeSpaceIndex;

        try
        {
            var lump =
                HeldItemLumpUtil.GetIsLump(
                    item.Index,
                    sav.Context
                );

            itemsprite =
                sav.Generation >= 9
                    ? lump is HeldItemLumpImage.TechnicalMachine
                        ? "aitem_tm.png"
                        : lump is HeldItemLumpImage.TechnicalRecord
                            ? "aitem_tr.png"
                            : $"aitem_{item.Index}.png"
                    : lump is HeldItemLumpImage.TechnicalMachine
                        ? "bitem_tm.png"
                        : lump is HeldItemLumpImage.TechnicalRecord
                            ? "bitem_tr.png"
                            : $"bitem_{item.Index}.png";
        }
        catch (Exception)
        {
            itemsprite =
                sav.Generation >= 9
                    ? "aitem_0.png"
                    : "bitem_0.png";
        }

        if (
            item.Index <= ushort.MaxValue &&
            Pouch_Material_SV.Contains(
                (ushort)item.Index
            )
        )
        {
            itemsprite =
                "aitem_material.png";
        }

        if (
            item.Index >= 2522 &&
            item.Index <= 2546
        )
        {
            itemsprite =
                "aitem_snack.png";
        }

        if (
            item.Index <= ushort.MaxValue &&
            Pouch_Picnic.Contains(
                (ushort)item.Index
            )
        )
        {
            itemsprite =
                "aitem_picnic.png";
        }

        InvItem = item;
    }

    public static List<ushort> Pouch_Material_SV =
    [
        1956, 1957, 1958, 1959, 1960, 1961, 1962, 1963, 1964, 1965,
        1966, 1967, 1968, 1969, 1970, 1971, 1972, 1973, 1974, 1975,
        1976, 1977, 1978, 1979, 1980, 1981, 1982, 1983, 1984, 1985,
        1986, 1987, 1988, 1989, 1990, 1991, 1992, 1993, 1994, 1995,
        1996, 1997, 1998, 1999, 2000, 2001, 2002, 2003, 2004, 2005,
        2006, 2007, 2008, 2009, 2010, 2011, 2012, 2013, 2014, 2015,
        2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023, 2024, 2025,
        2026, 2027, 2028, 2029, 2030, 2031, 2032, 2033, 2034, 2035,
        2036, 2037, 2038, 2039, 2040, 2041, 2042, 2043, 2044, 2045,
        2046, 2047, 2048, 2049, 2050, 2051, 2052, 2053, 2054, 2055,
        2056, 2057, 2058, 2059, 2060, 2061, 2062, 2063, 2064, 2065,
        2066, 2067, 2068, 2069, 2070, 2071, 2072, 2073, 2074, 2075,
        2076, 2077, 2078, 2079, 2080, 2081, 2082, 2083, 2084, 2085,
        2086, 2087, 2088, 2089, 2090, 2091, 2092, 2093, 2094, 2095,
        2096, 2097, 2098, 2099, 2100, 2101, 2102, 2103, 2104, 2105,
        2106, 2107, 2108, 2109, 2110, 2111, 2112, 2113, 2114, 2115,
        2116, 2117, 2118, 2119, 2120, 2121, 2122, 2123, 2124, 2125,
        2126, 2127, 2128, 2129, 2130, 2131, 2132, 2133, 2134, 2135,
        2136, 2137, 2138, 2139, 2140, 2141, 2142, 2143, 2144, 2145,
        2146, 2147, 2148, 2149, 2150, 2151, 2152, 2153, 2154, 2155,
        2156, 2157, 2158, 2159, 2438, 2439, 2440, 2441, 2442, 2443,
        2444, 2445, 2446, 2447, 2448, 2449, 2450, 2451, 2452, 2453,
        2454, 2455, 2456, 2457, 2458, 2459, 2460, 2461, 2462, 2463,
        2464, 2465, 2466, 2467, 2468, 2469, 2470, 2471, 2472, 2473,
        2474, 2475, 2476, 2477, 2478, 2479, 2480, 2481, 2482, 2483,
        2484, 2485, 2486, 2487, 2488, 2489, 2490, 2491, 2492, 2493,
        2494, 2495, 2496, 2497, 2498, 2499, 2500, 2501, 2502, 2503,
        2504, 2505, 2506, 2507, 2508, 2509, 2510, 2511, 2512, 2513,
        2514, 2515, 2516, 2517, 2518, 2519, 2520, 2521,
    ];

    public static List<ushort> Pouch_Picnic =
    [
        2311,
        2313, 2314, 2315, 2316, 2317, 2318, 2319, 2320, 2321, 2322,
        2323, 2324, 2325, 2326, 2327, 2329, 2330, 2331, 2332, 2333,
        2334, 2335, 2336, 2337, 2338, 2339, 2340, 2341, 2342, 2348,
        2349, 2350, 2351, 2352, 2353, 2354, 2355, 2356, 2357, 2358,
        2359, 2360, 2361, 2362, 2363, 2364, 2365, 2366, 2367, 2368,
        2369, 2370, 2371, 2372, 2373, 2374, 2375, 2376, 2377, 2378,
        2379, 2380, 2381, 2382, 2383, 2384, 2385, 2386, 2387, 2388,
        2389, 2390, 2391, 2392, 2393, 2394, 2395, 2396, 2397, 2398,
        2399, 2400, 2417, 2418, 2419, 2420, 2421, 2422, 2423, 2424,
        2425, 2426, 2427, 2428, 2429, 2430, 2431, 2432, 2433, 2434,
        2435, 2436, 2437, 2548, 2551, 2552,
    ];
}
