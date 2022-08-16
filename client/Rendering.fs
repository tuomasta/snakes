module Rendering

open Shared.Dtos
open Shared.Dtos.Game
open Spectre.Console

type SColor = Color
type Point = Shared.Dtos.Point
type Size = Shared.Dtos.Size

let borderColor = SColor.White
let berryColor = SColor.DeepPink4

let drawBoundary (canvas: Canvas) =
    for i in 0 .. canvas.Width - 1 do
        canvas.SetPixel(i, 0, borderColor) |> ignore

        canvas.SetPixel(i, canvas.Height - 1, borderColor)
        |> ignore

    for i in 0 .. canvas.Height - 1 do
        canvas.SetPixel(0, i, borderColor) |> ignore

        canvas.SetPixel(canvas.Width - 1, i, borderColor)
        |> ignore

    canvas

let drawPlayers (players: Player seq) (canvas: Canvas) =
    let drawPlayer i player =
        player.body
        |> Seq.iter (fun p ->
            canvas.SetPixel(p.x + 1, p.y + 1, SColor.FromInt32(i + 2))
            |> ignore)

    players |> Seq.iteri drawPlayer |> ignore
    canvas

let drawBerries (berries: Point seq) (canvas: Canvas) =
    let drawBerry p =
        canvas.SetPixel(p.x + 1, p.y + 1, berryColor)
        |> ignore

    berries |> Seq.iter drawBerry |> ignore
    canvas

let drawCanvas (players: Player list) (berries: Point list) (size: Size) =
    // +2 because boders take 1 px each
    new Canvas(size.width + 2, size.height + 2)
    |> drawBoundary
    |> drawBerries berries
    |> drawPlayers players
