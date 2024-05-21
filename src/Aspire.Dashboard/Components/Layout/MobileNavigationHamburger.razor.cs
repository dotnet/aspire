using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class MobileNavigationHamburger : ComponentBase
{
}

record NavMenuItemEntry(string Text, Func<Task> OnClick, Icon? Icon = null);
