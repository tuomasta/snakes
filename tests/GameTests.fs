module GameTests

open System.Numerics
open Xunit
open FsUnit

open Utils

type ``Given a snake exists``() =
    let initPos = Vector2(10f, 10f)

    [<Theory>]
    [<InlineData("Left", "Left", "Down")>]
    [<InlineData("Left", "Right", "Up")>]
    [<InlineData("Right", "Left", "Up")>]
    [<InlineData("Right", "Right", "Down")>]
    [<InlineData("Down", "Left", "Right")>]
    [<InlineData("Down", "Right", "Left")>]
    [<InlineData("Up", "Left", "Left")>]
    [<InlineData("Up", "Right", "Right")>]
    member x.``when direction is (initialDirection) turning to (turnTo) then the snakes goes (endDirection)``
        initDirection
        turnTo
        endDirection
        =
        withSnake initPos initDirection
        |> turn turnTo
        |> direction
        |> should equal endDirection

    [<Fact>]
    member x.``when another snake does not collide then snakes are not determined as colliding ``() =
        let setupPlayers names (game: GameLogic.Game) =
            names
            |> Seq.iter (fun n -> game.addPlayer n n |> ignore)

            game

        let collidingPlayers (game: GameLogic.Game) = game.getCollidingPlayers ()

        GameLogic.Game "test"
        |> setupPlayers [ "p1"; "p2" ]
        |> collidingPlayers
        |> should be Empty
