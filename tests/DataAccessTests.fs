module DataAccessTests

open System.Numerics
open GameLogic
open Microsoft.FSharp.Control
open NReJSON
open Shared.Enums
open StackExchange.Redis
open Xunit
open FsUnit
open System
open DataAccess.Game

// integration tests that requires redis to be exposed on local host.
// see k8s/redis.yml

type TestRecord = { stringProp: string; intProp: int }

module TestDb =
    let testDbID = 15
    let redisURI = System.Environment.GetEnvironmentVariable("127.0.0.1:6379")

    NReJSONSerializer.SerializerProxy <- DataAccess.Redis.SystemTextJsonSerializer()

    let redisConnection =  ConnectionMultiplexer.Connect("127.0.0.1:6379,allowAdmin=true")
    
    // clean the db before running tests
    redisConnection.GetServer("127.0.0.1:6379").FlushDatabase(testDbID)

    let getRedisDb () = redisConnection.GetDatabase(testDbID)


[<Fact>]
let ``GIVEN redis server runs locally THEN client can ping database``() =
    TestDb.getRedisDb () 
        |> fun db -> db.Ping ()
        |> should be (lessThan (TimeSpan.FromSeconds 5))
        
[<Fact>]
let ``GIVEN redis server runs locally THEN client can set and get objects``() =
    async {
        let expectedObject = { stringProp = "foo"; intProp = 10 }
        
        do! TestDb.getRedisDb |> DataAccess.Redis.setJson "key" expectedObject |> Async.Ignore
        let! result = TestDb.getRedisDb |> DataAccess.Redis.tryGetJson<TestRecord> "key"
        result |> should equal (Some expectedObject)
    }
    
type ``Given a game exists``() =
    let game = "test-game" |> Game
    
    do game.addPlayer "test-player" "test-player" |> ignore

    [<Fact>]
    member x.``when the game is saved then game can be fetch from the db`` () = async {
        let! expectedGame =  TestDb.getRedisDb |> saveGame game
        let! resultGame = TestDb.getRedisDb |> tryGetGame expectedGame.name
        
        resultGame |> Option.get |> toDto |> should equal (expectedGame |> toDto)
    }
    
    [<Fact>]
    member x.``when the game is saved then game is listed in game names`` () = async {
        let! expectedGame =  TestDb.getRedisDb |> saveGame game
        let! names = TestDb.getRedisDb |> getGameNames
        
        names |> should contain expectedGame.name
    }
    
    [<Fact>]
    member x.``when updating game status then it updates correctly`` () = async {
        do! TestDb.getRedisDb |> saveGame game |> Async.Ignore
        
        // act
        do! TestDb.getRedisDb |> startGame game.name
        
        // assert
        let! resultGame = TestDb.getRedisDb |> tryGetGame game.name
        resultGame |> Option.map (fun g -> g.status) |> Option.get |> should equal GameStatus.Running
    }
    
    [<Fact>]
    member x.``when updating player direction then it updates correctly`` () = async {
        do! TestDb.getRedisDb |> saveGame game |> Async.Ignore
        
        let expectedDirection = Vector2(float32 0, float32 1)
        // act
        do! TestDb.getRedisDb |> setPlayerDirection game.name "test-player" expectedDirection
        
        // assert
        let! resultGame = TestDb.getRedisDb |> tryGetGame game.name
        let player = resultGame |> Option.map (fun g -> g.players.Head) |> Option.get 
        
        player.direction |> should equal expectedDirection
    }
    
    [<Fact>]
    member x.``when removing player then it updates correctly`` () = async {
        do! TestDb.getRedisDb |> saveGame game |> Async.Ignore

        // act
        do! TestDb.getRedisDb |> deletePlayer game.name "test-player"
        
        // assert
        let! resultGame = TestDb.getRedisDb |> tryGetGame game.name
        resultGame |> Option.map (fun g -> g.players) |> Option.get |> should be Empty
    }
