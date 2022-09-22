module Async

let thenAsync (next: 'T -> Async<'TRes>) (task: Async<'T>) : Async<'TRes> =
    async {
        let! input = task
        let! result = next input
        return result
    }

let mapAsync (next: 'T -> 'TRes) (task: Async<'T>) : Async<'TRes> =
    async {
        let! input = task
        let result = next input
        return result
    }
