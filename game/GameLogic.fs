module GameLogic

open System
open System.Numerics
open Shared.Enums
open Shared.DTO

type SetOfVectors = System.Collections.Generic.HashSet<Vector2>

let random = Random.Shared

let randomX () =
    float32 (random.Next(0, Config.GameArea.width - 1))

let randomY () =
    float32 (random.Next(0, Config.GameArea.height - 1))

let randomVector () = Vector2(randomX (), randomY ())

let EmptyDirection = Vector2(0f, 0f)

let RotateRight =
    Matrix3x2.CreateRotation(float32 (Math.PI * 0.5))

let RotateLeft =
    Matrix3x2.CreateRotation(float32 (Math.PI * -0.5))
    
// TODO OO type domain objects are not a thing in f#, so consider using plain records instead
type Player (id: string, name: string, head: Vector2, direction: Vector2) =
    member this.id = id
    member this.name = name
    member val score = 0 with get, set

    member val body: Vector2 List =
        [ for i in 0 .. Config.InitalBodyLenght ->
              (head, direction, i)
              |> fun (h, d, i) -> h - Vector2.Multiply(float32 i, d) ] with get, set

    member val direction = direction with get, set

    member this.tail = this.body[1..]
    member this.head: Vector2 = this.body.Head
    
    member this.isAlive: bool =
        this.direction <> EmptyDirection

    member this.kill() =
        this.direction <- EmptyDirection
        this.body <- []

    member this.move(shouldGrow) =
        let limit v max =
            let fMax = float32 max

            if v > fMax then 0f
            elif v < 0f then fMax
            else v

        // creates a new head by applying the direction to the current head
        let newHead: Vector2 =
            this.head + this.direction
            |> fun h -> (limit h.X (Config.GameArea.width - 1), limit h.Y (Config.GameArea.height - 1))
            |> Vector2

        if shouldGrow newHead then
            this.score <- this.score + 1
            this.body <- newHead :: this.body
        else
            this.body <- newHead :: this.body[.. this.body.Length - 2]

    member this.turn(turnTo: TurnDirection) =
        let transformation =
            if turnTo = TurnDirection.left then
                RotateLeft
            else
                RotateRight

        this.direction <- Vector2.Transform(this.direction, transformation)

type Game(name: string) =
    member this.name = name
    member val status = GameStatus.Lobby with get, set

    member val players: Player list = [] with get, set
    member val berries: Vector2 list = [ for _ in 1 .. Config.NumberOfBerries -> randomVector () ] with get, set

    member this.livePlayers =
        this.players |> Seq.filter (fun p -> p.isAlive)

    member this.getCollidingPlayers() =
        let rec reduce (seq1: seq<Vector2 list>) : Vector2 seq =
            if Seq.isEmpty seq1 then
                Seq.empty
            else
                seq {
                    yield! Seq.head seq1
                    yield! reduce (Seq.tail seq1)
                }

        // TODO: this does not include heads so test head to head collision separately
        let occupiedSpaces =
            this.livePlayers
            |> Seq.map (fun p -> p.tail)
            |> reduce
            |> SetOfVectors

        this.livePlayers
        |> Seq.filter (fun p -> occupiedSpaces.Contains(p.head))

    member this.addPlayer name id =
        let player =
            Player(id, name, Vector2(4f, float32 (2 * this.players.Length)), Vector2(1f, 0f))

        this.players <- player :: this.players
        player

    member this.remove playerId =
        this.players <-
            this.players
            |> List.filter (fun x -> x.id <> playerId)


    member this.tick() =
        let tryEatBerries head =
            let nonEatenBerries =
                this.berries |> List.filter (fun b -> b <> head)

            if this.berries.Length <> nonEatenBerries.Length then
                this.berries <- randomVector () :: nonEatenBerries
                true
            else
                false

        for player in this.livePlayers do
            player.move tryEatBerries

        for player in this.getCollidingPlayers () do
            player.kill ()

        this.status <-
            if Seq.isEmpty this.livePlayers then
                GameStatus.Ended
            else
                this.status

        ()

    member this.start() = this.status <- GameStatus.Running

let toDto (game: Game) : GameDto =
    let mapVector (v: Vector2) = { x = int v.X; y = int v.Y }
    let mapPlayer (p: Player) : PlayerDto = {
      name = p.name
      body = p.body |> List.map mapVector
      score = p.score
      isAlive = p.isAlive
    }

    {
      name = game.name
      status = game.status
      area = Config.GameArea
      berries = game.berries |> List.map mapVector
      players = game.players |> List.map mapPlayer
    }