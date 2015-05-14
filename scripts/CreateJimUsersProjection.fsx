#load "EventStoreProjections.fsx"

open EventStoreProjections

let projection = """fromStream("jim-users-private").
    when({
        "UserCreated" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.Item.Id,
            name : event.body.Item.Name,
            email : event.body.Item.Email,
            creationTime : event.body.Item.CreationTime.ticks
          });
         },
         "NameChanged" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.Item.Id,
            name : event.body.Item.Name});
         },
         "EmailChanged" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.Item.Id,
            email : event.body.Item.Email});
         }
    })
"""

postProjection "jim-users-public" projection