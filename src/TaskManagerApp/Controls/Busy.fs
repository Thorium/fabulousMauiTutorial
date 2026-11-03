/// A busy indicator laid over content while a slow call runs, so the content stays visible
/// instead of being replaced by a spinner.
module TaskManagerApp.Controls.Busy

open Fabulous
open Fabulous.Maui
open Microsoft.Maui.Graphics

open type Fabulous.Maui.View

/// A spinner with a caption, for a page that has nothing to show yet.
let indicator<'msg when 'msg: equality> (caption: string) : WidgetBuilder<'msg, IFabVerticalStackLayout> =
    (VStack(spacing = 8.) {
        ActivityIndicator(true).color Colors.Blue

        Label(caption).textColor(Colors.Gray).centerTextHorizontal()
    })
        .center()

/// A translucent layer with a spinner, to place over content in the same Grid cell.
let overlay<'msg when 'msg: equality> (caption: string) : WidgetBuilder<'msg, IFabContentView> =
    ContentView(indicator<'msg> caption).background(SolidColorBrush(Color(1.f, 1.f, 1.f, 0.6f)))
