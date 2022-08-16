module BackgroundServices

open GameHub
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Hosting
open Shared.Enums
open State
open System.Threading

type GameService(hubContext: IHubContext<GameHub, IClientApi>) =
    inherit BackgroundService()

    override this.ExecuteAsync(stoppingToken: CancellationToken) : Tasks.Task =
        task {
            while stoppingToken.IsCancellationRequested = false do
                do!
                    Games.Values
                    |> Seq.filter (fun g -> g.status = GameStatus.Running)
                    |> Seq.map (fun game ->
                        async {
                            game.tick ()

                            do!
                                hubContext
                                    .Clients
                                    .Group(game.name)
                                    .GameState(game |> getGameDto)
                                |> Async.AwaitTask
                        })
                    |> Async.Parallel
                    |> Async.Ignore

                // clean up ended games
                Games.Values
                |> Seq.filter (fun g -> g.status = GameStatus.Ended)
                |> Seq.iter (fun g -> Games.TryRemove(g.name) |> ignore)

                // TODO: there is a better solution for this
                do! Tasks.Task.Delay(100) |> Async.AwaitTask
        }
