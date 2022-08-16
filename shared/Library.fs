namespace Shared

module Enums =
    type TurnDirection = 
        | left = 1
        | right = -1

    type GameStatus = 
        | Lobby = 0
        | Running = 1
        | Ended = 2

module Dtos =
    type Point = {
        x: int
        y: int
    }

    type Size = {
        width: int
        height: int
    }

    module Game =
        type Player = { 
            name: string 
            body: Point list
            score: int 
            isAlive: bool
        }

        type Data = { 
            name: string
            status: Enums.GameStatus
            area: Size
            players: Player list
            berries: Point list
        }
