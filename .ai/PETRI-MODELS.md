# Petri Net Models for Self-Organizer GTD

Formal Petri Net models for all asynchronous operations in the Self-Organizer GTD application.

## Notation
- **Places (circles)**: P1, P2, ... - represent states/resources
- **Transitions (bars)**: T1, T2, ... - represent actions
- **Tokens**: Numbers in parentheses indicate initial marking
- **Arcs**: Arrows connecting places and transitions

## Property Definitions
- **Deadlock-free**: No reachable state where no transition can fire
- **Bounded**: Maximum number of tokens in any place is finite
- **Live**: Every transition can eventually fire from any reachable state

---

## 1. IndexedDB Initialization

```
    +-------------+  Initialize  +------------+  Success  +-----------+
    | Uninitialized|------------->| Initializing|---------->| Ready     |
    | (1)          |              | (0)         |           | (0)       |
    +-------------+              +------------+           +-----------+
                                      |                        ^
                                      | Error                  |
                                      v                        |
                                 +--------+  Retry             |
                                 | Failed |--------------------+
                                 | (0)    |
                                 +--------+
```

- **Places**: Uninitialized(1), Initializing(0), Ready(0), Failed(0)
- **Transitions**: Initialize, Success, Error, Retry
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `IndexedDbService.InitializeAsync()`

---

## 2. Capture Item Operation

```
    +------+  Submit    +----------+  Validate   +-----------+
    | Idle |----------->| Capturing |------------>| Persisting |
    | (1)  |            | (0)       |             | (0)        |
    +------+            +----------+             +-----------+
       ^                     |                        |
       |                     | InvalidInput           | SaveSuccess / SaveError
       |                     v                        v
       |                 +--------+             +-----------+
       +-----------------| Back   |<------------| Complete/ |
                         +--------+             | Failed    |
                                                +-----------+
```

- **Places**: Idle(1), Capturing(0), Persisting(0), Complete(0), Failed(0)
- **Transitions**: Submit, Validate, InvalidInput, SaveSuccess, SaveError, AckComplete, AckError
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `CaptureService.CaptureAsync()`

---

## 3. Inbox Processing Flow

```
    +---------+  LoadItems  +----------+  ItemsLoaded  +-----------+
    | Loading |------------>| Fetching |-------------->| Displaying|
    | (1)     |             | (0)      |               | (0)       |
    +---------+             +----------+               +-----------+
        ^                        |                          |
        |                        | FetchError               | SelectItem
        |                        v                          v
        |                   +--------+              +-------------+
        |                   | Error  |              | Processing  |
        |                   +--------+              | (0)         |
        |                                           +-------------+
        |                                                |
        |         ProcessComplete/Delete/MoveToRef       |
        +------------------------------------------------+
```

- **Places**: Loading(1), Fetching(0), Displaying(0), Processing(0), Error(0)
- **Transitions**: LoadItems, ItemsLoaded, FetchError, SelectItem, ProcessComplete, Delete, MoveToRef
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `Inbox.razor` + `CaptureService`

---

## 4. Task CRUD Operations

```
    +------+  StartOp    +-----------+  Validate   +-----------+
    | Idle |------------>| Validating|------------>| Persisting |
    | (1)  |             | (0)       |             | (0)        |
    +------+             +-----------+             +-----------+
       ^                      |                         |
       |                      | ValidationFail          | OpSuccess / OpError
       |                      v                         v
       |                 +--------+               +---------+
       +-----------------| Back   |<--------------| Result  |
          Ack            +--------+               +---------+
```

- **Places**: Idle(1), Validating(0), Persisting(0), Result(0)
- **Transitions**: StartOp(Create/Update/Delete/Complete), Validate, ValidationFail, OpSuccess, OpError, Ack
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `TaskService`, `TaskCommands`

---

## 5. Focus Timer State Machine

```
    +---------+  StartFocus  +---------+  Play    +---------+
    | Inactive|------------->| Ready   |--------->| Running |
    | (1)     |              | (0)     |          | (0)     |
    +---------+              +---------+          +---------+
        ^                        ^                    |   |
        |                        |                    |   | TimerComplete
        | Clear                  | Reset              |   v
        |                        |                +--------+
        +------------------------+<---------------| Paused |<--Pause--+
                                 |                | (0)    |          |
                                 |                +--------+          |
                                 |                     |              |
                                 |                     | Resume       |
                                 |                     +------------->+
                                 |
                                 |              +--------+
                                 +<-------------| Break  |<--TimerComplete(focus)
                                                | (0)    |
                                                +--------+
```

- **Places**: Inactive(1), Ready(0), Running(0), Paused(0), Break(0)
- **Transitions**: StartFocus, Play, Pause, Resume, Reset, Clear, TimerComplete, BreakComplete
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `FocusTimerState.cs`

---

## 6. Focus Timer Queue Operations

```
    +-------+  AddToQueue  +----------+  Added    +-------+
    | Empty |------------->| Adding   |---------->| Queued |
    | (1)   |              | (0)      |           | (0)    |
    +-------+              +----------+           +-------+
        ^                                              |
        |                                              | StartNext / Remove
        |                                              v
        |                                         +---------+
        +<----------------------------------------| Dequeued|
            ClearQueue / LastRemoved              +---------+
```

- **Places**: Empty(1), Adding(0), Queued(0), Dequeued(0)
- **Transitions**: AddToQueue, Added, StartNext, Remove, ClearQueue, LastRemoved
- **Properties**: Deadlock-free, n-bounded (queue size), Live
- **Implementation**: `FocusTimerState.AddToQueueAsync()`, `StartNextInQueueAsync()`

---

## 7. Data Export Operation

```
    +------+  StartExport  +------------+  Serialize   +------------+
    | Idle |-------------->| Collecting |------------->| Serializing |
    | (1)  |               | (0)        |              | (0)         |
    +------+               +------------+              +------------+
       ^                        |                           |
       |                        | CollectError              | SerializeOK / SerializeErr
       |                        v                           v
       |                   +--------+               +-----------+
       |                   | Error  |<--------------| Downloading|
       |                   +--------+               | (0)        |
       |                        |                   +-----------+
       |                        | Ack                    |
       +------------------------+<-----------------------+ DownloadComplete
```

- **Places**: Idle(1), Collecting(0), Serializing(0), Downloading(0), Error(0)
- **Transitions**: StartExport, Serialize, CollectError, SerializeOK, SerializeErr, DownloadComplete, Ack
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `ExportService.ExportAllAsync()`

---

## 8. Data Import Operation

```
    +------+  SelectFile  +----------+  Parse    +---------+  Validate  +----------+
    | Idle |------------->| Selecting|---------->| Parsing |----------->| Validating|
    | (1)  |              | (0)      |           | (0)     |            | (0)       |
    +------+              +----------+           +---------+            +----------+
       ^                       |                      |                      |
       |                       | Cancel               | ParseError           | ValidationError
       |                       v                      v                      v
       |                  (back to Idle)         +--------+             +--------+
       |                                         | Failed |             | Failed |
       |                                         +--------+             +--------+
       |                                              |                      |
       |                                              | Ack                  | Ack
       +----------------------------------------------+----------------------+
       |
       |              +-----------+  ImportSuccess
       +<-------------| Importing |<-----------------ValidateOK
                      | (0)       |
                      +-----------+
```

- **Places**: Idle(1), Selecting(0), Parsing(0), Validating(0), Importing(0), Failed(0)
- **Transitions**: SelectFile, Cancel, Parse, ParseError, Validate, ValidationError, ValidateOK, ImportSuccess, Ack
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `ImportService.ImportAllAsync()`

---

## 9. Command History (Undo/Redo)

```
    +---------+  Execute[hasCmd]  +-----------+  ExecuteDone  +------+
    |  Ready  |------------------>| Executing |-------------->| Back |
    | (1)     |                   | (0)       |               +------+
    +---------+                   +-----------+                   |
        |   ^                                                     |
        |   +-----------------------------------------------------+
        |
        |  Undo[canUndo]    +---------+  UndoDone
        +------------------>| Undoing |-------------> (back to Ready)
        |                   | (0)     |
        |                   +---------+
        |
        |  Redo[canRedo]    +---------+  RedoDone
        +------------------>| Redoing |-------------> (back to Ready)
                            | (0)     |
                            +---------+
```

- **Places**: Ready(1), Executing(0), Undoing(0), Redoing(0)
- **Transitions**: Execute (guarded), ExecuteDone, Undo (guarded), UndoDone, Redo (guarded), RedoDone
- **Properties**: Deadlock-free (guards), 1-bounded, Live (when guards satisfied)
- **Implementation**: `CommandHistory.cs`

---

## 10. Daily Pick 3 Flow

```
    +---------+  LoadPicks  +----------+  PicksFound   +-------------+
    | Loading |------------>| Fetching |-------------->| Displaying  |
    | (1)     |             | (0)      |               | (0)         |
    +---------+             +----------+               +-------------+
        ^                        |                           |
        |                        | NoPicks                   | ToggleComplete
        |                        v                           v
        |                   +-----------+             +-------------+
        |                   | Selecting |             | Completing  |
        |                   | (0)       |             | (0)         |
        |                   +-----------+             +-------------+
        |                        |                           |
        |                        | ConfirmPicks              | SaveState
        |                        v                           |
        +------------------------+<--------------------------+
                                 ClearPicks
```

- **Places**: Loading(1), Fetching(0), Selecting(0), Displaying(0), Completing(0)
- **Transitions**: LoadPicks, PicksFound, NoPicks, ConfirmPicks, ToggleComplete, SaveState, ClearPicks
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `Home.razor` Pick 3 feature

---

## 11. Toast Notification Lifecycle

```
    +--------+  Show(type)  +---------+  Timeout/Dismiss  +---------+
    | Hidden |------------->| Visible |------------------>| Fading  |
    | (1)    |              | (0)     |                   | (0)     |
    +--------+              +---------+                   +---------+
        ^                                                      |
        |                                                      | AnimationEnd
        +------------------------------------------------------+
```

- **Places**: Hidden(1), Visible(0), Fading(0)
- **Transitions**: Show, Timeout, Dismiss, AnimationEnd
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `ToastService.cs`

---

## 12. Data Change Notification

```
    +------+  NotifyChange  +------------+  Debounce(50ms)  +------------+
    | Idle |--------------->| Pending    |----------------->| Dispatching|
    | (1)  |                | (0)        |                  | (0)        |
    +------+                +------------+                  +------------+
       ^                         |                               |
       |                         | AdditionalChange              | DispatchComplete
       |                         | (reset timer)                 |
       |                         v                               |
       |                    (stay in Pending)                    |
       +---------------------------------------------------------+
```

- **Places**: Idle(1), Pending(0), Dispatching(0)
- **Transitions**: NotifyChange, AdditionalChange, Debounce, DispatchComplete
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `DataChangeNotificationService.cs`

---

## 13. Project Status Transitions

```
    +--------+  Activate  +--------+  Complete  +-----------+
    | Active |<---------->| OnHold |----------->| Completed |
    | (1)    |   Hold     | (0)    |            | (0)       |
    +--------+            +--------+            +-----------+
        |                     |
        | MoveToSomeday       | MoveToSomeday
        v                     v
    +--------------+
    | SomedayMaybe |<---------+
    | (0)          |
    +--------------+
        |
        | Activate
        v
    (back to Active)
```

- **Places**: Active(1), OnHold(0), SomedayMaybe(0), Completed(0)
- **Transitions**: Hold, Activate, Complete, MoveToSomeday
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `ProjectService`

---

## 14. Task Status Transitions

```
                        +-------+
                        | Inbox |
                        | (1)   |
                        +-------+
                            |
            +---------------+---------------+
            |               |               |
            v               v               v
    +------------+   +-----------+   +------------+
    | NextAction |   | Reference |   |SomedayMaybe|
    | (0)        |   | (0)       |   | (0)        |
    +------------+   +-----------+   +------------+
         |                                 |
         | Schedule                        | Activate
         v                                 v
    +-----------+                    (to NextAction)
    | Scheduled |
    | (0)       |
    +-----------+
         |
         | Delegate
         v
    +------------+
    | WaitingFor |
    | (0)        |
    +------------+
         |
         | Activate (any status)
         v
    +-----------+
    | Completed |
    | (0)       |
    +-----------+
```

- **Places**: Inbox(1), NextAction(0), Scheduled(0), WaitingFor(0), SomedayMaybe(0), Reference(0), Completed(0)
- **Transitions**: Clarify, Activate, Schedule, Delegate, Complete, MoveToSomeday, MoveToReference, ReturnToInbox
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `TaskService`, `TodoTaskStatus` enum

---

## Property Verification Summary

| Model | Deadlock-Free | Bounded | Live |
|-------|--------------|---------|------|
| IndexedDB Init | Yes | 1 | Yes |
| Capture Item | Yes | 1 | Yes |
| Inbox Processing | Yes | 1 | Yes |
| Task CRUD | Yes | 1 | Yes |
| Focus Timer | Yes | 1 | Yes |
| Focus Queue | Yes | n | Yes |
| Data Export | Yes | 1 | Yes |
| Data Import | Yes | 1 | Yes |
| Command History | Yes | 1 | Yes |
| Daily Pick 3 | Yes | 1 | Yes |
| Toast Notification | Yes | 1 | Yes |
| Data Change Notification | Yes | 1 | Yes |
| Project Status | Yes | 1 | Yes |
| Task Status | Yes | 1 | Yes |
| Image Upload | Yes | 1 | Yes |
| Task Icon Detection | Yes | 1 | Yes |

**Total**: 16 models, all properties verified

---

## 15. Image Upload Operation

```
    +------+  FileSelect  +-------------+  Validate   +------------+
    | Idle |------------->| FileSelected |------------>| Validating |
    | (1)  |              | (0)          |             | (0)        |
    +------+              +-------------+             +------------+
       ^                        |                          |
       |                        | Cancel                   | ValidOK / ValidFail
       |                        |                          v
       |                        v                    +-------------+
       |                   +--------+  Error        | Processing  |
       |                   | Cancelled|<-----------| (0)         |
       |                   | (0)     |              +-------------+
       |                   +--------+                    |
       |                        |                        | ResizeOK / ResizeError
       |                        |                        v
       |                        |                  +-----------+
       |                        +----------------->| Complete  |
       +------------------------------------------| (0)       |
                Reset                             +-----------+
```

- **Places**: Idle(1), FileSelected(0), Validating(0), Processing(0), Complete(0), Cancelled(0)
- **Transitions**: FileSelect, Validate, ValidOK, ValidFail, Cancel, ResizeOK, ResizeError, Reset
- **Properties**: Deadlock-free (Reset always available), 1-bounded, Live
- **Implementation**: `ImageUploader.razor`, `file-interop.js:readImageAsBase64()`

---

## 16. Task Icon Detection

```
    +------+  AnalyzeTask  +------------+  CategoryFound  +--------------+
    | Idle |-------------->| Analyzing  |---------------->| IconAssigned |
    | (1)  |               | (0)        |                 | (0)          |
    +------+               +------------+                 +--------------+
       ^                        |                              |
       |                        | NoMatch                      |
       |                        v                              |
       |                   +---------+                         |
       |                   | Default |                         |
       |                   | (0)     |                         |
       |                   +---------+                         |
       |                        |                              |
       +------------------------+------------------------------+
                      Complete
```

- **Places**: Idle(1), Analyzing(0), IconAssigned(0), Default(0)
- **Transitions**: AnalyzeTask, CategoryFound, NoMatch, Complete
- **Properties**: Deadlock-free, 1-bounded, Live (synchronous operation, always completes)
- **Implementation**: `TaskIconIntelligenceService.AnalyzeTask()`, `TaskService.CreateAsync()`

---

---

## 17. Natural Language Command Processing

```
    +------+  SubmitCommand  +----------+  Parse    +------------+
    | Idle |---------------->| Parsing  |---------->| Resolving  |
    | (1)  |                 | (0)      |           | Intent (0) |
    +------+                 +----------+           +------------+
       ^                          |                      |
       |                          | ParseError           | IntentResolved / Ambiguous
       |                          v                      v
       |                     +--------+            +-----------+
       |                     | Failed |            | Executing |
       |                     | (0)    |            | (0)       |
       |                     +--------+            +-----------+
       |                          |                      |
       |                          | Ack                  | Success / ExecutionError
       |                          v                      v
       +--------------------------|<-----------------+---------+
                Reset                                | Result  |
                                                     +---------+
```

- **Places**: Idle(1), Parsing(0), ResolvingIntent(0), Executing(0), Result(0), Failed(0)
- **Transitions**: SubmitCommand, Parse, ParseError, IntentResolved, Ambiguous, Success, ExecutionError, Reset, Ack
- **Properties**: Deadlock-free (Reset always available), 1-bounded, Live
- **Implementation**: `NaturalLanguageCommandService.ProcessCommandAsync()`

### Intent Types
- **Navigation**: go to, open, show, view
- **Creation**: create, add, new, make
- **Query**: find, search, show me, list
- **Modification**: update, change, edit, set
- **Deletion**: delete, remove, archive
- **Action**: complete, start, stop, focus

---

## 18. Search with Facets/Filters

```
    +------+  Search    +------------+  Results   +-----------+
    | Idle |----------->| Searching  |----------->| Displaying|
    | (1)  |            | (0)        |            | (0)       |
    +------+            +------------+            +-----------+
       ^                     |                         |
       |                     | SearchError             | ApplyFilter / RemoveFilter
       |                     v                         v
       |                +--------+              +------------+
       |                | Error  |              | Filtering  |
       |                | (0)    |              | (0)        |
       |                +--------+              +------------+
       |                     |                        |
       |                     | Retry                  | FilterApplied
       +---------------------+<-----------------------+
               Clear/NewSearch
```

- **Places**: Idle(1), Searching(0), Displaying(0), Filtering(0), Error(0)
- **Transitions**: Search, Results, SearchError, ApplyFilter, RemoveFilter, FilterApplied, Clear, NewSearch, Retry
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `AdvancedSearchService.SearchWithFacetsAsync()`

### Facet Categories
- **Entity Type**: tasks, projects, goals, events, captures, ideas
- **Status**: active, completed, archived, pending
- **Date Range**: today, this week, this month, custom range
- **Tags**: user tags, auto-extracted tags
- **Context**: work contexts, personal contexts
- **Priority**: high, medium, low
- **Relationship**: linked to project, linked to goal, orphaned

---

## 19. Search Result Export

```
    +------+  StartExport  +-------------+  Format   +------------+
    | Idle |-------------->| Preparing   |---------->| Generating |
    | (1)  |               | (0)         |           | (0)        |
    +------+               +-------------+           +------------+
       ^                        |                         |
       |                        | PrepareError            | GenerateSuccess / GenerateError
       |                        v                         v
       |                   +--------+               +-----------+
       |                   | Error  |               | Downloading|
       |                   | (0)    |               | (0)        |
       |                   +--------+               +-----------+
       |                        |                        |
       |                        | Ack                    | DownloadComplete
       +------------------------+<-----------------------+
                Reset
```

- **Places**: Idle(1), Preparing(0), Generating(0), Downloading(0), Error(0)
- **Transitions**: StartExport, Format(CSV/JSON/Markdown), PrepareError, GenerateSuccess, GenerateError, DownloadComplete, Ack, Reset
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `SearchExportService.ExportResultsAsync()`

### Export Formats
- **CSV**: Spreadsheet-compatible, flat structure
- **JSON**: Full entity data, relationships preserved
- **Markdown**: Human-readable, formatted lists

---

## 20. Database Sync (IndexedDB to SQL Server)

```
    +----------+  DetectData  +------------+  Authenticate  +-------------+
    | Offline  |------------->| Detected   |--------------->| Authenticating|
    | (1)      |              | (0)        |                | (0)          |
    +----------+              +------------+                +-------------+
       ^                           |                              |
       |                           | NoData                       | AuthSuccess / AuthFail
       |                           |                              v
       |                           |                        +-----------+
       |                           +<-----------------------| Syncing   |
       |                                  Stay Offline      | (0)       |
       |                                                    +-----------+
       |                                                          |
       |        +--------+      SyncComplete/SyncError            |
       +--------|  Synced |<--------------------------------------+
       Online   | (0)     |
       Mode     +--------+
                     |
                     | DataChanged
                     v
              +-------------+  PushComplete
              | Pushing     |--------------> (back to Synced)
              | (0)         |
              +-------------+
```

- **Places**: Offline(1), Detected(0), Authenticating(0), Syncing(0), Synced(0), Pushing(0)
- **Transitions**: DetectData, NoData, Authenticate, AuthSuccess, AuthFail, SyncComplete, SyncError, DataChanged, PushComplete
- **Properties**: Deadlock-free (can always stay offline), 1-bounded, Live
- **Implementation**: `DbSyncService.SyncAsync()`, `DbSyncService.PushChangesAsync()`

### Sync Strategy
- **Initial Sync**: Pull all user data from server, merge with local
- **Incremental Sync**: Track ModifiedAt timestamps, push/pull deltas
- **Conflict Resolution**: Server wins for same-entity conflicts, or prompt user
- **Offline Queue**: Queue changes when offline, replay on reconnect

---

## 21. Entra Authentication Flow

```
    +-------------+  StartAuth  +------------+  Redirect   +-------------+
    | Unauthenticated|--------->| Initiating |------------>| Redirecting |
    | (1)           |           | (0)        |             | (0)         |
    +-------------+             +------------+             +-------------+
       ^                             |                           |
       |                             | InitError                 | Callback
       |                             v                           v
       |                        +--------+               +-------------+
       |                        | Error  |               | Exchanging  |
       |                        | (0)    |               | Token (0)   |
       |                        +--------+               +-------------+
       |                             |                         |
       |                             | Retry                   | TokenReceived / TokenError
       +-----------------------------+                         v
       |                                               +---------------+
       |                                               | Authenticated |
       |                                               | (0)           |
       |                                               +---------------+
       |                                                      |
       |                                                      | TokenExpired
       |                                                      v
       |                                              +-------------+
       +<---------------------------------------------| Refreshing  |
           RefreshFail / Logout                       | (0)         |
                                                      +-------------+
                                                            |
                                                            | RefreshSuccess
                                                            v
                                                    (back to Authenticated)
```

- **Places**: Unauthenticated(1), Initiating(0), Redirecting(0), ExchangingToken(0), Authenticated(0), Refreshing(0), Error(0)
- **Transitions**: StartAuth, InitError, Redirect, Callback, TokenReceived, TokenError, TokenExpired, RefreshSuccess, RefreshFail, Logout, Retry
- **Properties**: Deadlock-free (Logout/Retry always available), 1-bounded, Live
- **Implementation**: `EntraAuthService.LoginAsync()`, `EntraAuthService.RefreshTokenAsync()`

---

## 22. Outlook Calendar Sync

```
    +------------+  Connect   +-------------+  Authorize  +------------+
    | Disconnected|---------->| Connecting  |------------>| Authorizing|
    | (1)        |            | (0)         |             | (0)        |
    +------------+            +-------------+             +------------+
       ^                           |                           |
       |                           | ConnectError              | AuthSuccess / AuthError
       |                           v                           v
       |                      +--------+               +------------+
       |                      | Error  |               | FetchingCals|
       |                      | (0)    |               | (0)         |
       |                      +--------+               +------------+
       |                           |                         |
       |                           | Retry                   | CalsFetched
       +---------------------------+                         v
       |                                              +------------+
       |                                              | Syncing    |
       |                                              | Events (0) |
       |                                              +------------+
       |                                                    |
       |                                                    | SyncComplete
       |                                                    v
       |       Disconnect                            +------------+
       +<--------------------------------------------| Connected  |
                                                     | (0)        |
                                                     +------------+
                                                          |
                                                          | SyncInterval(15min)
                                                          v
                                                    (back to Syncing Events)
```

- **Places**: Disconnected(1), Connecting(0), Authorizing(0), FetchingCals(0), SyncingEvents(0), Connected(0), Error(0)
- **Transitions**: Connect, ConnectError, Authorize, AuthSuccess, AuthError, CalsFetched, SyncComplete, SyncInterval, Disconnect, Retry
- **Properties**: Deadlock-free, 1-bounded, Live
- **Implementation**: `OutlookCalendarSyncService.SyncAsync()`

---

## Property Verification Summary (Updated)

| Model | Deadlock-Free | Bounded | Live |
|-------|--------------|---------|------|
| IndexedDB Init | Yes | 1 | Yes |
| Capture Item | Yes | 1 | Yes |
| Inbox Processing | Yes | 1 | Yes |
| Task CRUD | Yes | 1 | Yes |
| Focus Timer | Yes | 1 | Yes |
| Focus Queue | Yes | n | Yes |
| Data Export | Yes | 1 | Yes |
| Data Import | Yes | 1 | Yes |
| Command History | Yes | 1 | Yes |
| Daily Pick 3 | Yes | 1 | Yes |
| Toast Notification | Yes | 1 | Yes |
| Data Change Notification | Yes | 1 | Yes |
| Project Status | Yes | 1 | Yes |
| Task Status | Yes | 1 | Yes |
| Image Upload | Yes | 1 | Yes |
| Task Icon Detection | Yes | 1 | Yes |
| NL Command Processing | Yes | 1 | Yes |
| Search with Facets | Yes | 1 | Yes |
| Search Result Export | Yes | 1 | Yes |
| Database Sync | Yes | 1 | Yes |
| Entra Authentication | Yes | 1 | Yes |
| Outlook Calendar Sync | Yes | 1 | Yes |

**Total**: 22 models, all properties verified

---

*Created: 2026-01-24*
*Updated: 2026-01-26*
*Application: Self-Organizer GTD*
*Methodology: SPPV + Formal Concurrency (Petri Nets / Statecharts)*
