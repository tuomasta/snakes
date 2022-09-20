namespace DataAccess

module Game =
    open Microsoft.FSharp.Control
    open StackExchange.Redis
    open GameLogic
    open Redis
    open Option
    open Shared.DTO
    open System.Numerics
    open Shared.Enums

    let private GameHashKey = "games"
    
    module DAO =
        type PlayerDAO = {
            id : string
            name: string 
            body: Point list
            direction: Point
            score: int 
            isAlive: bool
        }

        type GameDAO = { 
            name: string
            status: GameStatus
            area: Size
            players: Map<string, PlayerDAO>
            berries: Point list
        }
        
        let mapVector (v: Vector2) = { x = int v.X; y = int v.Y }
        
        let mapPoint (p: Point) = Vector2 (float32 p.x, float32 p.y)
        
        let toDAO (game: Game) : GameDAO =
            let mapPlayer (p: Player) : string*PlayerDAO =
                let dao = {
                  id = p.id
                  name = p.name
                  body = p.body |> List.map mapVector
                  direction = p.direction |> mapVector
                  score = p.score
                  isAlive = p.isAlive
                }
                (p.id, dao)

            {
              name = game.name
              status = game.status
              area = Config.GameArea
              berries = game.berries |> List.map mapVector
              players = game.players |> List.map mapPlayer |> Map.ofList
            }
    
        let fromDAO (dao: GameDAO) : Game =
            // todo: a big code smell here. I think it's because with f# you really should not do OO
            // so consider removing the "domain" objects and just use records
            let mapPlayer _ (pDAO: PlayerDAO) : Player =
              let body = pDAO.body |> List.map mapPoint
              let head = if body.Length > 0 then body.Head else Vector2 ()
              let direction = pDAO.direction |> mapPoint

              let player: Player = Player (pDAO.id, pDAO.name, head, direction)
              player.body <- body
              player.score <- pDAO.score
              player

            let game = dao.name |> Game
            game.berries <- dao.berries |> List.map mapPoint
            game.players <- dao.players |> Map.map mapPlayer |> Map.toSeq |> Seq.map snd |> Seq.toList  
            game.status <- dao.status
            game
        
    
    // todo use proper data access object
    let tryGetGame name getDb = getDb |> tryGetJson<DAO.GameDAO> name |> Option.bindAsync2 DAO.fromDAO

    let saveGame (game:Game) (getDb: GetDb) = async {
        // todo check conflicts
        // todo make transactional
        let gameDto = game |> DAO.toDAO
        do! setJson game.name gameDto getDb |> Async.Ignore
        
        do! setHash GameHashKey game.name (RedisValue "1") getDb
        return game
    }
        
    let getOrCreateGame name getDb =
        // todo make transactional
        tryGetGame name getDb |> Option.defaultWithAsync (fun () -> saveGame (name |> Game) getDb)
        
    let deleteGame name (getDb: GetDb) =
        // todo make transactional
        getDb
            |> deleteHash GameHashKey name
            |> Async.thenAsync (deleteJson name) 
            |> Async.Ignore
        
    let getGameNames (getDb: GetDb) : Async<string seq>=
        getDb
            |> getHashValues GameHashKey
            |> Async.mapAsync (fun hashes -> (hashes |> Seq.map (fun hash -> hash.Name.ToString())))
            
    let startGame name getDb = 
        getDb
            |> setJsonElement name "$.status" GameStatus.Running
            |> Async.Ignore
    
    let setPlayerDirection name playerId (direction: Vector2) getDb = 
        getDb
            |> setJsonElement name $"$.players.{playerId}.direction" (direction |> DAO.mapVector)
            |> Async.Ignore
            
    let deletePlayer name playerId getDb = 
        getDb
            |> deleteJsonElement name $"$.players.{playerId}"
            |> Async.Ignore