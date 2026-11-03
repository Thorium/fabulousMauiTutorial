namespace TaskManagerApp.Auth

open System
open System.Diagnostics
open Microsoft.Maui.Storage

/// Keeps the session in the platform's secure storage: Keychain on iOS, Keystore-encrypted
/// preferences on Android, DPAPI on Windows.
module SecureSessionStore =

    [<Literal>]
    let private Key = "taskmanager.session"

    let store: SessionStore =
        {
            Load =
                fun () ->
                    task {
                        try
                            let! json = SecureStorage.Default.GetAsync Key
                            return if isNull json then ValueNone else SessionJson.read json
                        with e ->
                            // An entry that can no longer be decrypted (e.g. after a device restore) means sign in again
                            Debug.WriteLine $"Stored session unreadable: {e.Message}"
                            SecureStorage.Default.Remove Key |> ignore
                            return ValueNone
                    }
            Save =
                fun session ->
                    task {
                        try
                            do! SecureStorage.Default.SetAsync(Key, SessionJson.write session)
                        with e ->
                            // The session still works for this run; the user signs in again next launch
                            Debug.WriteLine $"Session not stored: {e.Message}"
                    }
            Clear = fun () -> task { SecureStorage.Default.Remove Key |> ignore }
        }
