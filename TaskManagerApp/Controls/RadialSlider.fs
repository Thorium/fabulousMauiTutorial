namespace TaskManagerApp.Controls

open System
open System.Runtime.CompilerServices
open Fabulous
open Fabulous.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Graphics
open SkiaSharp
open SkiaSharp.Views.Maui
open SkiaSharp.Views.Maui.Controls

/// SkiaSharp-based radial slider control for value selection
type SkRadialSlider() =
    inherit SKCanvasView()
    
    let mutable hasTouch = false
    let mutable progress = 0.0
    let mutable progressArc = 0.0
    let mutable touchX = -1.0f
    let mutable touchY = -1.0f
    
    let progressPaint = new SKPaint(Style = SKPaintStyle.Stroke, Color = SKColors.Blue, StrokeWidth = 10.0f)
    let knobPaint = new SKPaint(Style = SKPaintStyle.Fill, Color = SKColors.Blue)
    let trackPaint = new SKPaint(Style = SKPaintStyle.Stroke, Color = SKColors.LightGray, StrokeWidth = 5.0f)
    
    // Helper functions
    let toRadians angle = angle * Math.PI / 180.0
    
    let pointOnCircle (originX: float32) (originY: float32) (radius: float32) (x: float32) (y: float32) =
        let angle = Math.Atan2(float (y - originY), float (x - originX))
        let px = originX + radius * float32 (Math.Cos(angle))
        let py = originY + radius * float32 (Math.Sin(angle))
        (px, py)
    
    let pointOnCircleToAngle (cx: float32) (cy: float32) (originX: float32) (originY: float32) =
        let angle = Math.Atan2(float (cy - originY), float (cx - originX)) * 180.0 / Math.PI
        if angle < 0.0 then angle + 360.0 else angle
    
    do
        base.EnableTouchEvents <- true

    // Bindable properties
    static let trackColorProperty = 
        BindableProperty.Create("TrackColor", typeof<Color>, typeof<SkRadialSlider>, Colors.LightGray)
    
    static let knobColorProperty = 
        BindableProperty.Create("KnobColor", typeof<Color>, typeof<SkRadialSlider>, Colors.Blue)
    
    static let trackProgressColorProperty = 
        BindableProperty.Create("TrackProgressColor", typeof<Color>, typeof<SkRadialSlider>, Colors.Blue)
    
    static let startProperty = 
        BindableProperty.Create("Start", typeof<float>, typeof<SkRadialSlider>, 135.0)
    
    static let arcProperty = 
        BindableProperty.Create("Arc", typeof<float>, typeof<SkRadialSlider>, 270.0)
    
    static let minimumProperty = 
        BindableProperty.Create("Minimum", typeof<float>, typeof<SkRadialSlider>, 0.0)
    
    static let maximumProperty = 
        BindableProperty.Create("Maximum", typeof<float>, typeof<SkRadialSlider>, 10.0)
    
    static let valueProperty = 
        BindableProperty.Create("Value", typeof<float>, typeof<SkRadialSlider>, 5.0, BindingMode.TwoWay)
    
    // Properties
    member this.TrackColor
        with get() = this.GetValue(trackColorProperty) :?> Color
        and set(value: Color) = this.SetValue(trackColorProperty, value)
    
    member this.KnobColor
        with get() = this.GetValue(knobColorProperty) :?> Color
        and set(value: Color) = this.SetValue(knobColorProperty, value)
    
    member this.TrackProgressColor
        with get() = this.GetValue(trackProgressColorProperty) :?> Color
        and set(value: Color) = this.SetValue(trackProgressColorProperty, value)
    
    member this.Start
        with get() = this.GetValue(startProperty) :?> float
        and set(value: float) = this.SetValue(startProperty, value)
    
    member this.Arc
        with get() = this.GetValue(arcProperty) :?> float
        and set(value: float) = this.SetValue(arcProperty, value)
    
    member this.Minimum
        with get() = this.GetValue(minimumProperty) :?> float
        and set(value: float) = this.SetValue(minimumProperty, value)
    
    member this.Maximum
        with get() = this.GetValue(maximumProperty) :?> float
        and set(value: float) = this.SetValue(maximumProperty, value)
    
    member this.Value
        with get() = this.GetValue(valueProperty) :?> float
        and set(value: float) = this.SetValue(valueProperty, value)
    
    member private this.RecalculateProgress() =
        progress <- (this.Value - this.Minimum) / (this.Maximum - this.Minimum)
        progressArc <- this.Arc * progress
        hasTouch <- false
        this.InvalidateSurface()
    
    override this.OnPropertyChanged(propertyName) =
        base.OnPropertyChanged(propertyName)
        
        if propertyName = "TrackColor" then
            trackPaint.Color <- this.TrackColor.ToSKColor()
            this.InvalidateSurface()
        elif propertyName = "KnobColor" then
            knobPaint.Color <- this.KnobColor.ToSKColor()
            this.InvalidateSurface()
        elif propertyName = "TrackProgressColor" then
            progressPaint.Color <- this.TrackProgressColor.ToSKColor()
            this.InvalidateSurface()
        elif propertyName = "Value" || propertyName = "Minimum" || propertyName = "Maximum" then
            this.RecalculateProgress()
    
    override this.OnTouch(e: SKTouchEventArgs) =
        base.OnTouch(e)
        hasTouch <- true
        touchX <- e.Location.X
        touchY <- e.Location.Y
        this.InvalidateSurface()
        e.Handled <- true
    
    override this.OnPaintSurface(e: SKPaintSurfaceEventArgs) =
        let canvas = e.Surface.Canvas
        canvas.Clear()
        
        let info = e.Info
        let padding = 50.0f
        let smallest = Math.Min(info.Width, info.Height) |> float32
        
        let x = padding + (float32 info.Width * 0.5f - smallest * 0.5f)
        let y = padding + (float32 info.Height * 0.5f - smallest * 0.5f)
        
        let arcRect = SKRect(x, y, x + smallest - 2.0f * padding, y + smallest - 2.0f * padding)
        let radius = arcRect.Width / 2.0f
        let originX = arcRect.MidX
        let originY = arcRect.MidY
        
        if hasTouch then
            let (cx, cy) = pointOnCircle originX originY radius touchX touchY
            let theta = pointOnCircleToAngle cx cy originX originY
            let angleEnd = (this.Start + this.Arc) % 360.0
            
            let mutable adjustedTheta = theta
            if this.Start > theta && theta <= angleEnd then
                adjustedTheta <- theta + 360.0
            
            let mutable newProgressArc = adjustedTheta - this.Start
            if newProgressArc > this.Arc then newProgressArc <- this.Arc
            elif newProgressArc < 0.0 then newProgressArc <- 0.0
            
            progress <- newProgressArc / this.Arc
            progressArc <- newProgressArc
            
            let calculatedValue = this.Minimum + ((this.Maximum - this.Minimum) * progress)
            if Math.Abs(this.Value - calculatedValue) > 0.001 then
                this.Value <- calculatedValue
        
        let angle = this.Start + progressArc
        let px = originX + radius * float32 (Math.Cos(toRadians angle))
        let py = originY + radius * float32 (Math.Sin(toRadians angle))
        
        // Draw track
        canvas.DrawArc(arcRect, float32 this.Start, float32 this.Arc, false, trackPaint)
        
        // Draw progress
        canvas.DrawArc(arcRect, float32 this.Start, float32 progressArc, false, progressPaint)
        
        // Draw knob
        canvas.DrawCircle(px, py, 20.0f, knobPaint)
    

/// F# wrapper for Fabulous integration
type CustomRadialSlider() =
    inherit SkRadialSlider()
    
    let mutable oldValue = 0.0
    let valueChanged = Event<EventHandler<ValueChangedEventArgs>, _>()
    
    [<CLIEvent>]
    member _.ValueChanged = valueChanged.Publish
    
    override this.OnPropertyChanged(propertyName) =
        base.OnPropertyChanged(propertyName)
        if propertyName = "Value" then
            valueChanged.Trigger(this, ValueChangedEventArgs(oldValue, this.Value))
            oldValue <- this.Value

type IRadialSlider =
    inherit IFabView

module RadialSlider =
    let WidgetKey = Widgets.register<CustomRadialSlider>()
    
    let TrackColor = Attributes.defineBindableColor (BindableProperty.Create("TrackColor", typeof<Color>, typeof<SkRadialSlider>, Colors.LightGray))
    let KnobColor = Attributes.defineBindableColor (BindableProperty.Create("KnobColor", typeof<Color>, typeof<SkRadialSlider>, Colors.Blue))
    let TrackProgressColor = Attributes.defineBindableColor (BindableProperty.Create("TrackProgressColor", typeof<Color>, typeof<SkRadialSlider>, Colors.Blue))
    let Start = Attributes.defineBindableFloat (BindableProperty.Create("Start", typeof<float>, typeof<SkRadialSlider>, 135.0))
    let Arc = Attributes.defineBindableFloat (BindableProperty.Create("Arc", typeof<float>, typeof<SkRadialSlider>, 270.0))
    let Minimum = Attributes.defineBindableFloat (BindableProperty.Create("Minimum", typeof<float>, typeof<SkRadialSlider>, 0.0))
    let Maximum = Attributes.defineBindableFloat (BindableProperty.Create("Maximum", typeof<float>, typeof<SkRadialSlider>, 10.0))
    
    let ValueChanged =
        Attributes.defineBindableWithEvent
            "RadialSlider_ValueProperty"
            (BindableProperty.Create("Value", typeof<float>, typeof<CustomRadialSlider>, 5.0, BindingMode.TwoWay))
            (fun target -> (target :?> CustomRadialSlider).ValueChanged)

[<AutoOpen>]
module RadialSliderBuilder =
    type Fabulous.Maui.View with
        static member inline RadialSlider(value: float, onValueChanged: ValueChangedEventArgs -> obj) =
            WidgetBuilder<'msg, IRadialSlider>(
                RadialSlider.WidgetKey,
                RadialSlider.ValueChanged.WithValue(ValueEventData.create value onValueChanged)
            )

type RadialSliderModifiers =
    [<Extension>]
    static member inline trackColor(this: WidgetBuilder<'msg, IRadialSlider>, value: Color) =
        this.AddScalar(RadialSlider.TrackColor.WithValue(value))
    
    [<Extension>]
    static member inline knobColor(this: WidgetBuilder<'msg, IRadialSlider>, value: Color) =
        this.AddScalar(RadialSlider.KnobColor.WithValue(value))
    
    [<Extension>]
    static member inline trackProgressColor(this: WidgetBuilder<'msg, IRadialSlider>, value: Color) =
        this.AddScalar(RadialSlider.TrackProgressColor.WithValue(value))
    
    [<Extension>]
    static member inline minimum(this: WidgetBuilder<'msg, IRadialSlider>, value: float) =
        this.AddScalar(RadialSlider.Minimum.WithValue(value))
    
    [<Extension>]
    static member inline maximum(this: WidgetBuilder<'msg, IRadialSlider>, value: float) =
        this.AddScalar(RadialSlider.Maximum.WithValue(value))
