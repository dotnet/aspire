// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Components.Tests;

public class GridColumnManagerTests
{
    [Fact]
    public void Returns_Correct_TemplateColumn_String()
    {
        var dimensionManager = new DimensionManager();
        dimensionManager.InvokeOnBrowserDimensionsChanged(new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));

        var manager = new GridColumnManager([
            new GridColumn("NoMobile", "1fr", null),
            new GridColumn("Both1", "1fr", "1fr"),
            new GridColumn("Both2", "3fr", "0.5fr"),
            new GridColumn("NoDesktop", null, "2fr"),
            new GridColumn("NoDesktopWithIsVisibleFalse", null, "2fr", IsVisible: () => false),
            new GridColumn("NoDesktopWithIsVisibleTrue", null, "4fr", IsVisible: () => true)
        ], dimensionManager);

        Assert.Equal("1fr 1fr 3fr", manager.GetGridTemplateColumns());

        dimensionManager.InvokeOnBrowserDimensionsChanged(new ViewportInformation(IsDesktop: false, IsUltraLowHeight: true, IsUltraLowWidth: false));
        Assert.Equal("1fr 0.5fr 2fr 4fr", manager.GetGridTemplateColumns());
    }

    [Fact]
    public void Returns_Right_Columns_IsVisible()
    {
        var dimensionManager = new DimensionManager();
        dimensionManager.InvokeOnBrowserDimensionsChanged(new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));

        var manager = new GridColumnManager([
            new GridColumn("NoMobile", "1fr", null),
            new GridColumn("Both1", "1fr", "1fr"),
            new GridColumn("Both2", "3fr", "0.5fr"),
            new GridColumn("NoDesktop", null, "2fr"),
            new GridColumn("NoDesktopWithIsVisibleFalse", null, "2fr", IsVisible: () => false),
            new GridColumn("NoDesktopWithIsVisibleTrue", null, "4fr", IsVisible: () => true)
        ], dimensionManager);

        Assert.True(manager.IsColumnVisible("NoMobile"));
        Assert.True(manager.IsColumnVisible("Both1"));
        Assert.True(manager.IsColumnVisible("Both2"));
        Assert.False(manager.IsColumnVisible("NoDesktop"));

        dimensionManager.InvokeOnBrowserDimensionsChanged(new ViewportInformation(IsDesktop: false, IsUltraLowHeight: true, IsUltraLowWidth: false));
        Assert.False(manager.IsColumnVisible("NoMobile"));
        Assert.True(manager.IsColumnVisible("Both1"));
        Assert.True(manager.IsColumnVisible("Both2"));
        Assert.True(manager.IsColumnVisible("NoDesktop"));
        Assert.False(manager.IsColumnVisible("NoDesktopWithIsVisibleFalse"));
        Assert.True(manager.IsColumnVisible("NoDesktopWithIsVisibleTrue"));
    }
}
