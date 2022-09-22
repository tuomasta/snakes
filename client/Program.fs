open FSharp.Control
open Microsoft.AspNetCore.SignalR.Client
open Rendering
open Shared.Enums
open Spectre.Console
open System.Threading
open System.Threading.Tasks

let ifNull def value = if value = null then def else value

let serverUri =
    System.Environment.GetEnvironmentVariable("SERVER_URI")
    |> ifNull "http://localhost:5000"

let cts = new CancellationTokenSource()
let canvasY = 8
let mutable isInLobby = true

let renderGameData (game: Shared.DTO.GameDto) =
    let chart: BarChart =
        BarChart(
            Width = 60,
            Label = "[green bold underline]Score[/]",
            MaxValue = double (game.area.width * game.area.height)
        )
        |> BarChartExtensions.CenterLabel
        |> fun c -> c.AddItems(game.players, (fun p -> BarChartItem(p.name, p.score, SColor.Yellow)))

    AnsiConsole.Cursor.SetPosition(0, 2)
    AnsiConsole.Write(chart)

    let livePlayers =
        game.players |> List.filter (fun p -> p.isAlive)

    drawCanvas livePlayers game.berries game.area
    |> fun canvas ->
        AnsiConsole.Cursor.SetPosition(0, canvasY)
        AnsiConsole.Write(canvas)

let onStateUpdate (game: Shared.DTO.GameDto) =
    renderGameData game

    isInLobby <- game.status = GameStatus.Lobby

    // TODO: should not write the running message on every update
    let message =
        match game.status with
        | GameStatus.Lobby -> "Press any key to start the Game"
        | GameStatus.Ended -> "Game Finished                  "
        | _ -> "Game Runnning                   "

    AnsiConsole.Cursor.SetPosition(0, game.area.height + canvasY + 4)
    AnsiConsole.WriteLine(message)


let join (nameOfPlayer: string) (nameOfGame: string) (hub: HubConnection) token =
    hub.InvokeAsync<bool>("Join", nameOfPlayer, nameOfGame, token)

let start (hub: HubConnection) token = hub.InvokeAsync("StartGame", token)

let turn (direction: TurnDirection) (hub: HubConnection) token =
    hub.InvokeAsync("Turn", direction, token)

let startGame hub =
    async {
        while cts.IsCancellationRequested = false do
            let key: System.ConsoleKey option =
                if System.Console.KeyAvailable then
                    Some(System.Console.ReadKey(true).Key)
                else
                    None

            match (key, isInLobby) with
            | None, _
            | _, false -> ()
            | _ -> do! start hub cts.Token |> Async.AwaitTask

            match key with
            | Some System.ConsoleKey.LeftArrow
            | Some System.ConsoleKey.A ->
                do!
                    turn TurnDirection.left hub cts.Token
                    |> Async.AwaitTask
            | Some System.ConsoleKey.RightArrow
            | Some System.ConsoleKey.D ->
                do!
                    turn TurnDirection.right hub cts.Token
                    |> Async.AwaitTask
            | Some System.ConsoleKey.Escape -> cts.Cancel()
            | _ -> ()

            do! Task.Delay(10) |> Async.AwaitTask
    }

// For more information see https://aka.ms/fsharp-console-apps
[<EntryPoint>]
let main _ =
    async {
        let mutable player = ""
        let mutable game = ""

        let init () =
            try
                AnsiConsole.Clear()
                AnsiConsole.WriteLine("\nSnakes V0.1")
                player <- AnsiConsole.Ask<string>("Your name please:\n")
                game <- AnsiConsole.Ask<string>("Name of the game:\n")
                true
            with
            | _ -> false

        while (init () = false) do
            printf "\nwaiting for console to attach...\n"
            do! Task.Delay(1000) |> Async.AwaitTask

        AnsiConsole.Clear()
        AnsiConsole.WriteLine("Snakes V0.1")

        let hub =
            HubConnectionBuilder()
                .WithUrl($"{serverUri}/gamehub")
                .Build()

        do! hub.StartAsync(cts.Token) |> Async.AwaitTask

        use _ =
            hub.On<Shared.DTO.GameDto>("GameState", onStateUpdate)

        let! joined = join player game hub cts.Token |> Async.AwaitTask

        AnsiConsole.Cursor.SetPosition(0, canvasY)

        if joined then
            do! startGame hub
        else
            AnsiConsole.WriteLine("Could not join. Game is likely already running.")

        return 0 // main needs to return int
    }
    |> Async.RunSynchronously
