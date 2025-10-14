namespace TaskManagerApp

open System

/// In-memory data store for tasks (replaces a real database)
module MockDataStore =
    
    /// Mutable list to store tasks in memory
    let private tasks = ResizeArray<MTask>()
    
    /// Initializes the store with sample data
    let initialize() =
        tasks.Clear()
        
        let sampleTasks = [
            Task.createDetailed "Welcome to Task Manager" "This is a sample task to get you started. You can edit or delete it." Priority.High
            Task.createDetailed "Create your first task" "Click the + button to add a new task" Priority.Medium
            Task.createDetailed "Complete a task" "Tap on a task to mark it as complete" Priority.Low
        ]
        
        tasks.AddRange(sampleTasks)
    
    /// Gets all tasks
    let getAllTasks() =
        tasks |> Seq.toList
    
    /// Gets a task by ID
    let getTaskById (TaskId id) =
        tasks 
        |> Seq.tryFind (fun t -> 
            let (TaskId taskId) = t.Id
            taskId = id)
    
    /// Adds a new task
    let addTask task =
        tasks.Add(task)
        task
    
    /// Updates an existing task
    let updateTask task =
        let (TaskId id) = task.Id
        let index = tasks.FindIndex(fun t -> 
            let (TaskId taskId) = t.Id
            taskId = id)
        
        if index >= 0 then
            tasks.[index] <- task
            Some task
        else
            None
    
    /// Deletes a task by ID
    let deleteTask (TaskId id) =
        let index = tasks.FindIndex(fun t -> 
            let (TaskId taskId) = t.Id
            taskId = id)
        
        if index >= 0 then
            tasks.RemoveAt(index)
            true
        else
            false
    
    /// Toggles task completion status
    let toggleTaskCompletion taskId =
        match getTaskById taskId with
        | Some task -> 
            let updated = Task.toggleCompletion task
            updateTask updated
        | None -> None
    
    /// Filters tasks based on the filter type
    let filterTasks filter =
        let allTasks = getAllTasks()
        match filter with
        | All -> allTasks
        | Active -> allTasks |> List.filter (fun t -> not t.IsCompleted)
        | Completed -> allTasks |> List.filter (fun t -> t.IsCompleted)

/// API abstraction layer (could be replaced with real HTTP calls)
module TaskApi =
    
    /// Simulates async data loading
    let private simulateDelay() = async {
        do! Async.Sleep(300) // Simulate network delay
    }
    
    /// Loads all tasks from the "backend"
    let loadTasks() = async {
        do! simulateDelay()
        return MockDataStore.getAllTasks()
    }
    
    /// Loads a specific task by ID
    let loadTask taskId = async {
        do! simulateDelay()
        return MockDataStore.getTaskById taskId
    }
    
    /// Saves a new task
    let saveTask task = async {
        do! simulateDelay()
        return MockDataStore.addTask task
    }
    
    /// Updates an existing task
    let updateTask task = async {
        do! simulateDelay()
        return MockDataStore.updateTask task
    }
    
    /// Deletes a task
    let deleteTask taskId = async {
        do! simulateDelay()
        return MockDataStore.deleteTask taskId
    }
    
    /// Toggles task completion
    let toggleTaskCompletion taskId = async {
        do! simulateDelay()
        return MockDataStore.toggleTaskCompletion taskId
    }
