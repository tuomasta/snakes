module Option


// additional functionality for option to make it more usable with async
module Option =
    let bindAsync (asyncBinder: 'T -> Async<'TRes option>) (option: 'T option) : Async<'TRes Option> = async {
            let! res = match option with
                            | Some x -> asyncBinder x
                            | None ->
                                let none: 'TRes option = None
                                async.Return none
            return res
    }
    
    let bindAsync2 (binder: 'T -> 'TRes) (optionAsync: Async<'T option>) : Async<'TRes Option>  = async {
        let! option = optionAsync
        return match option with None -> None | Some x -> Some(binder x)
    }
    
    let iterAsync (binder: 'T -> Async<unit>) (option: 'T option) : Async<unit> = async {
        do! match option with None -> async{()} | Some x -> binder x
        return ()
    }
    
    let defaultWithAsync (defThunk: unit -> Async<'T>) (optionAsync:  Async<'T option>) : Async<'T> = async {
            let! option = optionAsync
            let! result = match option with None -> defThunk () | Some v -> async.Return v
            return result;
    }
