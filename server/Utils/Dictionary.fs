module Dictionary

open System.Collections.Generic

type ConcurrentDictionary<'T, 'TValue> = System.Collections.Concurrent.ConcurrentDictionary<'T, 'TValue>

type Dictionary<'T, 'TValue> = IDictionary<'T, 'TValue>

let tryGet (key: 'T) (map: Dictionary<'T, 'TValue>) =
    match map.TryGetValue key with
    | true, value -> Some(value)
    | false, _ -> None
    
let createMap (pairs: ('TKey*'TValue) seq) : Dictionary<'TKey, 'TValue> =
    let keyValues = pairs |> Seq.map (KeyValuePair<'TKey, 'TValue>)
    new System.Collections.Generic.Dictionary<'TKey, 'TValue>(keyValues)
    
