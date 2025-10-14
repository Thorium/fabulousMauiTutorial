namespace TaskManagerApp

open System

/// Represents the unique identifier for a task
type TaskId = TaskId of Guid

/// Represents the priority level of a task
type Priority = 
    | Low
    | Medium
    | High

/// Converts an integer (0-10) to a Priority level
module Priority =
    let fromInt value =
        match value with
        | v when v <= 3 -> Low
        | v when v <= 7 -> Medium
        | _ -> High
    
    let toInt priority =
        match priority with
        | Low -> 2
        | Medium -> 5
        | High -> 9
    
    let toString priority =
        match priority with
        | Low -> "Low"
        | Medium -> "Medium"
        | High -> "High"

/// Represents a task filter option
type TaskFilter =
    | All
    | Active
    | Completed

/// Represents a single task
type MTask = {
    Id: TaskId
    Title: string
    Description: string
    Priority: Priority
    IsCompleted: bool
    CreatedAt: DateTime
}

/// Helper functions for working with tasks
module Task =
    /// Creates a new task with default values
    let create title =
        {
            Id = TaskId (Guid.NewGuid())
            Title = title
            Description = ""
            Priority = Medium
            IsCompleted = false
            CreatedAt = DateTime.Now
        }
    
    /// Creates a new task with all properties
    let createDetailed title description priority =
        {
            Id = TaskId (Guid.NewGuid())
            Title = title
            Description = description
            Priority = priority
            IsCompleted = false
            CreatedAt = DateTime.Now
        }
    
    /// Toggles the completion status of a task
    let toggleCompletion task =
        { task with IsCompleted = not task.IsCompleted }
    
    /// Updates the priority of a task
    let updatePriority priority task =
        { task with Priority = priority }
    
    /// Updates task details
    let updateDetails title description task =
        { task with Title = title; Description = description }

/// Application-wide settings and constants
module AppSettings =
    let MaxTaskTitleLength = 100
    let MaxTaskDescriptionLength = 500
    let DefaultPriorityValue = 5
