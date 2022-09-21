module BackgroundServices

open Microsoft.Extensions.Hosting
open Shared.Enums
open DataAccess.Redis
open DataAccess.Game
open GameLogic
open System.Threading

type GameService() =
    inherit BackgroundService()

    override this.ExecuteAsync(stoppingToken: CancellationToken) : Tasks.Task =
        task {
            while stoppingToken.IsCancellationRequested = false do

                // todo could be optimized by not processing games in lobby state
                let! gameNames = getRedisDb |> getGameNames

                do!
                    gameNames
                    |> Seq.map (fun gameName ->
                        async {
                            let! game =
                                getRedisDb
                                |> tryGetGame gameName
                                |> Async.mapAsync (Option.defaultWith (fun () -> gameName |> Game))

                            do!
                                match game.status with
                                | GameStatus.Running -> this.ProgressGame game
                                | GameStatus.Ended -> this.RemoveGame game
                                | _ -> async.Return ()
                        })
                    |> Async.Parallel
                    |> Async.Ignore

                // TODO: there is a better solution for this
                do! Tasks.Task.Delay(100) |> Async.AwaitTask
        }

    member this.ProgressGame(game: Game) : Async<unit> =
        async {
            game.tick ()

            // TODO: save only the berries, status, player bodies and alive status override player direction changes 
            do! getRedisDb |> saveGame game |> Async.Ignore

            do! getRedisSubscriber |> publishGameState (game |> toDto)
        }

    member this.RemoveGame(game: Game) : Async<unit> = getRedisDb |> deleteGame game.name
