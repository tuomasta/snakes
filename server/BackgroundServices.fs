module BackgroundServices

open GameHub
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Hosting
open DataAccess.Redis
open DataAccess.Game
open System.Threading

type GameUpdateProxyService(hubContext: IHubContext<GameHub, IClientApi>) =
    inherit BackgroundService()

    let handleGameUpdate (dto:Shared.DTO.GameDto) = 
        hubContext.Clients.Group(dto.name).GameState(dto) |> Async.AwaitTask |> Async.RunSynchronously
        ()

    let mutable unsubscriber: (unit -> Async<unit>) = fun () -> async.Return ()

    override this.StartAsync _ = task {
        do! unsubscriber()

        let! sub = getRedisSubscriber |> subscribeGameState handleGameUpdate
        unsubscriber <- sub
    }

    override this.StopAsync _ = task {
        do! unsubscriber()
    }

    override this.ExecuteAsync _ = Tasks.Task.CompletedTask
