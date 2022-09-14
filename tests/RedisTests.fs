module RedisTests

open Xunit
open StackExchange.Redis
open FsUnit
open System

[<Fact>]
let ``Client can ping database ``() =
    let redisClient = ConnectionMultiplexer.Connect("127.0.0.1:6379")
    let db = redisClient.GetDatabase()
    let acceptedTime = TimeSpan.FromSeconds 5
    db.Ping () |> should be (lessThan acceptedTime)