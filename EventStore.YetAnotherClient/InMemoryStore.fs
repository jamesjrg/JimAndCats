(* Based on the client in FsUno by Jérémie Chassaing *)

namespace EventStore.YetAnotherClient

open System

exception WrongExpectedVersion

type private Stream<'a> = { mutable Events:  ('a * int) list }
    with
    static member version stream = 
        stream.Events
        |> Seq.last
        |> snd

type InMemoryStore<'a>() = 
    let mutable streams = Map.empty
            
    interface IEventStore<'a> with
        member this.ReadStream streamId version count = 
            let result =
                match streams.TryFind streamId with
                    | Some(stream) -> 
                        let events =
                            stream.Events
                            |> Seq.skipWhile (fun (_,v) -> v < version )
                            |> Seq.takeWhile (fun (_,v) -> v <= version + count)
                            |> Seq.toList 
                        let lastEventNumber = events |> Seq.last |> snd 
            
                        events |> List.map fst,
                            lastEventNumber ,
                            if lastEventNumber < version + count 
                            then None 
                            else Some (lastEventNumber+1)
            
                    | None -> [], -1, None

            async {return result}

        member this.AppendToStream streamId expectedVersion newEvents = 
            let eventsWithVersion =
                newEvents |> List.mapi (fun index event -> (event, expectedVersion + index + 1))

            match streams.TryFind streamId with
            | Some stream when Stream<'a>.version stream = expectedVersion -> 
                stream.Events <- stream.Events @ eventsWithVersion
        
            | None when expectedVersion = -1 -> 
                streams <- streams.Add(streamId, { Events = eventsWithVersion })        

            | _ -> raise WrongExpectedVersion 
        
            async{()}

        member this.SubscribeToStreamFrom
            streamId
            (lastCheckpoint : int)
            (handleEvent: 'a -> unit) =
            ()

        member this.StreamExists streamId = async {return true}