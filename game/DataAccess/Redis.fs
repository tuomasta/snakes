namespace DataAccess

module Redis =
    open Microsoft.FSharp.Control
    open NReJSON
    open StackExchange.Redis
    open System.Text.Json
    open System.Text.Json.Serialization

    let private jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())

    type SystemTextJsonSerializer() =
        interface ISerializerProxy with
            member x.Deserialize<'TResult> (serializedValue: RedisResult) =
                JsonSerializer.Deserialize<'TResult>(serializedValue.ToString(), jsonOptions)
                
            member x.Serialize<'TObjectType> (obj : 'TObjectType) =
                JsonSerializer.Serialize(obj, jsonOptions)

    let private serializer : ISerializerProxy = SystemTextJsonSerializer()
    let private ifNull def value = if value = null then def else value

    let private redisURI = System.Environment.GetEnvironmentVariable("REDIS_URI") |> ifNull "127.0.0.1:6379"

    printf "USING Redis Uri: %s\n" redisURI

    let private redisClient = new System.Lazy<IConnectionMultiplexer>(fun _ ->
        NReJSONSerializer.SerializerProxy <- serializer
        let multiplexer : IConnectionMultiplexer = ConnectionMultiplexer.Connect(redisURI) 
        multiplexer
     )

    let getRedisDb () = redisClient.Value.GetDatabase()
    type GetDb = unit -> IDatabase

    let getRedisSubscriber () = redisClient.Value.GetSubscriber()
    type GetSubscriber = unit -> ISubscriber
        
    let setJson (key:string) (value:'TValue) (getDb:GetDb) =
        async {
            do! getDb () |> fun db -> db.JsonSetAsync<'TValue>(key, value) |> Async.AwaitTask |> Async.Ignore
            return value
        }
   
    let setJsonElement (key:string) path (value:'TValue) (getDb:GetDb) =
        async {
            do! getDb () |> fun db -> db.JsonSetAsync<'TValue>(key, value, path) |> Async.AwaitTask |> Async.Ignore
            return value
        }

    let getJson<'TValue > (key:string) (getDb:GetDb) : Async<'TValue> =
        async {
            let! redisResult = getDb () |> fun db -> db.JsonGetAsync<'TValue>(key) |> Async.AwaitTask
            return System.Linq.Enumerable.Single<'TValue>(redisResult)
        }

    let tryGetJson<'TValue> (key:string) (getDb:GetDb) : Async<'TValue option> =
        async {
            let! redisResult = getDb () |> fun db -> db.JsonGetAsync<'TValue>(key) |> Async.AwaitTask
            let enumerator = redisResult.GetEnumerator()
            
            let result = if enumerator.MoveNext() then Some(enumerator.Current) else None
            return result
        }
        
    let deleteJson (key:string) (getDb:GetDb) = async {
        do! getDb () |> fun db -> db.JsonDeleteAsync(key) |> Async.AwaitTask |> Async.Ignore
        return getDb
    }
    
    let deleteJsonElement (key:string) path (getDb:GetDb) = async {
        do! getDb () |> fun db -> db.JsonDeleteAsync(key, path) |> Async.AwaitTask |> Async.Ignore
        return getDb
    }
    
    let setHash (hash:string) (key:string) (value: RedisValue) (getDb:GetDb) : Async<unit>= async {
            do! getDb () |> fun db -> db.HashSetAsync (hash, key,  value) |> Async.AwaitTask |> Async.Ignore
        }
    
    let getHashValues (hash:string) (getDb:GetDb) = 
        getDb () |> fun db -> db.HashGetAllAsync (hash) |> Async.AwaitTask
        
    let deleteHash (hash:string) (key:string) (getDb:GetDb) = async {
        do! getDb () |> fun db -> db.HashDeleteAsync (hash, key) |> Async.AwaitTask |> Async.Ignore
        return getDb
    }

    let publish (key: string) value (getSubscriber: GetSubscriber) =
        getSubscriber () 
            |> fun sub -> sub.PublishAsync (key, serializer.Serialize value) |> Async.AwaitTask |> Async.Ignore


    let subscribe (key: string) (handler: ('TValue -> unit)) (getSubscriber: GetSubscriber) = async {
        let handlerAction c (v: RedisValue) = 
            let value = serializer.Deserialize<'TValue> (RedisResult.Create v)
            handler value

        let unsubscriber () = getSubscriber () |> fun sub -> sub.UnsubscribeAsync (key, handlerAction, CommandFlags.None) |> Async.AwaitTask 

        do! getSubscriber () |> fun sub -> sub.SubscribeAsync (key, handlerAction, CommandFlags.None) |> Async.AwaitTask 

        return unsubscriber
    }
    