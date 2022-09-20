namespace Shared

module Enums =
    type TurnDirection = 
        | left = 1
        | right = -1

    type GameStatus = 
        | Lobby = 0
        | Running = 1
        | Ended = 2

module DTO =
    type Point = {
        x: int
        y: int
    }

    type Size = {
        width: int
        height: int
    }

    type PlayerDto = {
        name: string 
        body: Point list
        score: int 
        isAlive: bool
    }

    type GameDto = { 
        name: string
        status: Enums.GameStatus
        area: Size
        players: PlayerDto list
        berries: Point list
    }
