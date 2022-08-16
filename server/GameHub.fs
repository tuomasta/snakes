module GameHub

open Microsoft.AspNetCore.SignalR
open GameLogic
open State
open Shared.Enums
open System.Threading

type IClientApi =
    abstract member GameState: Shared.Dtos.Game.Data -> Tasks.Task

type ConnectionData = { Game: Game; Player: Player }

type GameHub() =
    inherit Hub<IClientApi>()

    static let ConnectionData =
        Map<string, ConnectionData>()

    member this.Join(nameOfPlayer: string, nameOfGame: string) =
        task {
            let connectionId = this.Context.ConnectionId
            let game = getOrCreateGame nameOfGame

            let doAccept _ =
                async {
                    let player: Player =
                        game.addPlayer nameOfPlayer connectionId

                    // TODO: handle failure
                    ConnectionData.TryAdd(connectionId, { Game = game; Player = player })
                    |> ignore

                    do!
                        this.Groups.AddToGroupAsync(connectionId, nameOfGame, CancellationToken.None)
                        |> Async.AwaitTask

                    do!
                        game
                        |> getGameDto
                        |> this.NotifyStateUpdate nameOfGame
                        |> Async.AwaitTask

                    return true
                }

            if game.status = GameStatus.Lobby then
                return! doAccept ()
            else
                return false
        }

    override this.OnDisconnectedAsync e : Tasks.Task =
        let connectionId = this.Context.ConnectionId

        ConnectionData
        |> tryGet connectionId
        |> Option.iter (fun d -> d.Game.remove d.Player.id)

        // TODO: handle failure
        ConnectionData.TryRemove(connectionId) |> ignore
        ``base``.OnDisconnectedAsync(e)

    member this.StartGame() =
        ConnectionData
        |> tryGet this.Context.ConnectionId
        |> Option.iter (fun d -> d.Game.start ())

    member this.Turn(direction: TurnDirection) =
        ConnectionData
        |> tryGet this.Context.ConnectionId
        |> Option.iter (fun d -> d.Player.turn direction)

    member private this.NotifyStateUpdate nameOfGame jsonState : Tasks.Task =
        this
            .Clients
            .Group(nameOfGame)
            .GameState(jsonState)
