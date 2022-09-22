module GameHub

open Microsoft.AspNetCore.SignalR
open GameLogic
open Microsoft.FSharp.Control
open Shared.Enums
open System.Threading
open DataAccess.Game
open DataAccess.Redis
open Option
open Dictionary

type IClientApi =
    abstract member GameState: Shared.DTO.GameDto -> Tasks.Task

type ConnectionData = { GameId: string; PlayerId: string }

type GameHub() =
    inherit Hub<IClientApi>()

    static let ConnectionData =
        ConcurrentDictionary<string, ConnectionData>()

    member this.Join(nameOfPlayer: string, nameOfGame: string) =
        task {
            let connectionId = this.Context.ConnectionId
            let! game = getRedisDb |> getOrCreateGame nameOfGame

            let doAccept _ =
                async {
                    let player: Player =
                        game.addPlayer nameOfPlayer connectionId

                    let connectionData =
                        { GameId = game.name
                          PlayerId = player.id }

                    let update _ __ = connectionData

                    ConnectionData.AddOrUpdate(connectionId, connectionData, update)
                    |> ignore

                    // TODO: handle failures
                    // TODO: save only the new player to avoid conflicts
                    do! getRedisDb |> saveGame game |> Async.Ignore

                    do!
                        this.Groups.AddToGroupAsync(connectionId, nameOfGame, CancellationToken.None)
                        |> Async.AwaitTask

                    do!
                        getRedisSubscriber
                        |> publishGameState (game |> toDto)

                    return true
                }

            if game.status = GameStatus.Lobby then
                printf "Player connected, Player:%s, Game:%s\n" nameOfPlayer nameOfGame
                return! doAccept ()
            else
                return false
        }

    override this.OnDisconnectedAsync e =
        let connectionId = this.Context.ConnectionId

        let deletePlayer data =
            getRedisDb
            |> deletePlayer data.GameId data.PlayerId

        // TODO: handle failures
        ConnectionData
        |> tryGet connectionId
        |> Option.iterAsync deletePlayer
        |> Async.RunSynchronously

        ConnectionData.TryRemove(connectionId) |> ignore
        ``base``.OnDisconnectedAsync(e)


    member this.StartGame() =
        task {
            // TODO: handle failures
            do!
                ConnectionData
                |> tryGet this.Context.ConnectionId
                |> Option.iterAsync (fun data -> getRedisDb |> startGame data.GameId)
        }

    member this.Turn(direction: TurnDirection) =
        task {
            let turnPlayer (game: Game, data) =
                let player =
                    game.players
                    |> List.find (fun p -> p.id = data.PlayerId)

                player.turn direction

                getRedisDb
                |> setPlayerDirection game.name player.id player.direction

            // TODO: handle failures
            do!
                ConnectionData
                |> tryGet this.Context.ConnectionId
                |> Option.bindAsync (fun data ->
                    getRedisDb
                    |> tryGetGame data.GameId
                    |> Option.bindAsync2 (fun g -> (g, data)))
                |> Async.thenAsync (fun o ->
                    Option.map turnPlayer o
                    |> Option.defaultWith async.Zero)
        }
