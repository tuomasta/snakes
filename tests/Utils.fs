module Utils

open System.Numerics
open GameLogic
open Shared.Enums

let toDirection direction =
    match direction with
    | "Left" -> Vector2(-1f, 0f)
    | "Right" -> Vector2(1f, 0f)
    | "Up" -> Vector2(0f, -1f)
    | "Down" -> Vector2(0f, 1f)
    | _ -> failwith $"unknown direction {direction}"

let toTurnDirection direction =
    match direction with
    | "Left" -> TurnDirection.left
    | "Right" -> TurnDirection.right
    | _ -> failwith $"unknown turn to direction {direction}"

let withSnake initPos direction =
    direction
    |> toDirection
    |> fun d -> Player("id", "name", initPos, d)

let turn direction (snake: Player) =
    direction |> toTurnDirection |> snake.turn
    snake

let direction (snake: Player) =
    match (int snake.direction.X, int snake.direction.Y) with
    | 1, 0 -> "Right"
    | -1, 0 -> "Left"
    | 0, -1 -> "Up"
    | 0, 1 -> "Down"
    | _ -> failwith $"unknown direction"
