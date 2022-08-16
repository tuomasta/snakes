module State

open System.Numerics
open GameLogic
open Shared.Dtos

type Map<'T, 'TValue> = System.Collections.Concurrent.ConcurrentDictionary<'T, 'TValue>

let Games = Map<string, Game>()

let tryGet (key: 'T) (map: Map<'T, 'TValue>) =
    match map.TryGetValue key with
    | true, value -> Some(value)
    | false, _ -> None

let tryGetGame name = Games |> tryGet name

let getGameDto (game: Game) : Shared.Dtos.Game.Data =
    let mapToPoint (vectors: Vector2 list) : Point list =
        vectors
        |> List.map (fun v -> { x = int v.X; y = int v.Y })

    { name = game.name
      status = game.status
      area = Config.GameArea
      berries = game.berries |> mapToPoint
      players =
        game.players
        |> List.map (fun p ->
            { name = p.name
              body = p.body |> mapToPoint
              score = p.score
              isAlive = p.isAlive }) }

let getOrCreateGame name =
    Games.GetOrAdd(name, (fun name -> name |> Game))
