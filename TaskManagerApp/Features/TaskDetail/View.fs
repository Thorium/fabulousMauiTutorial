namespace TaskManagerApp.Features.TaskDetail

open Fabulous
open Fabulous.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Graphics
open TaskManagerApp
open TaskManagerApp.Controls

open type Fabulous.Maui.View

module View =
    
    /// Renders the priority selection with radial slider
    let private prioritySection (model: Model) =
        let priority = Priority.fromInt (int model.Priority)
        
        (VStack(spacing = 16.) {
            Label("Priority")
                .font(size = 18., attributes = FontAttributes.Bold)
                .textColor(Colors.Black)
            
            Label($"Value: {int model.Priority}")
                .font(size = 14.)
                .textColor(Colors.Gray)
                .centerTextHorizontal()
            
            Label(Priority.toString priority)
                .font(size = 16., attributes = FontAttributes.Bold)
                .textColor(
                    match priority with
                    | Priority.High -> Colors.Red
                    | Priority.Medium -> Colors.Orange
                    | Priority.Low -> Colors.Green
                )
                .centerTextHorizontal()
            
            RadialSlider(
                model.Priority,
                fun args -> PriorityChanged args.NewValue
            )
                .minimum(0.0)
                .maximum(10.0)
                .trackColor(Colors.LightGray)
                .trackProgressColor(
                    match priority with
                    | Priority.High -> Colors.Red
                    | Priority.Medium -> Colors.Orange
                    | Priority.Low -> Colors.Green
                )
                .knobColor(Colors.White)
                .height(250.)
                .width(250.)
                .centerHorizontal()
        })
            .padding(16.)
            .background(SolidColorBrush(Colors.White))
    
    /// Renders the task detail form
    let view model =
        let title = if model.TaskId.IsSome then "Edit Task" else "New Task"
        
        ContentPage(
            //title,
            if model.IsLoading then
                ScrollView(
                    (VStack() {
                        ActivityIndicator(true)
                            .color(Colors.Blue)
                        Label("Loading task...")
                            .textColor(Colors.Gray)
                    })
                        .centerHorizontal()
                        .centerVertical()
                )
            else
                ScrollView(
                    (VStack(spacing = 20.) {
                        // Title Section
                        (VStack(spacing = 8.) {
                            Label("Title")
                                .font(size = 18., attributes = FontAttributes.Bold)
                                .textColor(Colors.Black)
                            
                            Entry(model.Title, TitleChanged)
                                .placeholder("Enter task title...")
                                .font(size = 16.)
                            
                            Label($"{model.Title.Length}/{AppSettings.MaxTaskTitleLength} characters")
                                .font(size = 10.)
                                .textColor(Colors.Gray)
                        })
                            .padding(16.)
                            .background(SolidColorBrush(Colors.White))
                        
                        // Description Section
                        (VStack(spacing = 8.) {
                            Label("Description")
                                .font(size = 18., attributes = FontAttributes.Bold)
                                .textColor(Colors.Black)
                            
                            Editor(model.Description, DescriptionChanged)
                                .placeholder("Enter task description...")
                                .font(size = 16.)
                                .autoSize(EditorAutoSizeOption.TextChanges)
                                .minimumHeight(100.)
                            
                            Label($"{model.Description.Length}/{AppSettings.MaxTaskDescriptionLength} characters")
                                .font(size = 10.)
                                .textColor(Colors.Gray)
                        })
                            .padding(16.)
                            .background(SolidColorBrush(Colors.White))
                        
                        // Priority Section with Radial Slider
                        prioritySection model
                        
                        // Save Button
                        Button(
                            (if model.IsSaving then "Saving..." else "Save Task"),
                            SaveTask
                        )
                            .isEnabled(not model.IsSaving && not (System.String.IsNullOrWhiteSpace(model.Title)))
                            .background(SolidColorBrush(Colors.Blue))
                            .textColor(Colors.White)
                            .cornerRadius(8)
                            .padding(16.)
                            .margin(16.)
                    })
                        .padding(16.)
                )
        )